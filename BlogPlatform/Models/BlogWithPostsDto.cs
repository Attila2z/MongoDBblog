
public class BlogWithPostsDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? UserId { get; set; }
    public List<Post> Posts { get; set; } = [];
}
