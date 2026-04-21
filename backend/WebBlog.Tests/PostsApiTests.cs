using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace WebBlog.Tests;

public class PostsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PostsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPosts_ReturnsOk_AndPostsList()
    {
        var response = await _client.GetAsync("/api/posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var posts = await response.Content.ReadFromJsonAsync<List<PostListItemResponse>>();

        Assert.NotNull(posts);
        Assert.NotEmpty(posts);
    }

    [Fact]
    public async Task PostComment_WithInvalidInput_ReturnsBadRequest()
    {
        var request = new
        {
            authorName = "",
            text = ""
        };

        var response = await _client.PostAsJsonAsync("/api/posts/1/comments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class PostListItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CommentCount { get; set; }
    }
}