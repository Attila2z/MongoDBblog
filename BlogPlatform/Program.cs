using MongoDB.Driver;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Register MongoDB client as singleton — one connection shared across the app
builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(builder.Configuration["MongoDB:ConnectionString"]));

// Register MongoDB database as scoped
builder.Services.AddScoped<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>()
      .GetDatabase(builder.Configuration["MongoDB:Database"]));

// Register repositories — swap these lines to change databases
builder.Services.AddScoped<IPostRepository, MongoPostRepository>();
builder.Services.AddScoped<IBlogRepository, MongoBlogRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Redis connection as singleton
builder.Services.AddSingleton<IConnectionMultiplexer>(
ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));
builder.Services.AddSingleton<PostCacheService>();
builder.Services.AddSingleton<PostSearchService>();
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