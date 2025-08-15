using System;
using System.ComponentModel.DataAnnotations;

namespace YourApp.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string RecipientId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; } = NotificationType.Info;
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
        
        // Related entities
        public int? FormSubmissionId { get; set; }
        public FormSubmission? FormSubmission { get; set; }
        
        public int? FormTemplateId { get; set; }
        public FormTemplate? FormTemplate { get; set; }
        
        // Metadata
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
    
    public enum NotificationType
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3,
        FormSubmitted = 4,
        FormReturned = 5,
        FormApproved = 6,
        FormRejected = 7
    }
    
    public enum NotificationStatus
    {
        Unread = 0,
        Read = 1,
        Archived = 2
    }
}