using CatalogoJogos.Api.DTOs;
using CatalogoJogos.Api.Models;

namespace CatalogoJogos.Api.Services;

public interface IJogoService
{
    Task<List<Jogo>> ListarAsync(CancellationToken cancellationToken);
    Task<Jogo?> BuscarPorIdAsync(string id, CancellationToken cancellationToken);
    Task<Jogo> CriarAsync(JogoRequest request, CancellationToken cancellationToken);
    Task<Jogo?> AtualizarAsync(string id, JogoRequest request, CancellationToken cancellationToken);
    Task<bool> ExcluirAsync(string id, CancellationToken cancellationToken);
    Task<List<Jogo>> BuscarAsync(BuscaJogosRequest request, CancellationToken cancellationToken);
    Task<List<RelatorioEstoqueResponse>> RelatorioEstoqueAsync(CancellationToken cancellationToken);
}
