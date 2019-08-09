using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data {
    public class DataContext : DbContext {
        public DataContext (DbContextOptions<DataContext> option) : base (option) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Value> Values { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Like> Likes { get; set; }

        protected override void OnModelCreating (ModelBuilder builder) {
            builder.Entity<Like> ()
                .HasKey (k => new { k.LikerId, k.LikeeId });

            builder.Entity<Like> ()
                .HasOne (o => o.Likee)
                .WithMany (z => z.Likers)
                .HasForeignKey (z => z.LikeeId)
                .OnDelete (DeleteBehavior.Restrict);

            builder.Entity<Like> ()
                .HasOne (o => o.Liker)
                .WithMany (z => z.Likees)
                .HasForeignKey (z => z.LikerId)
                .OnDelete (DeleteBehavior.Restrict);

        }
    }
}