using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigiDocWebApp.Data;
using DigiDocWebApp.Models;
using DigiDocWebApp.Services;

namespace DigiDocWebApp.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public IndexModel(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public int TotalForms { get; set; }
        public int PendingReview { get; set; }
        public int UnderReview { get; set; }
        public int Approved { get; set; }
        public int Returned { get; set; }
        public int Rejected { get; set; }

        public async Task OnGetAsync()
        {
            TotalForms = await _context.FormTemplates.CountAsync();
            PendingReview = await _context.FormSubmissions.CountAsync(s => s.Status == FormStatus.Submitted);
            UnderReview = await _context.FormSubmissions.CountAsync(s => s.Status == FormStatus.UnderReview);
            Approved = await _context.FormSubmissions.CountAsync(s => s.Status == FormStatus.Approved);
            Returned = await _context.FormSubmissions.CountAsync(s => s.Status == FormStatus.Returned);
            Rejected = await _context.FormSubmissions.CountAsync(s => s.Status == FormStatus.Rejected);
        }

        public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.Status = NotificationStatus.Read;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Audit log for notification read
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = notification.RecipientId,
                    Action = "MarkNotificationRead",
                    EntityType = "Notification",
                    EntityId = id,
                    Details = $"Notification {id} marked as read",
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}