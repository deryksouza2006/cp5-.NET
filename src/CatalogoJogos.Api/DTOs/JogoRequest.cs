using System.ComponentModel.DataAnnotations;

namespace CatalogoJogos.Api.DTOs;

// O ID vem da rota no PUT e é gerado no servidor no POST.
public sealed class JogoRequest : IValidatableObject
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A plataforma é obrigatória.")]
    [StringLength(100)]
    public string Plataforma { get; set; } = string.Empty;

    [Required(ErrorMessage = "O gênero é obrigatório.")]
    [StringLength(80)]
    public string Genero { get; set; } = string.Empty;

    [Required(ErrorMessage = "O preço é obrigatório.")]
    public decimal? Preco { get; set; }

    [Required(ErrorMessage = "O ano de lançamento é obrigatório.")]
    [Range(1, 9999, ErrorMessage = "Informe um ano entre 1 e 9999.")]
    public int? AnoLancamento { get; set; }

    [Required(ErrorMessage = "O estoque é obrigatório.")]
    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo.")]
    public int? Estoque { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Preco is < 0)
            yield return new ValidationResult("O preço não pode ser negativo.", [nameof(Preco)]);
    }
}
