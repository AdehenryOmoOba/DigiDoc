using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigiDocWebApp.Data;
using DigiDocWebApp.Models;

namespace DigiDocWebApp.Pages.Dashboard
{
    public class ReviewModel : PageModel
    {
        private readonly AppDbContext _context;

        public ReviewModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            // This page loads data via JavaScript/API calls
            // No server-side data loading needed for the initial page
        }
    }
} 