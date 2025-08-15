using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DigiDocWebApp.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [Required]
        public UserRole Role { get; set; } = UserRole.Client;
        
        // Company relationship
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
        
        // Authentication
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; } = false;
        public DateTime? LastLoginAt { get; set; }
        
        // Security
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }
        
        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = "System";
        
        // Navigation properties
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        
        // Computed properties
        public string FullName => $"{FirstName} {LastName}";
        public bool IsClient => Role == UserRole.Client;
        public bool IsBroker => Role == UserRole.Broker;
        public bool IsInternalStaff => Role == UserRole.InternalStaff || Role == UserRole.Administrator;
    }
    
    public enum UserRole
    {
        Client = 0,
        Broker = 1,
        InternalStaff = 2,
        Administrator = 3
    }
} 