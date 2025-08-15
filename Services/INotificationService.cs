using System.Threading.Tasks;
using System.Collections.Generic;
using DigiDocWebApp.Models;

namespace DigiDocWebApp.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string recipientId, string title, string message, NotificationType type, int? formSubmissionId = null, int? formTemplateId = null, string? actionUrl = null);
        Task MarkNotificationAsReadAsync(int notificationId);
        Task<int> GetUnreadNotificationCountAsync(string recipientId);
        Task<List<Notification>> GetUserNotificationsAsync(string recipientId, bool unreadOnly = false);
        
        // Form workflow notifications
        Task SendFormSubmittedNotificationAsync(FormSubmission submission);
        Task SendFormReturnedNotificationAsync(FormSubmission submission, string returnReason);
        Task SendFormApprovedNotificationAsync(FormSubmission submission);
        Task SendFormRejectedNotificationAsync(FormSubmission submission, string rejectionReason);
        Task SendFormAssignedForReviewNotificationAsync(FormSubmission submission, string reviewerId);
        
        // Bulk notifications for internal staff
        Task NotifyInternalStaffOfNewSubmissionAsync(FormSubmission submission);
        Task SendProgressSavedNotificationAsync(FormSubmission submission);
    }
}
