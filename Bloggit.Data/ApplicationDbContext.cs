using Bloggit.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bloggit.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserName).IsUnique();

                // Configure relationships
                entity.HasMany(u => u.Posts)
                    .WithOne(p => p.Author)
                    .HasForeignKey(p => p.AuthorId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(u => u.Comments)
                    .WithOne(c => c.Commenter)
                    .HasForeignKey(c => c.CommenterId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Post
            builder.Entity<Post>(entity =>
            {
                entity.HasOne(p => p.Author)
                    .WithMany(u => u.Posts)
                    .HasForeignKey(p => p.AuthorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Comment
            builder.Entity<Comment>(entity =>
            {
                entity.HasOne(c => c.Commenter)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.CommenterId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(c => c.Post)
                    .WithMany()
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}