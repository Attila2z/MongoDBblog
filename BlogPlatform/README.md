# Week 11 — MongoDB Blogging Platform

---



### Decision summary

| Relationship | Decision | Key reason | Trade-off |
|---|---|---|---|
| User → Blog | Reference | Blogs queried independently | User doc stays small |
| Blog → Post | Reference | Posts grow unbounded over time | Requires separate query to list posts |
| Post → Comment | Embed | Always read with post, never alone | Large doc if post goes viral |
| Comment → User | Reference (userId) | User is shared across many comments | Name change needs no backfill |

---

### User → Blog (Reference)

Blogs are stored as separate documents with a `userId` field pointing back to the owner. Blogs are frequently queried independently — listing all blogs, or fetching a single blog — without needing user details at the same time. Embedding all of a user's blogs inside the user document would also cause it to grow unboundedly and make independent blog queries impossible without scanning every user.

### Blog → Post (Reference)

Posts live in their own collection with a `blogId` reference. A blog accumulates posts over time — potentially hundreds — so embedding them would eventually hit MongoDB's 16 MB document limit. Posts are also frequently read individually (a single post page) or listed in summary form (title + excerpt), both of which suit a separate collection with targeted queries.

### Post → Comment (Embed)

Comments are embedded as an array inside the post document. They are always shown in the context of their post and never queried independently, so embedding gives a single read operation to fetch a post and all its comments.

**trade-off:** a viral post with thousands of comments could grow the document very large. For this course project that is acceptable. In production, comments would move to their own collection once a post exceeded a few hundred.

### Comment → User (Reference)

Even though comments are embedded inside the post, the comment author is stored only as a `userId` reference. A user is shared data — they may author dozens of comments across many posts. Embedding a copy of their username in each comment would require updating every comment if the user changed their display name. Referencing keeps the data consistent by definition.

---

## Task 2 — Implementation

### Architecture

The application follows a strict four-layer architecture. 

```
API Layer          — Controllers, request/response DTOs
    │
Service Layer      — Business logic, Redis (cache + search + rate limiting)
    │
Repository Layer   — Interfaces + MongoDB implementations
    │
MongoDB Driver     — Only the repository layer sees this
```

This means swapping MongoDB for PostgreSQL requires writing new repository classes and changing a few lines in `Program.cs`. Controllers and services stay completely untouched.

### Project structure

```
BlogPlatform/
├── Controllers/
│   ├── BlogController.cs
│   └── PostController.cs
├── Models/
│   ├── Blog.cs
│   ├── Post.cs
│   ├── Comment.cs
│   └── BlogWithPostsDto.cs
├── Repositories/
│   ├── IBlogRepository.cs
│   ├── IPostRepository.cs
│   ├── MongoBlogRepository.cs
│   └── MongoPostRepository.cs
├── Services/
│   ├── PostCacheService.cs
│   └── PostSearchService.cs
├── Program.cs
└── appsettings.Development.json
```

---

### Key implementation decisions

#### `[BsonRepresentation(BsonType.ObjectId)]` on every Id field

This attribute tells the MongoDB driver to store the value as an `ObjectId` internally while exposing it as a plain C# `string`. Without it, string-to-ObjectId comparisons fail silently and queries return nothing. It is applied to every Id field including foreign keys like `BlogId`.

```csharp
[BsonId]
[BsonRepresentation(BsonType.ObjectId)]
public string? Id { get; set; }

[BsonRepresentation(BsonType.ObjectId)]
public string? BlogId { get; set; }
```

#### Atomic `$push` for adding comments

New comments are appended using MongoDB's `UpdateOne` with a `$push` operator rather than fetching the document, modifying the list in C#, and saving the whole document back. This is a single atomic database operation — no unnecessary read, no race condition if two users comment simultaneously.

```csharp
await _posts.UpdateOneAsync(
    p => p.Id == postId,
    Builders<Post>.Update.Push(p => p.Comments, comment));
```

#### Manual cascade delete

MongoDB has no cascading deletes. When a blog is deleted, the repository explicitly deletes all posts in that blog first, then deletes the blog document. If this were skipped, posts would remain in the database forever with no parent (orphaned data).

```csharp
public async Task Delete(string id)
{
    await _posts.DeleteByBlog(id);           // delete all posts first
    await _blogs.DeleteOneAsync(b => b.Id == id);  // then delete the blog
}
```

> **Known limitation:** this is not atomic. If `DeleteByBlog` succeeds but the blog delete fails, posts are gone but the blog still exists. A production fix would use a MongoDB multi-document transaction.

#### `BlogWithPostsDto`

The `Blog` document in MongoDB stores only `Id`, `Name`, and `UserId` — not its posts. When a client requests `GET /api/blogs/{id}` they expect the blog and its posts together. Rather than changing the `Blog` model (which would break how it maps to MongoDB), the repository fetches both separately and combines them into a `BlogWithPostsDto` before returning the response. This keeps the domain model clean.

---

### API endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/blogs` | Return all blogs |
| `POST` | `/api/blogs` | Create a new blog |
| `GET` | `/api/blogs/{id}` | Get blog with all its posts |
| `DELETE` | `/api/blogs/{id}` | Delete blog and all its posts |
| `POST` | `/api/blogs/{blogId}/posts` | Create a post under a blog |
| `GET` | `/api/posts/{id}` | Get a single post (Redis cache-aside) |
| `PUT` | `/api/posts/{id}` | Update a post (invalidates cache) |
| `DELETE` | `/api/posts/{id}` | Delete a post |
| `POST` | `/api/posts/{id}/comments` | Add a comment (atomic `$push`, rate-limited) |
| `GET` | `/api/posts/search?q=keyword` | Full-text search via Redis |

---

## Running the project

### 1. Start MongoDB

```bash
docker run --name mongo -p 27017:27017 -d mongodb/mongodb-community-server:7.0.5-ubuntu2204
```

### 2. Start Redis

```bash
docker run --name redis -p 6379:6379 -d redis/redis-stack:latest
```

### 3. Run the API

```bash
dotnet run --project BlogPlatform
```

Swagger UI is available at `https://localhost:{port}/swagger` in Development mode.

### appsettings.Development.json

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "blog-platform"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```