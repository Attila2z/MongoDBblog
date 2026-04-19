public class GetPostQueryHandler
{
    private readonly IPostRepository _mongo;

    public GetPostQueryHandler(IPostRepository mongo)
    {
        _mongo = mongo;
    }

    // Reads always go to MongoDB
    public async Task<Post?> HandleAsync(GetPostQuery query)
        => await _mongo.GetById(query.PostId);
}