using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Post
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string BlogId { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public List<string> Tags { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}