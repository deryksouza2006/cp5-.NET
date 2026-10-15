# CP5 — Catálogo de Jogos (.NET e MongoDB)

Implementação dos sete endpoints do enunciado, em .NET 8, com MongoDB.Driver e Swagger.

## Executar no VS Code / terminal

Requisitos: SDK .NET 8 e MongoDB local. Docker é uma opção para iniciar o banco. Se usar somente um SDK mais recente, instale também o runtime ASP.NET Core 8 para executar o alvo net8.0, ou use a opção com Docker.

Na pasta que contém `CatalogoJogos.sln`:

```bash
docker compose up -d mongo
dotnet restore
dotnet build -c Release
dotnet run --project src/CatalogoJogos.Api --launch-profile http
```

Acesse **http://localhost:5080/swagger**. Mantenha o terminal da API aberto.

Alternativa para o banco, conforme a dica do professor:

```bash
docker run -d -p 127.0.0.1:27017:27017 --name mongo-local mongo:8.0.12
```

Escolha uma das opções para o MongoDB; ambas usam a porta 27017. Se o container `mongo-local` já existir, use `docker start mongo-local`.

O projeto não depende de Docker para a API: também funciona com MongoDB instalado e rodando localmente. Caso não tenha Docker, inicie o serviço MongoDB antes de executar os comandos `dotnet`.

## Executar tudo com Docker

Com Docker e Docker Compose disponíveis:

```bash
docker compose up -d --build
```

Abra **http://localhost:5080/swagger**. Nessa opção, não é necessário instalar o SDK no computador. O Compose mantém os dados em um volume e aguarda o MongoDB ficar pronto para iniciar a API.

Para parar, execute `docker compose down`. O volume é preservado.

## Configuração

Arquivo: `src/CatalogoJogos.Api/appsettings.json`.

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "cp5_catalogo_jogos",
    "CollectionName": "jogos"
  }
}
```

O banco e a coleção são criados pelo MongoDB na primeira inserção; não há migrations nem cadastro automático de exemplos. A aplicação lê essas opções por `IOptions<MongoDbSettings>`. Variáveis de ambiente podem sobrescrevê-las; o Compose usa `MongoDb__ConnectionString=mongodb://mongo:27017`, porque a API dentro do container acessa o banco pelo nome do serviço.

## Endpoints

| Método | Rota | Retorno |
|---|---|---|
| POST | `/api/jogos` | 201, recurso criado e cabeçalho Location; 400 para dados inválidos |
| GET | `/api/jogos` | 200 e lista (inclusive vazia) |
| GET | `/api/jogos/{id}` | 200; 404 se inexistente; 400 se ID inválido |
| PUT | `/api/jogos/{id}` | 200 e recurso atualizado; 404 se inexistente; 400 para dados/ID inválidos |
| DELETE | `/api/jogos/{id}` | 204 sem corpo; 404 se inexistente; 400 se ID inválido |
| GET | `/api/jogos/busca?plataforma=PlayStation%203&precoMaximo=100.00` | 200 e lista filtrada; 400 para parâmetros inválidos |
| GET | `/api/jogos/relatorio-estoque` | 200 e resumo por plataforma |

Use ponto no preço do JSON e da query string. O filtro de plataforma compara o texto exato, incluindo maiúsculas, minúsculas e acentos. Espaços nas extremidades são removidos no cadastro, atualização e busca. PUT exige todos os campos e preserva o ID; não cria um recurso inexistente.

POST/PUT exigem título, plataforma, gênero, preço, ano e estoque. Preço e estoque devem ser não negativos; zero é permitido. O ano aceita 1 a 9999. Os campos numéricos de entrada são nullable com `[Required]`, permitindo diferenciar ausência de valor e zero. IDs devem conter 24 caracteres hexadecimais.

## Primeiro teste no Swagger

1. Abra POST `/api/jogos` → **Try it out**. Envie:

```json
{
  "titulo": "Gran Turismo 5",
  "plataforma": "PlayStation 3",
  "genero": "Corrida",
  "preco": 79.90,
  "anoLancamento": 2010,
  "estoque": 3
}
```

2. Copie o `id` recebido e consulte GET `/api/jogos/{id}`.
3. Cadastre um segundo título da mesma plataforma, preço 120.00 e estoque 2.
4. Na busca, informe `PlayStation 3` e `precoMaximo=100.00`: apenas o primeiro jogo deve aparecer.
5. No relatório, essa plataforma deve ter **2 títulos** e **valorTotalInventario = 479.70**: `(79.90 × 3) + (120.00 × 2)`.
6. Atualize o primeiro jogo pelo PUT e consulte novamente o GET e o relatório.
7. Exclua pelo DELETE (204) e consulte seu ID novamente (404).

O arquivo `exemplos/CatalogoJogos.http` também contém requisições para clientes HTTP de IDEs. Na extensão REST Client do VS Code, substitua `@id` pelo ID criado.

## Arquitetura e pontos para explicar ao professor

| Parte | Responsabilidade |
|---|---|
| `Controllers/JogosController.cs` | Rotas, entrada HTTP e códigos de resposta. `[ApiController]` valida as anotações e responde 400 antes de chamar o serviço. |
| `Services/JogoService.cs` | Mapeia os DTOs, remove espaços nas extremidades e gera o ObjectId no cadastro. Depende de `IJogoRepository`. |
| `Repositories/JogoRepository.cs` | Executa CRUD, filtros e agregação pelo driver oficial. Recebe `IMongoDatabase` por injeção. |
| `Models/Jogo.cs` | Documento persistido: `[BsonId]`, `[BsonRepresentation]` e `[BsonElement]`. Preço usa Decimal128. |
| `DTOs/` | Contratos de entrada e resposta do relatório. O cliente não fornece o ID no corpo. |
| `Settings/MongoDbSettings.cs` | Opções da conexão, banco e coleção. |
| `Program.cs` | Configura Options Pattern, Swagger e injeção de dependência. |

`IMongoClient` e `IMongoDatabase` são singletons. O cliente reutiliza o pool de conexões. Repository e Service são scoped, uma instância por requisição. O controller depende de `IJogoService`, implementado por `JogoService`.

### Filtro: por que AND?

No repositório:

```csharp
var filtro = Builders<Jogo>.Filter.And(
    Builders<Jogo>.Filter.Eq(jogo => jogo.Plataforma, plataforma),
    Builders<Jogo>.Filter.Lte(jogo => jogo.Preco, precoMaximo));
```

`Eq` exige a plataforma informada. `Lte` significa menor ou igual ao preço máximo. `And` exige as duas condições para o mesmo documento. A coleção executa o filtro no banco; não há filtragem de uma lista carregada em memória.

### Agregação: o que é contado e somado?

No repositório, `.Aggregate().Group(...)` agrupa por plataforma. `grupo.Count()` conta documentos/títulos, inclusive jogos com estoque zero; não soma unidades de estoque. `grupo.Sum(jogo => jogo.Preco * jogo.Estoque)` soma o valor de cada título em estoque.

O driver traduz as expressões para uma pipeline no servidor. Sua lógica equivale a:

```javascript
db.jogos.aggregate([
  {
    $group: {
      _id: "$plataforma",
      quantidadeTitulos: { $sum: 1 },
      valorTotalInventario: { $sum: { $multiply: ["$preco", "$estoque"] } }
    }
  },
  {
    $project: {
      _id: 0,
      plataforma: "$_id",
      quantidadeTitulos: 1,
      valorTotalInventario: 1
    }
  },
  { $sort: { plataforma: 1 } }
])
```

Esse trecho serve para estudar a lógica; a aplicação usa o driver C# com expressões tipadas. Decimal128 mantém o preço em formato numérico decimal no MongoDB, para permitir a multiplicação e soma com precisão decimal.

## Teste automatizado de integração

Com a API e o banco ativos, na pasta da solução:

```bash
python3 tests/testar_api.py
```

No Windows, você pode usar `py tests/testar_api.py`. Para outra URL, passe-a como argumento. O script usa somente a biblioteca padrão do Python e valida CRUD, Location, dados persistidos, filtro, limites, precisão decimal, agrupamento, atualização do relatório e respostas 400/404. Ele cria plataformas exclusivas e remove apenas os documentos que criou.

