using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YourApp.Data;
using YourApp.Models;

namespace YourApp.Pages.Submissions
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
    }
}
