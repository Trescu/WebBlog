using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using WebBlog.Api.Data;
using WebBlog.Api.Models;
using WebBlog.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlite("Data Source=blog.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebBlog.Api",
        Version = "v1"
    });

    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = new List<string>()
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("A Jwt:Key nincs beállítva.");

string jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WebBlog.Api";
string jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WebBlog.Client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<JwtTokenService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any())
    {
        var adminPassword = PasswordHelper.HashPassword("Admin123!");
        var userPassword = PasswordHelper.HashPassword("User123!");

        var admin = new User
        {
            Username = "admin",
            Email = "admin@webblog.local",
            PasswordHash = adminPassword.hash,
            PasswordSalt = adminPassword.salt,
            Role = "Admin"
        };

        var user = new User
        {
            Username = "richi",
            Email = "richi@webblog.local",
            PasswordHash = userPassword.hash,
            PasswordSalt = userPassword.salt,
            Role = "User"
        };

        db.Users.Add(admin);
        db.Users.Add(user);
        db.SaveChanges();
    }

    if (!db.Posts.Any())
    {
        var adminUser = db.Users.First(u => u.Username == "admin");
        var normalUser = db.Users.First(u => u.Username == "richi");

        var firstPost = new Post
        {
            Title = "Első bejegyzés",
            Content = "Ez az első teszt bejegyzés tartalma. Innen fogjuk felépíteni a blogplatformot.",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            AuthorUserId = adminUser.Id,
            AuthorName = adminUser.Username
        };

        var secondPost = new Post
        {
            Title = "Második bejegyzés",
            Content = "Ez a második bejegyzés. Ehhez már alapból tartozik néhány komment is a teszteléshez.",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            AuthorUserId = normalUser.Id,
            AuthorName = normalUser.Username
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

app.MapGet("/api/posts", async (string? search, string? author, string? sort, BlogDbContext db) =>
{
    IQueryable<Post> query = db.Posts;

    if (!string.IsNullOrWhiteSpace(search))
    {
        string pattern = $"%{search.Trim()}%";

        query = query.Where(p =>
            EF.Functions.Like(p.Title, pattern) ||
            EF.Functions.Like(p.Content, pattern) ||
            EF.Functions.Like(p.AuthorName, pattern));
    }

    if (!string.IsNullOrWhiteSpace(author))
    {
        string normalizedAuthor = author.Trim();
        query = query.Where(p => p.AuthorName == normalizedAuthor);
    }

    query = sort switch
    {
        "oldest" => query.OrderBy(p => p.CreatedAt),
        "title_asc" => query.OrderBy(p => p.Title),
        "title_desc" => query.OrderByDescending(p => p.Title),
        "comments_desc" => query.OrderByDescending(p => p.Comments.Count).ThenByDescending(p => p.CreatedAt),
        _ => query.OrderByDescending(p => p.CreatedAt)
    };

    var posts = await query
        .Select(p => new PostListItemDto(
            p.Id,
            p.Title,
            p.Content.Length > 180 ? p.Content.Substring(0, 180) + "..." : p.Content,
            p.CreatedAt,
            p.Comments.Count,
            p.AuthorName
        ))
        .ToListAsync();

    return Results.Ok(posts);
});

app.MapGet("/api/posts/authors", async (BlogDbContext db) =>
{
    var authors = await db.Posts
        .Select(p => p.AuthorName)
        .Distinct()
        .OrderBy(name => name)
        .ToListAsync();

    return Results.Ok(authors);
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
            p.AuthorName,
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

app.MapPost("/api/posts/{id:int}/comments", async (ClaimsPrincipal user, int id, CreateCommentRequest request, BlogDbContext db) =>
{
    var errors = new Dictionary<string, string[]>();

    string? loggedInUsername = user.FindFirstValue(ClaimTypes.Name);
    bool isLoggedIn = !string.IsNullOrWhiteSpace(loggedInUsername);

    if (!isLoggedIn && string.IsNullOrWhiteSpace(request.AuthorName))
    {
        errors["authorName"] = new[] { "A név megadása kötelező." };
    }

    if (string.IsNullOrWhiteSpace(request.Text))
    {
        errors["text"] = new[] { "A komment szövege kötelező." };
    }

    if (!isLoggedIn && request.AuthorName is not null && request.AuthorName.Length > 100)
    {
        errors["authorName"] = new[] { "A név legfeljebb 100 karakter lehet." };
    }

    if (request.Text is not null && request.Text.Length > 1000)
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
        AuthorName = isLoggedIn ? loggedInUsername! : request.AuthorName!.Trim(),
        Text = request.Text!.Trim(),
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

app.MapPost("/api/auth/register", async (RegisterRequest request, BlogDbContext db, JwtTokenService jwtTokenService) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Username))
    {
        errors["username"] = new[] { "A felhasználónév kötelező." };
    }

    if (string.IsNullOrWhiteSpace(request.Email))
    {
        errors["email"] = new[] { "Az e-mail cím kötelező." };
    }

    if (string.IsNullOrWhiteSpace(request.Password))
    {
        errors["password"] = new[] { "A jelszó kötelező." };
    }

    if (!string.IsNullOrWhiteSpace(request.Username) && request.Username.Length > 50)
    {
        errors["username"] = new[] { "A felhasználónév legfeljebb 50 karakter lehet." };
    }

    if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.Length > 100)
    {
        errors["email"] = new[] { "Az e-mail cím legfeljebb 100 karakter lehet." };
    }

    if (!string.IsNullOrWhiteSpace(request.Password) && request.Password.Length < 6)
    {
        errors["password"] = new[] { "A jelszónak legalább 6 karakter hosszúnak kell lennie." };
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    string normalizedUsername = request.Username.Trim();
    string normalizedEmail = request.Email.Trim().ToLowerInvariant();

    bool usernameExists = await db.Users.AnyAsync(u => u.Username == normalizedUsername);
    if (usernameExists)
    {
        return Results.Conflict(new { message = "Ez a felhasználónév már foglalt." });
    }

    bool emailExists = await db.Users.AnyAsync(u => u.Email == normalizedEmail);
    if (emailExists)
    {
        return Results.Conflict(new { message = "Ez az e-mail cím már használatban van." });
    }

    var passwordData = PasswordHelper.HashPassword(request.Password);

    var user = new User
    {
        Username = normalizedUsername,
        Email = normalizedEmail,
        PasswordHash = passwordData.hash,
        PasswordSalt = passwordData.salt,
        Role = "User"
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    string token = jwtTokenService.CreateToken(user);

    return Results.Ok(new AuthResponse(
        token,
        user.Username,
        user.Email,
        user.Role
    ));
});

app.MapPost("/api/auth/login", async (LoginRequest request, BlogDbContext db, JwtTokenService jwtTokenService) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "A felhasználónév és a jelszó kötelező." });
    }

    string normalizedUsername = request.Username.Trim();

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    bool validPassword = PasswordHelper.VerifyPassword(
        request.Password,
        user.PasswordHash,
        user.PasswordSalt);

    if (!validPassword)
    {
        return Results.Unauthorized();
    }

    string token = jwtTokenService.CreateToken(user);

    return Results.Ok(new AuthResponse(
        token,
        user.Username,
        user.Email,
        user.Role
    ));
});

app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
{
    string? id = user.FindFirstValue(ClaimTypes.NameIdentifier);
    string? username = user.FindFirstValue(ClaimTypes.Name);
    string? email = user.FindFirstValue(ClaimTypes.Email);
    string? role = user.FindFirstValue(ClaimTypes.Role);

    return Results.Ok(new CurrentUserResponse(
        id ?? string.Empty,
        username ?? string.Empty,
        email ?? string.Empty,
        role ?? string.Empty
    ));
}).RequireAuthorization();

app.MapPost("/api/posts", async (ClaimsPrincipal user, CreatePostRequest request, BlogDbContext db) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        errors["title"] = new[] { "A cím kötelező." };
    }

    if (string.IsNullOrWhiteSpace(request.Content))
    {
        errors["content"] = new[] { "A tartalom kötelező." };
    }

    if (!string.IsNullOrWhiteSpace(request.Title) && request.Title.Length > 200)
    {
        errors["title"] = new[] { "A cím legfeljebb 200 karakter lehet." };
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    string? userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
    string? username = user.FindFirstValue(ClaimTypes.Name);

    if (!int.TryParse(userIdValue, out int userId) || string.IsNullOrWhiteSpace(username))
    {
        return Results.Unauthorized();
    }

    var post = new Post
    {
        Title = request.Title.Trim(),
        Content = request.Content.Trim(),
        CreatedAt = DateTime.UtcNow,
        AuthorUserId = userId,
        AuthorName = username
    };

    db.Posts.Add(post);
    await db.SaveChangesAsync();

    return Results.Created($"/api/posts/{post.Id}", new CreatePostResponse(
        post.Id,
        post.Title,
        post.Content,
        post.CreatedAt,
        post.AuthorName
    ));
}).RequireAuthorization();

app.MapGet("/api/my/posts", async (ClaimsPrincipal user, BlogDbContext db) =>
{
    string? userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
    string? role = user.FindFirstValue(ClaimTypes.Role);

    if (!int.TryParse(userIdValue, out int userId))
    {
        return Results.Unauthorized();
    }

    IQueryable<Post> query = db.Posts;

    if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
    {
        query = query.Where(p => p.AuthorUserId == userId);
    }

    var posts = await query
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new MyPostListItemDto(
            p.Id,
            p.Title,
            p.CreatedAt,
            p.AuthorName,
            p.Comments.Count
        ))
        .ToListAsync();

    return Results.Ok(posts);
}).RequireAuthorization();

app.MapPut("/api/posts/{id:int}", async (ClaimsPrincipal user, int id, UpdatePostRequest request, BlogDbContext db) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        errors["title"] = new[] { "A cím kötelező." };
    }

    if (string.IsNullOrWhiteSpace(request.Content))
    {
        errors["content"] = new[] { "A tartalom kötelező." };
    }

    if (!string.IsNullOrWhiteSpace(request.Title) && request.Title.Length > 200)
    {
        errors["title"] = new[] { "A cím legfeljebb 200 karakter lehet." };
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    string? userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
    string? role = user.FindFirstValue(ClaimTypes.Role);

    if (!int.TryParse(userIdValue, out int userId))
    {
        return Results.Unauthorized();
    }

    var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id);
    if (post is null)
    {
        return Results.NotFound(new { message = "A bejegyzés nem található." });
    }

    bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    bool isOwner = post.AuthorUserId == userId;

    if (!isAdmin && !isOwner)
    {
        return Results.Forbid();
    }

    post.Title = request.Title.Trim();
    post.Content = request.Content.Trim();

    await db.SaveChangesAsync();

    return Results.Ok(new CreatePostResponse(
        post.Id,
        post.Title,
        post.Content,
        post.CreatedAt,
        post.AuthorName
    ));
}).RequireAuthorization();

app.MapDelete("/api/posts/{id:int}", async (ClaimsPrincipal user, int id, BlogDbContext db) =>
{
    string? userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
    string? role = user.FindFirstValue(ClaimTypes.Role);

    if (!int.TryParse(userIdValue, out int userId))
    {
        return Results.Unauthorized();
    }

    var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id);
    if (post is null)
    {
        return Results.NotFound(new { message = "A bejegyzés nem található." });
    }

    bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    bool isOwner = post.AuthorUserId == userId;

    if (!isAdmin && !isOwner)
    {
        return Results.Forbid();
    }

    db.Posts.Remove(post);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/admin/posts", async (ClaimsPrincipal user, BlogDbContext db) =>
{
    string? role = user.FindFirstValue(ClaimTypes.Role);

    bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    if (!isAdmin)
    {
        return Results.Forbid();
    }

    var posts = await db.Posts
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new AdminPostListItemDto(
            p.Id,
            p.Title,
            p.AuthorName,
            p.CreatedAt,
            p.Comments.Count
        ))
        .ToListAsync();

    return Results.Ok(posts);
}).RequireAuthorization();

app.MapGet("/api/admin/comments", async (ClaimsPrincipal user, BlogDbContext db) =>
{
    string? role = user.FindFirstValue(ClaimTypes.Role);

    bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    if (!isAdmin)
    {
        return Results.Forbid();
    }

    var comments = await db.Comments
        .OrderByDescending(c => c.CreatedAt)
        .Select(c => new AdminCommentListItemDto(
            c.Id,
            c.PostId,
            c.Post != null ? c.Post.Title : "(ismeretlen bejegyzés)",
            c.AuthorName,
            c.Text,
            c.CreatedAt
        ))
        .ToListAsync();

    return Results.Ok(comments);
}).RequireAuthorization();

app.MapDelete("/api/admin/comments/{id:int}", async (ClaimsPrincipal user, int id, BlogDbContext db) =>
{
    string? role = user.FindFirstValue(ClaimTypes.Role);

    bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    if (!isAdmin)
    {
        return Results.Forbid();
    }

    var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id);
    if (comment is null)
    {
        return Results.NotFound(new { message = "A komment nem található." });
    }

    db.Comments.Remove(comment);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

app.Run();

public record PostListItemDto(
    int Id,
    string Title,
    string Preview,
    DateTime CreatedAt,
    int CommentCount,
    string AuthorName
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
    string AuthorName,
    List<CommentDto> Comments
);

public record CreateCommentRequest(
    string? AuthorName,
    string? Text
);

public record RegisterRequest(
    string Username,
    string Email,
    string Password
);

public record LoginRequest(
    string Username,
    string Password
);

public record AuthResponse(
    string Token,
    string Username,
    string Email,
    string Role
);

public record CurrentUserResponse(
    string Id,
    string Username,
    string Email,
    string Role
);

public record CreatePostRequest(
    string Title,
    string Content
);

public record CreatePostResponse(
    int Id,
    string Title,
    string Content,
    DateTime CreatedAt,
    string AuthorName
);

public record MyPostListItemDto(
    int Id,
    string Title,
    DateTime CreatedAt,
    string AuthorName,
    int CommentCount
);

public record UpdatePostRequest(
    string Title,
    string Content
);

public record AdminPostListItemDto(
    int Id,
    string Title,
    string AuthorName,
    DateTime CreatedAt,
    int CommentCount
);

public record AdminCommentListItemDto(
    int Id,
    int PostId,
    string PostTitle,
    string AuthorName,
    string Text,
    DateTime CreatedAt
);

public partial class Program { }