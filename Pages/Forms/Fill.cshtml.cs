using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigiDocWebApp.Data;
using DigiDocWebApp.Models;
using DigiDocWebApp.Services;

namespace DigiDocWebApp.Pages.Forms
{
    public class FillModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IFormRenderingService _formRenderingService;
        private readonly ILogger<FillModel> _logger;

        public FillModel(AppDbContext context, IFormRenderingService formRenderingService, ILogger<FillModel> logger)
        {
            _context = context;
            _formRenderingService = formRenderingService;
            _logger = logger;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public int CurrentPage { get; set; } = 1;

        [BindProperty]
        public string DataJson { get; set; } = "{}";

        public FormTemplate? FormTemplate { get; set; }
        public FormSubmission? FormSubmission { get; set; }
        public string FormHtml { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int id, int pageNumber = 1)
        {
            Id = id;
            CurrentPage = pageNumber;

            FormTemplate = await _context.FormTemplates
                .FirstOrDefaultAsync(f => f.Id == id);

            if (FormTemplate == null)
            {
                return NotFound();
            }

            // Find existing form submission for this user (if any)
            var currentUser = User.Identity?.Name ?? "Anonymous";
            FormSubmission = await _context.FormSubmissions
                .FirstOrDefaultAsync(s => s.FormTemplateId == id && 
                                        s.SubmittedBy == currentUser && 
                                        s.Status == FormStatus.Draft);

            // If we have existing data, use it
            if (FormSubmission != null)
            {
                DataJson = FormSubmission.DataJson ?? "{}";
                // If user is navigating to a page they haven't reached yet, update current page
                if (pageNumber > FormSubmission.CurrentPage)
                {
                    FormSubmission.CurrentPage = pageNumber;
                    await _context.SaveChangesAsync();
                }
            }

            // Generate the form HTML for the current page with existing data
            try
            {
                FormHtml = await _formRenderingService.RenderFormPageAsync(FormTemplate, CurrentPage, FormSubmission);
            }
            catch (Exception ex)
            {
                // Log error and show fallback message
                FormHtml = $"<div class='alert alert-warning'><strong>Form content unavailable.</strong><br/>Error: {ex.Message}<br/><br/>Form Structure: <pre>{FormTemplate.StructureJson}</pre></div>";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostNextAsync()
        {
            await SaveFormDataAsync();
            CurrentPage++;
            return RedirectToPage(new { id = Id, pageNumber = CurrentPage });
        }

        public async Task<IActionResult> OnPostPreviousAsync()
        {
            await SaveFormDataAsync();
            CurrentPage = CurrentPage > 1 ? CurrentPage - 1 : 1;
            return RedirectToPage(new { id = Id, pageNumber = CurrentPage });
        }

        private async Task SaveFormDataFromRequestAsync()
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            
            // Debug: Log all form keys
            _logger.LogInformation("SaveFormDataFromRequestAsync: Processing {Count} form keys", Request.Form.Keys.Count);
            foreach (var key in Request.Form.Keys)
            {
                var values = Request.Form[key];
                _logger.LogInformation("Form field: {Key} = {Values}", key, string.Join(",", values));
            }
            
            // Collect form data from the current request
            var formDataDict = new Dictionary<string, object>();
            var checkboxGroups = new Dictionary<string, List<string>>();
            
            foreach (var key in Request.Form.Keys)
            {
                // Skip system fields
                if (key == "Id" || key == "CurrentPage" || key == "DataJson" || 
                    key.StartsWith("__") || key.Contains("Handler") || key.Contains("Token"))
                {
                    _logger.LogInformation("Skipping system field: {Key}", key);
                    continue;
                }
                
                var values = Request.Form[key].Where(v => !string.IsNullOrEmpty(v)).ToArray();
                if (values.Length == 0) continue;
                
                if (values.Length == 1)
                {
                    // Single value (text, radio, single checkbox, etc.)
                    formDataDict[key] = values[0];
                    _logger.LogInformation("Added single value: {Key} = {Value}", key, values[0]);
                }
                else
                {
                    // Multiple values (checkbox group)
                    checkboxGroups[key] = values.ToList();
                    _logger.LogInformation("Added multiple values: {Key} = {Values}", key, string.Join(",", values));
                }
            }
            
            // Add checkbox groups as JSON arrays
            foreach (var group in checkboxGroups)
            {
                formDataDict[group.Key] = System.Text.Json.JsonSerializer.Serialize(group.Value);
            }
            
            var jsonData = System.Text.Json.JsonSerializer.Serialize(formDataDict);
            _logger.LogInformation("Final JSON data: {JsonData}", jsonData);
            
            // Find or create form submission
            var submission = await _context.FormSubmissions
                .FirstOrDefaultAsync(s => s.FormTemplateId == Id && 
                                        s.SubmittedBy == currentUser && 
                                        s.Status == FormStatus.Draft);

            if (submission == null)
            {
                submission = new FormSubmission
                {
                    FormTemplateId = Id,
                    SubmittedBy = currentUser,
                    Status = FormStatus.Draft,
                    CurrentPage = CurrentPage,
                    DataJson = jsonData,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.FormSubmissions.Add(submission);
            }
            else
            {
                submission.CurrentPage = CurrentPage;
                submission.DataJson = jsonData;
                submission.UpdatedAt = DateTime.UtcNow;
            }

            // Audit log for save progress
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = currentUser,
                Action = "SaveProgress",
                EntityType = "FormSubmission",
                EntityId = submission.Id,
                Details = $"Form data saved at page {CurrentPage} with {formDataDict.Count} fields",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        private async Task SaveFormDataAsync()
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            
            _logger.LogInformation("SaveFormDataAsync: DataJson from model = {DataJson}", DataJson);
            
            // Find or create form submission
            var submission = await _context.FormSubmissions
                .FirstOrDefaultAsync(s => s.FormTemplateId == Id && 
                                        s.SubmittedBy == currentUser && 
                                        s.Status == FormStatus.Draft);

            if (submission == null)
            {
                submission = new FormSubmission
                {
                    FormTemplateId = Id,
                    SubmittedBy = currentUser,
                    Status = FormStatus.Draft,
                    CurrentPage = CurrentPage,
                    DataJson = DataJson,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.FormSubmissions.Add(submission);
            }
            else
            {
                submission.CurrentPage = CurrentPage;
                submission.DataJson = DataJson;
                submission.UpdatedAt = DateTime.UtcNow;
            }

            // Audit log for save progress
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = currentUser,
                Action = "SaveProgress",
                EntityType = "FormSubmission",
                EntityId = submission.Id,
                Details = $"Form data saved at page {CurrentPage} with DataJson: {DataJson}",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> OnPostSubmitAsync()
        {
            await SaveFormDataAsync();
            
            // Mark as completed
            var currentUser = User.Identity?.Name ?? "Anonymous";
            var submission = await _context.FormSubmissions
                .FirstOrDefaultAsync(s => s.FormTemplateId == Id && 
                                        s.SubmittedBy == currentUser && 
                                        s.Status == FormStatus.Draft);

            if (submission != null)
            {
                submission.Status = FormStatus.Submitted;
                submission.IsComplete = true;
                submission.SubmittedAt = DateTime.UtcNow;
                submission.UpdatedAt = DateTime.UtcNow;

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = currentUser,
                    Action = "SubmitForm",
                    EntityType = "FormSubmission", 
                    EntityId = submission.Id,
                    Details = $"Form submitted successfully",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Submissions/Index");
        }

        public async Task<IActionResult> OnPostDiscardAsync(int submissionId)
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            
            var submission = await _context.FormSubmissions
                .FirstOrDefaultAsync(s => s.Id == submissionId && 
                                        s.SubmittedBy == currentUser && 
                                        s.Status == FormStatus.Draft);

            if (submission == null)
            {
                return NotFound();
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
                Details = $"Discarded draft form from fill page: {submission.FormTemplateId}",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            
            return RedirectToPage("/Forms/Index");
        }
    }
}