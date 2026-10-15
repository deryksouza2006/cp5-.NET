using System.ComponentModel.DataAnnotations;
using CatalogoJogos.Api.DTOs;
using CatalogoJogos.Api.Models;
using CatalogoJogos.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogoJogos.Api.Controllers;

[ApiController]
[Route("api/jogos")]
[Produces("application/json")]
public sealed class JogosController(IJogoService service) : ControllerBase
{
    private const string ObjectIdPattern = "^[0-9a-fA-F]{24}$";
    private const string ObjectIdError = "O ID deve conter 24 caracteres hexadecimais.";

    /// <summary>Lista todos os jogos, ordenados pelo título.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Jogo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Jogo>>> Listar(CancellationToken cancellationToken) =>
        Ok(await service.ListarAsync(cancellationToken));

    /// <summary>Busca um jogo pelo ObjectId.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Jogo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Jogo>> BuscarPorId(
        [RegularExpression(ObjectIdPattern, ErrorMessage = ObjectIdError)] string id,
        CancellationToken cancellationToken)
    {
        var jogo = await service.BuscarPorIdAsync(id, cancellationToken);
        return jogo is null ? NaoEncontrado() : Ok(jogo);
    }

    /// <summary>Cadastra um jogo e devolve o recurso com seu ID e cabeçalho Location.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Jogo), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Jogo>> Criar([FromBody] JogoRequest request, CancellationToken cancellationToken)
    {
        var jogo = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(BuscarPorId), new { id = jogo.Id }, jogo);
    }

    /// <summary>Substitui todos os dados de um jogo existente, preservando seu ID.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Jogo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Jogo>> Atualizar(
        [RegularExpression(ObjectIdPattern, ErrorMessage = ObjectIdError)] string id,
        [FromBody] JogoRequest request, CancellationToken cancellationToken)
    {
        var jogo = await service.AtualizarAsync(id, request, cancellationToken);
        return jogo is null ? NaoEncontrado() : Ok(jogo);
    }

    /// <summary>Exclui um jogo existente.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(
        [RegularExpression(ObjectIdPattern, ErrorMessage = ObjectIdError)] string id,
        CancellationToken cancellationToken) =>
        await service.ExcluirAsync(id, cancellationToken) ? NoContent() : NaoEncontrado();

    /// <summary>Filtra pela plataforma exata e por preço menor ou igual ao limite.</summary>
    [HttpGet("busca")]
    [ProducesResponseType(typeof(List<Jogo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Jogo>>> Buscar(
        [FromQuery] BuscaJogosRequest request, CancellationToken cancellationToken) =>
        Ok(await service.BuscarAsync(request, cancellationToken));

    /// <summary>Agrupa por plataforma: total de títulos e soma de preço × estoque.</summary>
    [HttpGet("relatorio-estoque")]
    [ProducesResponseType(typeof(List<RelatorioEstoqueResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RelatorioEstoqueResponse>>> RelatorioEstoque(CancellationToken cancellationToken) =>
        Ok(await service.RelatorioEstoqueAsync(cancellationToken));

    private ObjectResult NaoEncontrado() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Jogo não encontrado.");
}
