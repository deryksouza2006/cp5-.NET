using CatalogoJogos.Api.DTOs;
using CatalogoJogos.Api.Models;
using CatalogoJogos.Api.Repositories;
using MongoDB.Bson;

namespace CatalogoJogos.Api.Services;

public sealed class JogoService(IJogoRepository repository) : IJogoService
{
    public Task<List<Jogo>> ListarAsync(CancellationToken cancellationToken) =>
        repository.ListarAsync(cancellationToken);

    public Task<Jogo?> BuscarPorIdAsync(string id, CancellationToken cancellationToken) =>
        repository.BuscarPorIdAsync(id, cancellationToken);

    public async Task<Jogo> CriarAsync(JogoRequest request, CancellationToken cancellationToken)
    {
        var jogo = Mapear(request, ObjectId.GenerateNewId().ToString());
        await repository.CriarAsync(jogo, cancellationToken);
        return jogo;
    }

    public Task<Jogo?> AtualizarAsync(string id, JogoRequest request, CancellationToken cancellationToken) =>
        repository.AtualizarAsync(id, Mapear(request, ObjectId.Parse(id).ToString()), cancellationToken);

    public Task<bool> ExcluirAsync(string id, CancellationToken cancellationToken) =>
        repository.ExcluirAsync(id, cancellationToken);

    public Task<List<Jogo>> BuscarAsync(BuscaJogosRequest request, CancellationToken cancellationToken) =>
        repository.BuscarAsync(request.Plataforma.Trim(), request.PrecoMaximo!.Value, cancellationToken);

    public Task<List<RelatorioEstoqueResponse>> RelatorioEstoqueAsync(CancellationToken cancellationToken) =>
        repository.RelatorioEstoqueAsync(cancellationToken);

    private static Jogo Mapear(JogoRequest request, string id) => new()
    {
        Id = id,
        Titulo = request.Titulo.Trim(),
        Plataforma = request.Plataforma.Trim(),
        Genero = request.Genero.Trim(),
        Preco = request.Preco!.Value,
        AnoLancamento = request.AnoLancamento!.Value,
        Estoque = request.Estoque!.Value
    };
}
