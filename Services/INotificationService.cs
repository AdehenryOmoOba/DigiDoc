using System.Threading.Tasks;
using YourApp.Models;

namespace YourApp.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string recipientId, string title, string message, NotificationType type, int? formSubmissionId = null, int? formTemplateId = null, string? actionUrl = null);
        Task MarkNotificationAsReadAsync(int notificationId);
        Task<int> GetUnreadNotificationCountAsync(string recipientId);
        Task SendFormSubmittedNotificationAsync(FormSubmission submission);
        Task SendFormReturnedNotificationAsync(FormSubmission submission, string returnReason);
        Task SendFormApprovedNotificationAsync(FormSubmission submission);
        Task SendFormRejectedNotificationAsync(FormSubmission submission, string rejectionReason);
    }
}
