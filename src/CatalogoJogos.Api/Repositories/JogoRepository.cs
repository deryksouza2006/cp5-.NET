using CatalogoJogos.Api.DTOs;
using CatalogoJogos.Api.Models;
using CatalogoJogos.Api.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CatalogoJogos.Api.Repositories;

public sealed class JogoRepository : IJogoRepository
{
    private readonly IMongoCollection<Jogo> _jogos;

    public JogoRepository(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        _jogos = database.GetCollection<Jogo>(options.Value.CollectionName);
    }

    public Task<List<Jogo>> ListarAsync(CancellationToken cancellationToken) =>
        _jogos.Find(Builders<Jogo>.Filter.Empty)
            .SortBy(jogo => jogo.Titulo).ToListAsync(cancellationToken);

    public Task<Jogo?> BuscarPorIdAsync(string id, CancellationToken cancellationToken) =>
        _jogos.Find(jogo => jogo.Id == id).FirstOrDefaultAsync(cancellationToken)!;

    public Task CriarAsync(Jogo jogo, CancellationToken cancellationToken) =>
        _jogos.InsertOneAsync(jogo, cancellationToken: cancellationToken);

    public Task<Jogo?> AtualizarAsync(string id, Jogo jogo, CancellationToken cancellationToken) =>
        // Uma única operação: devolve o documento atualizado ou null se não existir.
        _jogos.FindOneAndReplaceAsync(
            Builders<Jogo>.Filter.Eq(item => item.Id, id),
            jogo,
            new FindOneAndReplaceOptions<Jogo> { ReturnDocument = ReturnDocument.After, IsUpsert = false },
            cancellationToken)!;

    public async Task<bool> ExcluirAsync(string id, CancellationToken cancellationToken)
    {
        var resultado = await _jogos.DeleteOneAsync(jogo => jogo.Id == id, cancellationToken);
        return resultado.DeletedCount > 0;
    }

    public Task<List<Jogo>> BuscarAsync(string plataforma, decimal precoMaximo, CancellationToken cancellationToken)
    {
        var filtro = Builders<Jogo>.Filter.And(
            Builders<Jogo>.Filter.Eq(jogo => jogo.Plataforma, plataforma),
            Builders<Jogo>.Filter.Lte(jogo => jogo.Preco, precoMaximo));

        return _jogos.Find(filtro).SortBy(jogo => jogo.Preco).ToListAsync(cancellationToken);
    }

    public Task<List<RelatorioEstoqueResponse>> RelatorioEstoqueAsync(CancellationToken cancellationToken) =>
        // O driver traduz Group/Count/Sum em $group/$sum/$multiply no MongoDB.
        // O cálculo ocorre no banco, sem carregar o catálogo para agrupar na API.
        _jogos.Aggregate()
            .Group(jogo => jogo.Plataforma, grupo => new RelatorioEstoqueResponse
            {
                Plataforma = grupo.Key,
                QuantidadeTitulos = grupo.Count(),
                ValorTotalInventario = grupo.Sum(jogo => jogo.Preco * jogo.Estoque)
            })
            .SortBy(item => item.Plataforma)
            .ToListAsync(cancellationToken);
}
