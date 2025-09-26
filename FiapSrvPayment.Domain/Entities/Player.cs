
using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace FiapSrvPayment.Domain.Entities;

[ExcludeFromCodeCoverage]
public class Player : User
{
    public string Cpf { get; set; }
    [BsonRepresentation(BsonType.String)]
    public List<Guid> Library { get; set; } = new();
    [BsonRepresentation(BsonType.String)]
    public List<Guid> Cart { get; set; } = new();
    [BsonRepresentation(BsonType.String)]
    public List<Guid> Wishlist { get; set; } = new();
}
