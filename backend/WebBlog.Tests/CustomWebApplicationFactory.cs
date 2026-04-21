using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebBlog.Api.Data;

namespace WebBlog.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<BlogDbContext>));
            services.RemoveAll(typeof(BlogDbContext));

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddSingleton(_connection);

            services.AddDbContext<BlogDbContext>((serviceProvider, options) =>
            {
                var connection = serviceProvider.GetRequiredService<SqliteConnection>();
                options.UseSqlite(connection);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}