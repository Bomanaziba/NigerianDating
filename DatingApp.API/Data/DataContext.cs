using DatingApp.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data {
    public class DataContext : IdentityDbContext<User, Role, int, 
        IdentityUserClaim<int>, UserRole, IdentityUserLogin<int>,
        IdentityRoleClaim<int>, IdentityUserToken<int>> 
    {
        public DataContext (DbContextOptions<DataContext> option) : base (option) { }
        public DbSet<Value> Values { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating (ModelBuilder builder) 
        {

            base.OnModelCreating(builder);

            builder.Entity<UserRole>(userRole =>
            {
                userRole.HasKey(ur => new {ur.UserId, ur.RoleId});

                userRole.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                userRole.HasOne(ur => ur.User)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

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

            builder.Entity<Message> ()
                .HasOne (o => o.Sender)
                .WithMany (z => z.MessagesSent)
                .OnDelete (DeleteBehavior.Restrict);

            builder.Entity<Message> ()
                .HasOne (o => o.Recipient)
                .WithMany (z => z.MessagesReceived)
                .OnDelete (DeleteBehavior.Restrict);

            builder.Entity<Photo>().HasQueryFilter(p=>p.IsApproved);
        }
    }
}