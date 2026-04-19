using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("posts")]
public class PostEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string MongoId { get; set; } = null!;  // links back to MongoDB

    [Required]
    public string BlogId { get; set; } = null!;

    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Body { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}