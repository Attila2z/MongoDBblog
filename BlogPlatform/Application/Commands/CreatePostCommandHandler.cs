public class CreatePostCommandHandler
{
    private readonly IPostRepository _mongo;
    private readonly SqlPostRepository _sql;

    public CreatePostCommandHandler(IPostRepository mongo, SqlPostRepository sql)
    {
        _mongo = mongo;
        _sql = sql;
    }

    public async Task<Post> HandleAsync(CreatePostCommand command)
{
    var post = new Post
    {
        BlogId = command.BlogId,
        Title = command.Title,
        Body = command.Body
    };
    var created = await _mongo.Create(post);

    Console.WriteLine($"Saving to SQL: MongoId={created.Id}");

    try
    {
        await _sql.SaveAsync(new PostEntity
        {
            MongoId = created.Id!,
            BlogId = command.BlogId,
            Title = command.Title,
            Body = command.Body
        });
        Console.WriteLine("SQL save done.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"SQL ERROR: {ex.Message}");
        Console.WriteLine(ex.InnerException?.Message);
    }

    return created;
}
}