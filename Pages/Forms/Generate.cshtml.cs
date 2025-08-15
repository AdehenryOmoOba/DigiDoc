using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using DigiDocWebApp.Data;
using DigiDocWebApp.Models;
using DigiDocWebApp.Services;

namespace DigiDocWebApp.Pages.Forms
{
    public class GenerateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAIFormGenerationService _aiService;
        private readonly ILogger<GenerateModel> _logger;

        public GenerateModel(
            AppDbContext context,
            IAIFormGenerationService aiService,
            ILogger<GenerateModel> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        [BindProperty]
        public string FormName { get; set; } = string.Empty;

        [BindProperty]
        public string FormDescription { get; set; } = string.Empty;

        [BindProperty]
        public string FormCategory { get; set; } = "General";

        [BindProperty]
        public IFormFile? FormFile { get; set; }

        public void OnGet()
        {
            // Initialize the page
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || FormFile == null)
            {
                return Page();
            }

            try
            {
                // Read the uploaded file
                using var memoryStream = new MemoryStream();
                await FormFile.CopyToAsync(memoryStream);
                var fileData = memoryStream.ToArray();

                // Generate form using AI service
                var generatedBy = User.Identity?.Name ?? "Anonymous";
                var formTemplate = await _aiService.GenerateFormFromImageAsync(fileData, FormFile.FileName, generatedBy);

                // Update form properties from user input
                formTemplate.Name = FormName;
                formTemplate.Description = FormDescription;
                formTemplate.Category = FormCategory;

                // Save to database
                _context.FormTemplates.Add(formTemplate);
                await _context.SaveChangesAsync();

                // Log the successful generation
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = generatedBy,
                    Action = "GenerateForm",
                    EntityType = "FormTemplate",
                    EntityId = formTemplate.Id,
                    Details = $"Generated form '{FormName}' from file '{FormFile.FileName}'",
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                // Redirect to the forms list with success message
                TempData["SuccessMessage"] = $"Form '{FormName}' has been generated successfully!";
                return RedirectToPage("/Forms/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating form from file: {FileName}", FormFile?.FileName);
                ModelState.AddModelError("", "An error occurred while generating the form. Please try again.");
                return Page();
            }
        }
    }
} 