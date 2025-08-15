using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YourApp.Models
{
    public class Company
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ContactPerson { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(200)]
        public string? Address { get; set; }
        
        public CompanyType Type { get; set; } = CompanyType.Client;
        public bool IsActive { get; set; } = true;
        
        // Relationships
        public int? ParentCompanyId { get; set; }
        public Company? ParentCompany { get; set; }
        public ICollection<Company> Subsidiaries { get; set; } = new List<Company>();
        
        // Form submissions from this company
        public ICollection<FormSubmission> FormSubmissions { get; set; } = new List<FormSubmission>();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum CompanyType
    {
        Client = 0,
        Broker = 1,
        Internal = 2
    }
}