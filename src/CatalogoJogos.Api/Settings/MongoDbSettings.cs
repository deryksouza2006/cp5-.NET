using System.ComponentModel.DataAnnotations;

namespace CatalogoJogos.Api.Settings;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    public string CollectionName { get; set; } = string.Empty;
}
