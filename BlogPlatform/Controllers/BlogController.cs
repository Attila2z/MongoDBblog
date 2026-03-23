using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/blogs")]
public class BlogController : ControllerBase
{
    private readonly IBlogRepository _blogs;

    public BlogController(IBlogRepository blogs)
    {
        _blogs = blogs;
    }

    // GET api/blogs/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var blog = await _blogs.GetById(id);
        if (blog is null) return NotFound();
        return Ok(blog);
    }

    // GET api/blogs
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var blogs = await _blogs.GetAll();
        return Ok(blogs);
    }

    // POST api/blogs
    [HttpPost]
    public async Task<IActionResult> Create(Blog blog)
    {
        var created = await _blogs.Create(blog);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // DELETE api/blogs/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        // Deletes the blog AND all its posts (handled in repository)
        await _blogs.Delete(id);
        return NoContent();
    }
}