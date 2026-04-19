
public interface IBlogRepository
{
    Task<BlogWithPostsDto?> GetById(string id);
    Task<List<Blog>> GetAll();
    Task<Blog> Create(Blog blog);
    Task Delete(string id);
}
