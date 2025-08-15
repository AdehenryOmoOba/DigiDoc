using System;
using System.ComponentModel.DataAnnotations;

namespace DigiDocWebApp.Models
{
    public class FormSubmission
    {
        public int Id { get; set; }
        
        [Required]
        public int FormTemplateId { get; set; }
        public FormTemplate FormTemplate { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string SubmittedBy { get; set; } = string.Empty;
        
        [Required]
        public string DataJson { get; set; } = string.Empty;
        
        // Company relationship - ADDED
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
        
        // User role for submission - ADDED
        [StringLength(50)]
        public string UserRole { get; set; } = "Client"; // Client, Broker, Internal
        
        // Status tracking
        public FormStatus Status { get; set; } = FormStatus.Draft;
        public int CurrentPage { get; set; } = 1;
        public bool IsComplete { get; set; } = false;
        
        // Review information
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public string? ReturnReason { get; set; }
        
        // Workflow tracking - ENHANCED
        public bool IsUnderReview { get; set; } = false;
        public string? AssignedReviewer { get; set; }
        public int ReviewAttempts { get; set; } = 0;
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
    }
    
    public enum FormStatus
    {
        Draft = 0,
        Submitted = 1,
        UnderReview = 2,
        Approved = 3,
        Returned = 4,
        Rejected = 5
    }
}