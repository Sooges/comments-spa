using CommentsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentsApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.Property(c => c.UserName).IsRequired().HasMaxLength(50);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(254);
            entity.Property(c => c.HomePage).HasMaxLength(2048);
            entity.Property(c => c.Text).IsRequired().HasMaxLength(5000);
            entity.Property(c => c.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(c => c.UserAgent).IsRequired().HasMaxLength(512);

            // a comment can have any number of replies
            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // indexes for the fields the main page sorts and groups by
            entity.HasIndex(c => c.ParentId);
            entity.HasIndex(c => c.CreatedAt);
            entity.HasIndex(c => c.UserName);
            entity.HasIndex(c => c.Email);
        });
    }
}
