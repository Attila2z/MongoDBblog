using MongoDB.Driver;
public class MongoBlogRepository : IBlogRepository
{
    private readonly IMongoCollection<Blog> _blogs;

    private readonly IPostRepository _posts;

    public MongoBlogRepository(IMongoDatabase db, IPostRepository posts, PostSearchService search)
    {
        _blogs = db.GetCollection<Blog>("blogs");  // "blogs" = collection name in MongoDB
        _posts = posts;
    }

    public async Task<BlogWithPostsDto?> GetById(string id)
    {
        var blog = await _blogs.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (blog is null) return null;

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
    public async Task Delete(string id)
    {
        await _posts.DeleteByBlog(id);          // delete all posts first
        await _blogs.DeleteOneAsync(b => b.Id == id);  // then delete the blog
    }
}
