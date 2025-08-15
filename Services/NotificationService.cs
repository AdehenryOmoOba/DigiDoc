using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigiDocWebApp.Data;
using DigiDocWebApp.Models;

namespace DigiDocWebApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateNotificationAsync(string recipientId, string title, string message, NotificationType type, int? formSubmissionId = null, int? formTemplateId = null, string? actionUrl = null)
        {
            try
            {
                var notification = new Notification
                {
                    RecipientId = recipientId,
                    Title = title,
                    Message = message,
                    Type = type,
                    FormSubmissionId = formSubmissionId,
                    FormTemplateId = formTemplateId,
                    ActionUrl = actionUrl,
                    Status = NotificationStatus.Unread,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created notification for user {UserId}: {Title}", recipientId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", recipientId);
                throw;
            }
        }

        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification != null)
                {
                    notification.Status = NotificationStatus.Read;
                    notification.ReadAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
                throw;
            }
        }

        public async Task<int> GetUnreadNotificationCountAsync(string recipientId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.RecipientId == recipientId && n.Status == NotificationStatus.Unread)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notification count for user {UserId}", recipientId);
                return 0;
            }
        }

        public async Task SendFormSubmittedNotificationAsync(FormSubmission submission)
        {
            try
            {
                // Notify internal reviewers
                var reviewers = await GetInternalReviewersAsync();
                foreach (var reviewer in reviewers)
                {
                    await CreateNotificationAsync(
                        reviewer,
                        "New Form Submission",
                        $"A new form submission has been received: {submission.FormTemplate.Name}",
                        NotificationType.FormSubmitted,
                        submission.Id,
                        submission.FormTemplateId,
                        $"/submissions/review/{submission.Id}"
                    );
                }

                _logger.LogInformation("Sent form submitted notifications for submission {SubmissionId}", submission.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending form submitted notifications for submission {SubmissionId}", submission.Id);
                throw;
            }
        }

        public async Task SendFormReturnedNotificationAsync(FormSubmission submission, string returnReason)
        {
            try
            {
                await CreateNotificationAsync(
                    submission.SubmittedBy,
                    "Form Returned for Revision",
                    $"Your form submission '{submission.FormTemplate.Name}' has been returned for revision. Reason: {returnReason}",
                    NotificationType.FormReturned,
                    submission.Id,
                    submission.FormTemplateId,
                    $"/forms/fill/{submission.FormTemplateId}?submissionId={submission.Id}"
                );

                _logger.LogInformation("Sent form returned notification for submission {SubmissionId}", submission.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending form returned notification for submission {SubmissionId}", submission.Id);
                throw;
            }
        }

        public async Task SendFormApprovedNotificationAsync(FormSubmission submission)
        {
            try
            {
                await CreateNotificationAsync(
                    submission.SubmittedBy,
                    "Form Approved",
                    $"Your form submission '{submission.FormTemplate.Name}' has been approved.",
                    NotificationType.FormApproved,
                    submission.Id,
                    submission.FormTemplateId
                );

                _logger.LogInformation("Sent form approved notification for submission {SubmissionId}", submission.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending form approved notification for submission {SubmissionId}", submission.Id);
                throw;
            }
        }

        public async Task SendFormRejectedNotificationAsync(FormSubmission submission, string rejectionReason)
        {
            try
            {
                await CreateNotificationAsync(
                    submission.SubmittedBy,
                    "Form Rejected",
                    $"Your form submission '{submission.FormTemplate.Name}' has been rejected. Reason: {rejectionReason}",
                    NotificationType.Error,
                    submission.Id,
                    submission.FormTemplateId
                );

                _logger.LogInformation("Sent form rejected notification for submission {SubmissionId}", submission.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending form rejected notification for submission {SubmissionId}", submission.Id);
                throw;
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string recipientId, bool unreadOnly = false)
        {
            try
            {
                var query = _context.Notifications
                    .Include(n => n.FormSubmission)
                    .ThenInclude(fs => fs!.FormTemplate)
                    .Include(n => n.FormTemplate)
                    .Where(n => n.RecipientId == recipientId);

                if (unreadOnly)
                {
                    query = query.Where(n => n.Status == NotificationStatus.Unread);
                }

                return await query
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user: {UserId}", recipientId);
                throw;
            }
        }

        public async Task SendFormAssignedForReviewNotificationAsync(FormSubmission submission, string reviewerId)
        {
            try
            {
                var title = $"Form Assignment: {submission.FormTemplate?.Name}";
                var message = $"You have been assigned to review a form submission from {submission.SubmittedBy}. " +
                             $"Form: {submission.FormTemplate?.Name}. Please review and take appropriate action.";

                await CreateNotificationAsync(
                    reviewerId,
                    title,
                    message,
                    NotificationType.Info,
                    submission.Id,
                    actionUrl: $"/pages/submissions/review/{submission.Id}"
                );

                _logger.LogInformation("Form assignment notification sent to reviewer: {ReviewerId} for submission: {SubmissionId}",
                    reviewerId, submission.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending form assignment notification for submission: {SubmissionId}", submission.Id);
                throw;
            }
        }

        public async Task NotifyInternalStaffOfNewSubmissionAsync(FormSubmission submission)
        {
            try
            {
                var internalReviewers = await GetInternalReviewersAsync();
                var title = $"New Form Submission: {submission.FormTemplate?.Name}";
                var message = $"A new form submission has been received from {submission.SubmittedBy}. " +
                             $"Company: {submission.Company?.Name ?? "N/A"}. Form: {submission.FormTemplate?.Name}. " +
                             "Please assign for review or take appropriate action.";

                foreach (var reviewer in internalReviewers)
                {
                    await CreateNotificationAsync(
                        reviewer,
                        title,
                        message,
                        NotificationType.FormSubmitted,
                        submission.Id,
                        actionUrl: $"/pages/dashboard/review?highlight={submission.Id}"
                    );
                }

                _logger.LogInformation("New submission notifications sent to internal staff for submission: {SubmissionId}", submission.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying internal staff of new submission: {SubmissionId}", submission.Id);
                throw;
            }
        }

        public async Task SendProgressSavedNotificationAsync(FormSubmission submission)
        {
            try
            {
                // Only send notification if this is a significant progress milestone
                if (submission.CurrentPage > 1 && submission.CurrentPage % 5 == 0) // Every 5th page
                {
                    var title = "Form Progress Saved";
                    var message = $"Your progress on form '{submission.FormTemplate?.Name}' has been saved. " +
                                 $"You are currently on page {submission.CurrentPage} of {submission.FormTemplate?.TotalPages}.";

                    await CreateNotificationAsync(
                        submission.SubmittedBy,
                        title,
                        message,
                        NotificationType.Info,
                        submission.Id,
                        actionUrl: $"/pages/forms/fill/{submission.FormTemplateId}"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending progress saved notification for submission: {SubmissionId}", submission.Id);
                // Don't throw here as this is not critical
            }
        }

        private async Task<string[]> GetInternalReviewersAsync()
        {
            // In a real application, this would query your user/role system
            // For now, return a default set of reviewers
            return new[] { "admin", "reviewer1", "reviewer2" };
        }
    }
}
