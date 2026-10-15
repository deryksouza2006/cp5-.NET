using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CatalogoJogos.Api.DTOs;

public sealed class RelatorioEstoqueResponse
{
    public string Plataforma { get; set; } = string.Empty;
    public int QuantidadeTitulos { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal ValorTotalInventario { get; set; }
}
