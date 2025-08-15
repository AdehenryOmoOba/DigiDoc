using Microsoft.EntityFrameworkCore;
using DigiDocWebApp.Models;

namespace DigiDocWebApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FormTemplate> FormTemplates { get; set; }
        public DbSet<FormSubmission> FormSubmissions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<User> Users { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure User relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany()
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Configure FormSubmission relationships
            modelBuilder.Entity<FormSubmission>()
                .HasOne(fs => fs.Company)
                .WithMany(c => c.FormSubmissions)
                .HasForeignKey(fs => fs.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Configure User-Notification relationship
            modelBuilder.Entity<Notification>()
                .HasOne<User>()
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.RecipientId)
                .HasPrincipalKey(u => u.Username)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure indexes for performance
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
                
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
                
            modelBuilder.Entity<FormSubmission>()
                .HasIndex(fs => fs.Status);
                
            modelBuilder.Entity<FormSubmission>()
                .HasIndex(fs => fs.SubmittedBy);
                
            modelBuilder.Entity<FormSubmission>()
                .HasIndex(fs => fs.AssignedReviewer);
        }
    }
}