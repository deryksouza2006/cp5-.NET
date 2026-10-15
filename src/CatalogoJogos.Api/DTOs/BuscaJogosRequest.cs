using System.ComponentModel.DataAnnotations;

namespace CatalogoJogos.Api.DTOs;

public sealed class BuscaJogosRequest : IValidatableObject
{
    [Required(ErrorMessage = "Informe a plataforma.")]
    [StringLength(100)]
    public string Plataforma { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o preço máximo.")]
    public decimal? PrecoMaximo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PrecoMaximo is < 0)
            yield return new ValidationResult("O preço máximo não pode ser negativo.", [nameof(PrecoMaximo)]);
    }
}
