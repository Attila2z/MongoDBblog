using MongoDB.Driver;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// MONGODB SETUP 

builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(builder.Configuration["MongoDB:ConnectionString"]));

// IMongoDatabase is SCOPED — a new instance per HTTP request.
// This is lightweight (just a reference to the client + database name) so the
// overhead is minimal, and scoping it ensures each request gets a clean context.
builder.Services.AddScoped<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>()
      .GetDatabase(builder.Configuration["MongoDB:Database"]));

// REPOSITORIES 
builder.Services.AddScoped<IBlogRepository, MongoBlogRepository>();
builder.Services.AddScoped<IPostRepository, MongoPostRepository>();

//  REDIS SETUP
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

// Redis services ingletons
builder.Services.AddSingleton<PostCacheService>();
builder.Services.AddSingleton<PostSearchService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();  // Swagger UI available at /swagger in Development

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
