using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<EventOrganiser> EventOrganisers => Set<EventOrganiser>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Prevent duplicate enrolments for the same user in the same category
            modelBuilder.Entity<Enrolment>()
                .HasIndex(e => new { e.UserId, e.CategoryId })
                .IsUnique();

            // Configure 1-to-1 relationship between User and EventOrganiser
            modelBuilder.Entity<User>()
                .HasOne(u => u.EventOrganiser)
                .WithOne(eo => eo.User)
                .HasForeignKey<EventOrganiser>(eo => eo.UserId);

            // FIX: Disable Cascade Delete on Enrolments to fix SQL Server Error 1785
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}