using Backend.Models;
using Backend.Models.Auth;
using Backend.Models.Product;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.DBContext
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Location> Locations { get; set; }
        public DbSet<Deal> Deals { get; set; }
        public DbSet<AgencyApplication> AgencyApplications { get; set; }
        public DbSet<AgencyProfile> AgencyProfiles { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Deal>(entity =>
            {
                entity.Property(e => e.DiscountedPrice).HasPrecision(18, 2);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.Rating).HasPrecision(10, 2);
                entity.Property(e => e.RelevanceScore).HasPrecision(12, 4);
            });

            builder
                .Entity<AgencyApplication>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
