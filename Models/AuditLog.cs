using System;
using System.ComponentModel.DataAnnotations;

namespace DigiDocWebApp.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty;
        
        public int? EntityId { get; set; }
        
        public string? Details { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        
        // IP and session info
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? SessionId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}