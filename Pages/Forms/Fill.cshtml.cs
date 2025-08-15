using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YourApp.Data;
using YourApp.Models;

namespace YourApp.Pages.Forms
{
    public class FillModel : PageModel
    {
        private readonly AppDbContext _context;

        public FillModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public int CurrentPage { get; set; } = 1;

        [BindProperty]
        public string DataJson { get; set; } = "{}";

        public FormTemplate? FormTemplate { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, int page = 1)
        {
            Id = id;
            CurrentPage = page;

            FormTemplate = await _context.FormTemplates
                .FirstOrDefaultAsync(f => f.Id == id);

            if (FormTemplate == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostNextAsync()
        {
            // Save progress logic here
            // ... (update FormSubmission with new DataJson and CurrentPage)
            // Audit log for save progress
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = "Anonymous",
                Action = "SaveProgress",
                EntityType = "FormSubmission",
                EntityId = Id,
                Details = $"Progress saved at page {CurrentPage + 1}",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            CurrentPage++;
            return RedirectToPage(new { id = Id, page = CurrentPage });
        }

        public async Task<IActionResult> OnPostPreviousAsync()
        {
            // Audit log for save progress
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = "Anonymous",
                Action = "SaveProgress",
                EntityType = "FormSubmission",
                EntityId = Id,
                Details = $"Progress saved at page {CurrentPage - 1}",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            CurrentPage = CurrentPage > 1 ? CurrentPage - 1 : 1;
            return RedirectToPage(new { id = Id, page = CurrentPage });
        }
    }
}