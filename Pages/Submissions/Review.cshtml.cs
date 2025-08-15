using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YourApp.Data;
using YourApp.Models;
using YourApp.Services;

namespace YourApp.Pages.Submissions
{
    public class ReviewModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public ReviewModel(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [BindProperty]
        public string Comment { get; set; } = string.Empty;

        public FormSubmission? Submission { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Submission = await _context.FormSubmissions
                .Include(s => s.FormTemplate)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (Submission == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostReturnAsync(int id)
        {
            var submission = await _context.FormSubmissions.FindAsync(id);
            if (submission == null) return NotFound();
            
            submission.Status = FormStatus.Returned;
            submission.ReturnReason = Comment;
            submission.ReturnedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = "Employee",
                Action = "Return",
                EntityType = "FormSubmission",
                EntityId = id,
                Details = Comment,
                CreatedAt = DateTime.UtcNow
            });

            // Create notification
            await _notificationService.SendFormReturnedNotificationAsync(
                submission, 
                Comment
            );

            await _context.SaveChangesAsync();
            return RedirectToPage("Index");
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var submission = await _context.FormSubmissions.FindAsync(id);
            if (submission == null) return NotFound();
            
            submission.Status = FormStatus.Approved;
            submission.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = "Employee",
                Action = "Approve",
                EntityType = "FormSubmission",
                EntityId = id,
                Details = Comment,
                CreatedAt = DateTime.UtcNow
            });

            // Create notification
            await _notificationService.SendFormApprovedNotificationAsync(
                submission
            );

            await _context.SaveChangesAsync();
            return RedirectToPage("Index");
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var submission = await _context.FormSubmissions.FindAsync(id);
            if (submission == null) return NotFound();
            
            submission.Status = FormStatus.Rejected;
            submission.ReturnReason = Comment;
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = "Employee",
                Action = "Reject",
                EntityType = "FormSubmission",
                EntityId = id,
                Details = Comment,
                CreatedAt = DateTime.UtcNow
            });

            // Create notification
            await _notificationService.SendFormRejectedNotificationAsync(
                submission, 
                Comment
            );

            await _context.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}