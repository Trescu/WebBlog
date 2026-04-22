using Microsoft.EntityFrameworkCore;
using WebBlog.Api.Models;

namespace WebBlog.Api.Data;

public class BlogDbContext : DbContext
{
    public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options)
    {
    }

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>(entity =>
        {
            entity.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Content)
                .IsRequired();

            entity.Property(p => p.AuthorName)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasMany(p => p.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.Property(c => c.AuthorName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Text)
                .IsRequired()
                .HasMaxLength(1000);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.PasswordSalt)
                .IsRequired();

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.HasIndex(u => u.Email)
                .IsUnique();
        });
    }
}