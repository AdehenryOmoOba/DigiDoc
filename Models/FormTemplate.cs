using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DigiDocWebApp.Models
{
    public class FormTemplate
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string StructureJson { get; set; } = string.Empty;
        
        // AI Generation metadata
        public string? OriginalImagePath { get; set; }
        public string? GeneratedBy { get; set; } // AI model used
        public DateTime? GeneratedAt { get; set; }
        
        // Form metadata
        public int TotalPages { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public string Category { get; set; } = "General";
        
        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        
        // Navigation
        public ICollection<FormSubmission> Submissions { get; set; } = new List<FormSubmission>();
    }
}