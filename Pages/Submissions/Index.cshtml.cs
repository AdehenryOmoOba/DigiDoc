using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigiDocWebApp.Data;
using DigiDocWebApp.Models;

namespace DigiDocWebApp.Pages.Submissions
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<FormSubmission> FormSubmissions { get; set; } = new List<FormSubmission>();

        public async Task OnGetAsync()
        {
            FormSubmissions = await _context.FormSubmissions
                .Include(s => s.FormTemplate)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDiscardAsync(int id)
        {
            var submission = await _context.FormSubmissions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
            {
                return NotFound();
            }

            // Only allow discarding drafts
            if (submission.Status != FormStatus.Draft)
            {
                TempData["Error"] = "Only draft forms can be discarded.";
                return RedirectToPage();
            }

            // Check if this is the user's own submission (in a real app, you'd check actual user identity)
            var currentUser = User.Identity?.Name ?? "Anonymous";
            if (submission.SubmittedBy != currentUser)
            {
                TempData["Error"] = "You can only discard your own drafts.";
                return RedirectToPage();
            }

            // Remove the submission
            _context.FormSubmissions.Remove(submission);
            
            // Log the action
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = currentUser,
                Action = "DiscardDraft",
                EntityType = "FormSubmission",
                EntityId = submission.Id,
                Details = $"Discarded draft form: {submission.FormTemplateId}",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Draft form has been discarded successfully.";
            return RedirectToPage();
        }
    }
}
