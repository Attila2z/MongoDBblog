public interface IBlogRepository
{
    Task<Blog?> GetById(string id);
    Task<List<Blog>> GetAll();
    Task<Blog> Create(Blog blog);
    Task Delete(string id);
}