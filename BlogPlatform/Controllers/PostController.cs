using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class PostController : ControllerBase
{
    private readonly IPostRepository _posts;

    public PostController(IPostRepository posts)
    {
        _posts = posts;
    }

    // POST api/blogs/{blogId}/posts
    [HttpPost("blogs/{blogId}/posts")]
    public async Task<IActionResult> Create(string blogId, Post post)
    {
        // Assign the blogId from the route to the post
        post.BlogId = blogId;
        var created = await _posts.Create(post);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // GET api/posts/{id}
    [HttpGet("posts/{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var post = await _posts.GetById(id);
        if (post is null) return NotFound();
        return Ok(post);
    }

    // PUT api/posts/{id}
    [HttpPut("posts/{id}")]
    public async Task<IActionResult> Update(string id, Post post)
    {
        // Make sure the id from the route is used
        post.Id = id;
        await _posts.Update(post);
        return NoContent();
    }

    // DELETE api/posts/{id}
    [HttpDelete("posts/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _posts.Delete(id);
        return NoContent();
    }

    // POST api/posts/{id}/comments
    [HttpPost("posts/{id}/comments")]
    public async Task<IActionResult> AddComment(string id, Comment comment)
    {
        // Pushes comment into the embedded array inside the post
        // No need to fetch the whole post first
        await _posts.AddComment(id, comment);
        return NoContent();
    }
}