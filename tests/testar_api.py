"""Teste de integração por HTTP. Execute com a API e o MongoDB iniciados.

Uso: python3 tests/testar_api.py [http://localhost:5080]
Cria dados com plataformas exclusivas e remove apenas os IDs criados no teste.
Usa somente a biblioteca padrão do Python 3.
"""
import json
import re
import sys
import uuid
from decimal import Decimal
from urllib.error import HTTPError
from urllib.parse import urlencode
from urllib.request import Request, urlopen

BASE = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5080").rstrip("/")
IDS = []
TOTAL = 0


def verificar(condicao, descricao):
    global TOTAL
    if not condicao:
        raise AssertionError(descricao)
    TOTAL += 1
    print(f"OK {TOTAL:02d}: {descricao}")


def requisicao(metodo, caminho, corpo=None):
    payload = json.dumps(corpo).encode() if corpo is not None else None
    req = Request(BASE + caminho, data=payload, method=metodo,
                  headers={"Content-Type": "application/json"})
    try:
        resposta = urlopen(req, timeout=15)
    except HTTPError as erro:
        resposta = erro
    with resposta:
        conteudo = resposta.read()
        return resposta.status, json.loads(conteudo, parse_float=Decimal) if conteudo else None, resposta.headers


def executar():
    tag = uuid.uuid4().hex[:12]
    plataforma_a, plataforma_b = f"CP5-A-{tag}", f"CP5-B-{tag}"
    dados = [
        dict(titulo="Corrida Aurora", plataforma=plataforma_a, genero="Corrida", preco=79.90, anoLancamento=2010, estoque=3),
        dict(titulo="Lendas do Horizonte", plataforma=plataforma_a, genero="RPG", preco=120.00, anoLancamento=2012, estoque=2),
        dict(titulo="Ilha dos Enigmas", plataforma=plataforma_b, genero="Aventura", preco=59.90, anoLancamento=2009, estoque=4),
        dict(titulo="Teste sem estoque", plataforma=plataforma_a, genero="Ação", preco=300.00, anoLancamento=2020, estoque=0),
    ]
    status, swagger, _ = requisicao("GET", "/swagger/v1/swagger.json")
    verificar(status == 200 and len(swagger["paths"]) == 4, "OpenAPI contém as quatro rotas e sete operações")
    verificar(sum(len(p) for p in swagger["paths"].values()) == 7, "Sete endpoints documentados")

    for dado in dados:
        status, jogo, headers = requisicao("POST", "/api/jogos", dado)
        if status == 201:
            IDS.append(jogo["id"])
        verificar(status == 201 and re.fullmatch(r"[0-9a-f]{24}", jogo["id"]) is not None, "POST retorna 201 e ObjectId")
        verificar(headers.get("Location", "").endswith("/api/jogos/" + jogo["id"]), "POST retorna Location do recurso")

    status, lista, _ = requisicao("GET", "/api/jogos")
    verificar(status == 200 and set(IDS).issubset({j["id"] for j in lista}), "GET lista os jogos persistidos")
    status, jogo, _ = requisicao("GET", "/api/jogos/" + IDS[0].upper())
    verificar(status == 200 and jogo["preco"] == Decimal("79.90"), "GET por ID e precisão decimal")

    query = urlencode({"plataforma": plataforma_a, "precoMaximo": "79.90"})
    status, resultado, _ = requisicao("GET", "/api/jogos/busca?" + query)
    verificar(status == 200 and [j["id"] for j in resultado] == [IDS[0]], "Filtro combina plataforma e preço, incluindo o limite")
    status, vazio, _ = requisicao("GET", "/api/jogos/busca?" + urlencode({"plataforma": plataforma_a, "precoMaximo": "0"}))
    verificar(status == 200 and vazio == [], "Filtro sem resultados retorna 200 com lista vazia")

    status, relatorio, _ = requisicao("GET", "/api/jogos/relatorio-estoque")
    verificar(status == 200, "Relatório executa agregação no MongoDB")
    grupos = {r["plataforma"]: r for r in relatorio}
    verificar(grupos[plataforma_a]["quantidadeTitulos"] == 3 and grupos[plataforma_a]["valorTotalInventario"] == Decimal("479.70"), "Plataforma A: 3 títulos e inventário 479,70; estoque zero não soma valor")
    verificar(grupos[plataforma_b]["quantidadeTitulos"] == 1 and grupos[plataforma_b]["valorTotalInventario"] == Decimal("239.60"), "Plataforma B: 1 título e inventário 239,60")

    atualizado = dict(dados[0], titulo="Corrida Aurora — edição especial", preco=89.90, estoque=5)
    status, jogo, _ = requisicao("PUT", "/api/jogos/" + IDS[0].upper(), atualizado)
    verificar(status == 200 and jogo["id"] == IDS[0] and jogo["estoque"] == 5, "PUT retorna 200, dados novos e ID preservado")
    status, jogo, _ = requisicao("GET", "/api/jogos/" + IDS[0])
    verificar(status == 200 and jogo["titulo"] == atualizado["titulo"] and jogo["preco"] == Decimal("89.90"), "Atualização persistida no banco")
    _, relatorio, _ = requisicao("GET", "/api/jogos/relatorio-estoque")
    grupo_a = next(r for r in relatorio if r["plataforma"] == plataforma_a)
    verificar(grupo_a["valorTotalInventario"] == Decimal("689.50"), "Relatório reflete preço e estoque atualizados")

    inexistente = uuid.uuid4().hex[:24]
    for metodo, corpo in [("GET", None), ("PUT", atualizado), ("DELETE", None)]:
        status, _, _ = requisicao(metodo, "/api/jogos/" + inexistente, corpo)
        verificar(status == 404, f"{metodo} com ObjectId inexistente retorna 404")
        status, _, _ = requisicao(metodo, "/api/jogos/id-invalido", corpo)
        verificar(status == 400, f"{metodo} com ID inválido retorna 400")
    status, _, _ = requisicao("GET", "/api/jogos/" + inexistente)
    verificar(status == 404, "PUT inexistente não cria jogo (sem upsert)")

    for campo, valor in [("titulo", "   "), ("plataforma", ""), ("genero", ""), ("preco", -1), ("estoque", -1), ("anoLancamento", 0)]:
        for metodo, caminho in [("POST", "/api/jogos"), ("PUT", "/api/jogos/" + IDS[0])]:
            status, _, _ = requisicao(metodo, caminho, dict(dados[0], **{campo: valor}))
            verificar(status == 400, f"{metodo} rejeita {campo} inválido")
    for campo in ["preco", "estoque", "anoLancamento"]:
        incompleto = dados[0].copy()
        del incompleto[campo]
        status, _, _ = requisicao("POST", "/api/jogos", incompleto)
        verificar(status == 400, f"POST rejeita ausência de {campo}")
    for consulta in ["", "plataforma=PC", "precoMaximo=50", "plataforma=PC&precoMaximo=-1", "plataforma=PC&precoMaximo=abc"]:
        status, _, _ = requisicao("GET", "/api/jogos/busca?" + consulta)
        verificar(status == 400, f"Busca rejeita parâmetros inválidos: {consulta or '(ausentes)'}")

    status, corpo, _ = requisicao("DELETE", "/api/jogos/" + IDS[0])
    verificar(status == 204 and corpo is None, "DELETE retorna 204 sem corpo")
    status, _, _ = requisicao("GET", "/api/jogos/" + IDS[0])
    verificar(status == 404, "GET após exclusão retorna 404")
    status, _, _ = requisicao("DELETE", "/api/jogos/" + IDS[0])
    verificar(status == 404, "Nova exclusão retorna 404")


if __name__ == "__main__":
    try:
        executar()
    finally:
        for jogo_id in IDS:
            requisicao("DELETE", "/api/jogos/" + jogo_id)
    print(f"\n{TOTAL} verificações concluídas com sucesso.")
