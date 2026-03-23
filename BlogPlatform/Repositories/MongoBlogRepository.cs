using MongoDB.Driver;

// MongoDBDriver lives here only.
// The rest of the application depends on IBlogRepository interface,
// not this implementation. This means we can swap MongoDB for
// another database by only changing this file and Program.cs.
public class MongoBlogRepository : IBlogRepository
{
    private readonly IMongoCollection<Blog> _blogs;
    private readonly IPostRepository _posts;

    public MongoBlogRepository(IMongoDatabase db, IPostRepository posts)
    {
        _blogs = db.GetCollection<Blog>("blogs");
        _posts = posts;
    }

    // Returns the blog AND all its posts together in one response
    public async Task<BlogWithPostsDto?> GetById(string id)
    {
        var blog = await _blogs.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (blog is null) return null;

        // Fetch all posts belonging to this blog
        var posts = await _posts.GetByBlog(id);

        return new BlogWithPostsDto
        {
            Id = blog.Id,
            Name = blog.Name,
            UserId = blog.UserId,
            Posts = posts
        };
    }

    public async Task<List<Blog>> GetAll()
        => await _blogs.Find(_ => true).ToListAsync();

    public async Task<Blog> Create(Blog blog)
    {
        await _blogs.InsertOneAsync(blog);
        return blog;
    }

    // MongoDB has no cascading deletes — we must manually delete
    // all posts belonging to this blog before deleting the blog itself.
    // If we only delete the blog, the posts would be orphaned in the
    // database forever with no blog to belong to.
    public async Task Delete(string id)
    {
        await _posts.DeleteByBlog(id);
        await _blogs.DeleteOneAsync(b => b.Id == id);
    }
}