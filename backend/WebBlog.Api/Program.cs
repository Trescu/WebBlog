using Microsoft.EntityFrameworkCore;
using WebBlog.Api.Data;
using WebBlog.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlite("Data Source=blog.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");
app.UseDefaultFiles();
app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
    db.Database.EnsureCreated();

    if (!db.Posts.Any())
    {
        var firstPost = new Post
        {
            Title = "Első bejegyzés",
            Content = "Ez az első teszt bejegyzés tartalma. Innen fogjuk felépíteni a blogplatformot.",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var secondPost = new Post
        {
            Title = "Második bejegyzés",
            Content = "Ez a második bejegyzés. Ehhez már alapból tartozik néhány komment is a teszteléshez.",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        secondPost.Comments.Add(new Comment
        {
            AuthorName = "Anna",
            Text = "Nagyon hasznos bejegyzés.",
            CreatedAt = DateTime.UtcNow.AddHours(-10)
        });

        secondPost.Comments.Add(new Comment
        {
            AuthorName = "Péter",
            Text = "Kipróbáltam, működik.",
            CreatedAt = DateTime.UtcNow.AddHours(-5)
        });

        db.Posts.Add(firstPost);
        db.Posts.Add(secondPost);
        db.SaveChanges();
    }
}

app.MapGet("/api/posts", async (BlogDbContext db) =>
{
    var posts = await db.Posts
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new PostListItemDto(
            p.Id,
            p.Title,
            p.Content.Length > 180 ? p.Content.Substring(0, 180) + "..." : p.Content,
            p.CreatedAt,
            p.Comments.Count
        ))
        .ToListAsync();

    return Results.Ok(posts);
});

app.MapGet("/api/posts/{id:int}", async (int id, BlogDbContext db) =>
{
    var post = await db.Posts
        .Where(p => p.Id == id)
        .Select(p => new PostDetailDto(
            p.Id,
            p.Title,
            p.Content,
            p.CreatedAt,
            p.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto(
                    c.Id,
                    c.AuthorName,
                    c.Text,
                    c.CreatedAt
                ))
                .ToList()
        ))
        .FirstOrDefaultAsync();

    return post is null
        ? Results.NotFound(new { message = "A bejegyzés nem található." })
        : Results.Ok(post);
});

app.MapPost("/api/posts/{id:int}/comments", async (int id, CreateCommentRequest request, BlogDbContext db) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.AuthorName))
    {
        errors["authorName"] = new[] { "A név megadása kötelező." };
    }

    if (string.IsNullOrWhiteSpace(request.Text))
    {
        errors["text"] = new[] { "A komment szövege kötelező." };
    }

    if (request.AuthorName?.Length > 100)
    {
        errors["authorName"] = new[] { "A név legfeljebb 100 karakter lehet." };
    }

    if (request.Text?.Length > 1000)
    {
        errors["text"] = new[] { "A komment legfeljebb 1000 karakter lehet." };
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var postExists = await db.Posts.AnyAsync(p => p.Id == id);
    if (!postExists)
    {
        return Results.NotFound(new { message = "A bejegyzés nem található." });
    }

    var comment = new Comment
    {
        PostId = id,
        AuthorName = request.AuthorName.Trim(),
        Text = request.Text.Trim(),
        CreatedAt = DateTime.UtcNow
    };

    db.Comments.Add(comment);
    await db.SaveChangesAsync();

    var result = new CommentDto(
        comment.Id,
        comment.AuthorName,
        comment.Text,
        comment.CreatedAt
    );

    return Results.Created($"/api/posts/{id}", result);
});

app.Run();

public record PostListItemDto(
    int Id,
    string Title,
    string Preview,
    DateTime CreatedAt,
    int CommentCount
);

public record CommentDto(
    int Id,
    string AuthorName,
    string Text,
    DateTime CreatedAt
);

public record PostDetailDto(
    int Id,
    string Title,
    string Content,
    DateTime CreatedAt,
    List<CommentDto> Comments
);

public record CreateCommentRequest(
    string AuthorName,
    string Text
);

public partial class Program { }