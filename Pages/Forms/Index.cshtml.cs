using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigiDocWebApp.Data;
using DigiDocWebApp.Models;

namespace DigiDocWebApp.Pages.Forms
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<FormTemplate> FormTemplates { get; set; } = new List<FormTemplate>();

        public async Task OnGetAsync()
        {
            FormTemplates = await _context.FormTemplates
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }
    }
}
