using CatalogoJogos.Api.DTOs;
using CatalogoJogos.Api.Models;

namespace CatalogoJogos.Api.Repositories;

public interface IJogoRepository
{
    Task<List<Jogo>> ListarAsync(CancellationToken cancellationToken);
    Task<Jogo?> BuscarPorIdAsync(string id, CancellationToken cancellationToken);
    Task CriarAsync(Jogo jogo, CancellationToken cancellationToken);
    Task<Jogo?> AtualizarAsync(string id, Jogo jogo, CancellationToken cancellationToken);
    Task<bool> ExcluirAsync(string id, CancellationToken cancellationToken);
    Task<List<Jogo>> BuscarAsync(string plataforma, decimal precoMaximo, CancellationToken cancellationToken);
    Task<List<RelatorioEstoqueResponse>> RelatorioEstoqueAsync(CancellationToken cancellationToken);
}
