using CommentsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentsApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

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

            // The date is saved in UTC, but the database does not store the kind of the date, so EF reads it
            // as "unspecified" and the JSON has no "Z" at the end. The browser would then take it as local time.
            entity.Property(c => c.CreatedAt)
                .HasConversion(date => date, date => DateTime.SpecifyKind(date, DateTimeKind.Utc));

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

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.Property(a => a.Kind).HasConversion<string>().HasMaxLength(20);
            entity.Property(a => a.FileName).IsRequired().HasMaxLength(255);
            entity.Property(a => a.StoredFileName).IsRequired().HasMaxLength(100);
            entity.Property(a => a.ContentType).IsRequired().HasMaxLength(100);

            // one attachment per comment, it is removed together with the comment
            entity.HasOne(a => a.Comment)
                .WithOne(c => c.Attachment)
                .HasForeignKey<Attachment>(a => a.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => a.CommentId).IsUnique();
        });
    }
}
