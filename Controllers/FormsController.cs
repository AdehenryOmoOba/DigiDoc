using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigiDocWebApp.Data;
using DigiDocWebApp.Models;
using DigiDocWebApp.Services;
using System.Collections.Generic; // Added missing import
using System.Linq; // Added missing import

namespace DigiDocWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FormsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAIFormGenerationService _aiService;
        private readonly IFormRenderingService _renderingService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<FormsController> _logger;

        public FormsController(
            AppDbContext context,
            IAIFormGenerationService aiService,
            IFormRenderingService renderingService,
            INotificationService notificationService,
            ILogger<FormsController> logger)
        {
            _context = context;
            _aiService = aiService;
            _renderingService = renderingService;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetForms()
        {
            try
            {
                var forms = await _context.FormTemplates
                    .Where(f => f.IsActive)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                return Ok(forms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving forms");
                return StatusCode(500, "Error retrieving forms");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetForm(int id)
        {
            try
            {
                var form = await _context.FormTemplates
                    .Include(f => f.Submissions)
                    .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

                if (form == null)
                {
                    return NotFound("Form not found");
                }

                return Ok(form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving form {FormId}", id);
                return StatusCode(500, "Error retrieving form");
            }
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateFormFromFile([FromForm] IFormFile formFile, [FromForm] string formName, [FromForm] string formDescription, [FromForm] string formCategory)
        {
            try
            {
                if (formFile == null || formFile.Length == 0)
                {
                    return BadRequest("No form file provided");
                }

                if (string.IsNullOrWhiteSpace(formName))
                {
                    return BadRequest("Form name is required");
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(formFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Invalid file type. Supported formats: JPG, PNG, GIF, PDF, DOC, DOCX.");
                }

                // Validate file size (10MB limit)
                if (formFile.Length > 10 * 1024 * 1024)
                {
                    return BadRequest("File size must be less than 10MB");
                }

                // Read file content
                using var memoryStream = new MemoryStream();
                await formFile.CopyToAsync(memoryStream);
                var fileData = memoryStream.ToArray();

                // Generate form using AI
                var generatedBy = User.Identity?.Name ?? "system";
                var formTemplate = await _aiService.GenerateFormFromImageAsync(fileData, formFile.FileName, generatedBy);

                // Update form properties from user input
                formTemplate.Name = formName;
                formTemplate.Description = formDescription ?? "";
                formTemplate.Category = formCategory ?? "General";

                // Save to database
                _context.FormTemplates.Add(formTemplate);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated form template {FormId} from file {FileName}", formTemplate.Id, formFile.FileName);

                return Ok(new { success = true, formId = formTemplate.Id, message = "Form generated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating form from file {FileName}", formFile?.FileName);
                return StatusCode(500, "Error generating form from file");
            }
        }

        [HttpGet("{id}/render")]
        public async Task<IActionResult> RenderForm(int id, int? submissionId = null)
        {
            try
            {
                var formTemplate = await _context.FormTemplates
                    .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

                if (formTemplate == null)
                {
                    return NotFound("Form not found");
                }

                FormSubmission? submission = null;
                if (submissionId.HasValue)
                {
                    submission = await _context.FormSubmissions
                        .Include(s => s.FormTemplate)
                        .FirstOrDefaultAsync(s => s.Id == submissionId.Value);
                }

                var html = await _renderingService.RenderFormAsync(formTemplate, submission);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering form {FormId}", id);
                return StatusCode(500, "Error rendering form");
            }
        }

        [HttpPost("autosave")]
        public async Task<IActionResult> AutoSaveForm([FromForm] IFormCollection formData)
        {
            try
            {
                _logger.LogInformation("Auto-save request received with {Count} fields", formData.Count);
                foreach (var kvp in formData)
                {
                    _logger.LogInformation("Field: {Key} = {Value}", kvp.Key, string.Join(",", kvp.Value));
                }
                // Parse formId - take first value if multiple
                var formIdString = formData["formId"].FirstOrDefault();
                if (!int.TryParse(formIdString, out var formId))
                {
                    return BadRequest("Invalid form ID");
                }

                // Parse currentPage - take first value if multiple  
                var currentPageString = formData["currentPage"].FirstOrDefault();
                if (!int.TryParse(currentPageString, out var currentPage))
                {
                    return BadRequest("Invalid current page");
                }

                var submittedBy = User.Identity?.Name ?? "anonymous";

                // Find or create submission
                var submission = await _context.FormSubmissions
                    .FirstOrDefaultAsync(s => s.FormTemplateId == formId && s.SubmittedBy == submittedBy && s.Status == FormStatus.Draft);

                if (submission == null)
                {
                    submission = new FormSubmission
                    {
                        FormTemplateId = formId,
                        SubmittedBy = submittedBy,
                        Status = FormStatus.Draft,
                        CurrentPage = currentPage,
                        DataJson = "{}"
                    };
                    _context.FormSubmissions.Add(submission);
                }
                else
                {
                    submission.CurrentPage = currentPage;
                    submission.UpdatedAt = DateTime.UtcNow;
                }

                // Update form data
                var formDataDict = new Dictionary<string, object>();
                foreach (var key in formData.Keys)
                {
                    if (key != "formId" && key != "currentPage")
                    {
                        formDataDict[key] = formData[key].ToString();
                    }
                }

                submission.DataJson = System.Text.Json.JsonSerializer.Serialize(formDataDict);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, submissionId = submission.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-saving form");
                return StatusCode(500, "Error auto-saving form");
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromForm] IFormCollection formData)
        {
            try
            {
                var formId = int.Parse(formData["formId"]);
                var submittedBy = User.Identity?.Name ?? "anonymous";

                // Find existing submission or create new one
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .FirstOrDefaultAsync(s => s.FormTemplateId == formId && s.SubmittedBy == submittedBy && s.Status == FormStatus.Draft);

                if (submission == null)
                {
                    submission = new FormSubmission
                    {
                        FormTemplateId = formId,
                        SubmittedBy = submittedBy,
                        Status = FormStatus.Submitted,
                        CurrentPage = 1,
                        IsComplete = true,
                        SubmittedAt = DateTime.UtcNow
                    };
                    _context.FormSubmissions.Add(submission);
                }
                else
                {
                    submission.Status = FormStatus.Submitted;
                    submission.IsComplete = true;
                    submission.SubmittedAt = DateTime.UtcNow;
                    submission.UpdatedAt = DateTime.UtcNow;
                }

                // Update form data
                var formDataDict = new Dictionary<string, object>();
                foreach (var key in formData.Keys)
                {
                    if (key != "formId")
                    {
                        formDataDict[key] = formData[key].ToString();
                    }
                }

                submission.DataJson = System.Text.Json.JsonSerializer.Serialize(formDataDict);

                // Validate form data
                var formTemplate = await _context.FormTemplates.FindAsync(formId);
                if (formTemplate != null && !await _renderingService.ValidateFormDataAsync(formTemplate, submission.DataJson))
                {
                    return BadRequest(new { success = false, message = "Form validation failed" });
                }

                await _context.SaveChangesAsync();

                // Send notifications
                await _notificationService.SendFormSubmittedNotificationAsync(submission);

                _logger.LogInformation("Form submitted successfully: {SubmissionId}", submission.Id);

                return Ok(new { success = true, submissionId = submission.Id, message = "Form submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting form");
                return StatusCode(500, "Error submitting form");
            }
        }

        [HttpGet("{id}/submissions")]
        public async Task<IActionResult> GetFormSubmissions(int id)
        {
            try
            {
                var submissions = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .Where(s => s.FormTemplateId == id)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return Ok(submissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submissions for form {FormId}", id);
                return StatusCode(500, "Error retrieving submissions");
            }
        }
    }
}
