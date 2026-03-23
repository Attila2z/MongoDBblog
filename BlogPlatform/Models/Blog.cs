using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Blog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }  // nullable — MongoDB generates this

    public string Name { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;
}