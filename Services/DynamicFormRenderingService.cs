using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DigiDocWebApp.Models;

namespace DigiDocWebApp.Services
{
    public class DynamicFormRenderingService : IFormRenderingService
    {
        private readonly ILogger<DynamicFormRenderingService> _logger;
        private readonly IAIFormGenerationService _aiService;

        public DynamicFormRenderingService(ILogger<DynamicFormRenderingService> logger, IAIFormGenerationService aiService)
        {
            _logger = logger;
            _aiService = aiService;
        }

        public async Task<string> RenderFormAsync(FormTemplate formTemplate, FormSubmission? submission = null)
        {
            try
            {
                var formStructure = JsonSerializer.Deserialize<FormStructure>(formTemplate.StructureJson);
                if (formStructure == null)
                {
                    throw new InvalidOperationException("Invalid form structure");
                }

                var html = new StringBuilder();
                
                // Form container
                html.AppendLine("<div class=\"form-container\" data-form-id=\"" + formTemplate.Id + "\">");
                
                // Progress bar
                html.AppendLine(await GetFormProgressAsync(formTemplate, submission));
                
                // Form content
                html.AppendLine("<div class=\"form-content\">");
                
                if (formStructure.Pages.Count == 1)
                {
                    // Single page form
                    html.AppendLine(await RenderFormPageAsync(formTemplate, 1, submission));
                }
                else
                {
                    // Multi-page form
                    for (int i = 1; i <= formStructure.Pages.Count; i++)
                    {
                        var pageClass = i == 1 ? "form-page active" : "form-page";
                        html.AppendLine($"<div class=\"{pageClass}\" data-page=\"{i}\">");
                        html.AppendLine(await RenderFormPageAsync(formTemplate, i, submission));
                        html.AppendLine("</div>");
                    }
                    
                    // Navigation buttons
                    html.AppendLine(GenerateNavigationButtons(formStructure.Pages.Count));
                }
                
                html.AppendLine("</div>"); // form-content
                html.AppendLine("</div>"); // form-container
                
                // Add JavaScript for form functionality
                html.AppendLine(GenerateFormJavaScript(formTemplate.Id, formStructure.Pages.Count));
                
                return html.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering form: {FormId}", formTemplate.Id);
                throw;
            }
        }

        public async Task<string> RenderFormPageAsync(FormTemplate formTemplate, int pageNumber, FormSubmission? submission = null)
        {
            try
            {
                _logger.LogInformation("Attempting to render form page {PageNumber} for form {FormId}", pageNumber, formTemplate.Id);
                _logger.LogDebug("Form structure JSON: {Json}", formTemplate.StructureJson);
                
                var formStructure = JsonSerializer.Deserialize<FormStructure>(formTemplate.StructureJson);
                
                if (formStructure == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize form structure. JSON: {formTemplate.StructureJson}");
                }
                
                _logger.LogInformation("Deserialized form structure successfully. Pages count: {PagesCount}", formStructure.Pages?.Count ?? 0);
                
                if (formStructure.Pages == null || formStructure.Pages.Count == 0)
                {
                    throw new InvalidOperationException($"Form structure has no pages. JSON: {formTemplate.StructureJson}");
                }
                
                if (pageNumber < 1 || pageNumber > formStructure.Pages.Count)
                {
                    throw new InvalidOperationException($"Invalid page number {pageNumber}. Form has {formStructure.Pages.Count} pages. JSON: {formTemplate.StructureJson}");
                }

                var page = formStructure.Pages[pageNumber - 1];
                var submissionData = new Dictionary<string, object>();
                
                if (submission != null && !string.IsNullOrEmpty(submission.DataJson))
                {
                    _logger.LogInformation("Form submission found with DataJson: {DataJson}", submission.DataJson);
                    try
                    {
                        submissionData = JsonSerializer.Deserialize<Dictionary<string, object>>(submission.DataJson) ?? new Dictionary<string, object>();
                        _logger.LogInformation("Successfully parsed {Count} fields from submission data", submissionData.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse submission DataJson: {DataJson}", submission.DataJson);
                    }
                }
                else
                {
                    _logger.LogInformation("No existing submission data found");
                }

                var html = new StringBuilder();
                
                // Page header
                html.AppendLine($"<div class=\"page-header\">");
                html.AppendLine($"<h3 class=\"page-title\">{page.Title ?? "Form Page"}</h3>");
                html.AppendLine($"<p class=\"page-description\">Page {pageNumber} of {formStructure.Pages.Count}</p>");
                html.AppendLine("</div>");
                
                // Form fields
                html.AppendLine("<div class=\"form-fields\">");
                
                if (page.Fields != null && page.Fields.Any())
                {
                    foreach (var field in page.Fields)
                    {
                        html.AppendLine(RenderField(field, submissionData));
                    }
                }
                else
                {
                    html.AppendLine("<div class='alert alert-info'>No fields found for this page.</div>");
                }
                
                html.AppendLine("</div>");
                
                return html.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering form page: {FormId}, Page: {PageNumber}. JSON: {Json}", formTemplate.Id, pageNumber, formTemplate.StructureJson);
                
                // Return detailed error information for debugging
                return $@"
                    <div class='alert alert-danger'>
                        <h6>Error rendering form page</h6>
                        <p><strong>Form ID:</strong> {formTemplate.Id}</p>
                        <p><strong>Page Number:</strong> {pageNumber}</p>
                        <p><strong>Error:</strong> {ex.Message}</p>
                        <details>
                            <summary>Form Structure JSON</summary>
                            <pre style='max-height: 300px; overflow-y: auto; font-size: 12px;'>{formTemplate.StructureJson}</pre>
                        </details>
                        <details>
                            <summary>Full Exception</summary>
                            <pre style='max-height: 200px; overflow-y: auto; font-size: 11px;'>{ex}</pre>
                        </details>
                    </div>";
            }
        }

        public async Task<string> GetFormProgressAsync(FormTemplate formTemplate, FormSubmission submission)
        {
            try
            {
                var formStructure = JsonSerializer.Deserialize<FormStructure>(formTemplate.StructureJson);
                if (formStructure == null)
                {
                    return "";
                }

                var totalPages = formStructure.Pages.Count;
                var currentPage = submission?.CurrentPage ?? 1;
                var progressPercentage = ((currentPage - 1) * 100) / Math.Max(totalPages - 1, 1);

                var html = new StringBuilder();
                
                // Enhanced Progress Container
                html.AppendLine("<div class=\"form-progress-container mb-4\">");
                
                // Form header with title and progress info
                html.AppendLine("<div class=\"form-header d-flex justify-content-between align-items-center mb-3\">");
                html.AppendLine($"<h4 class=\"mb-0\">{formTemplate.Name}</h4>");
                html.AppendLine($"<span class=\"progress-text\">Step {currentPage} of {totalPages}</span>");
                html.AppendLine("</div>");
                
                // Modern Progress Bar
                html.AppendLine("<div class=\"progress mb-3\" style=\"height: 8px;\">");
                html.AppendLine($"<div class=\"progress-bar bg-success progress-bar-striped progress-bar-animated\" role=\"progressbar\" style=\"width: {progressPercentage}%\" aria-valuenow=\"{progressPercentage}\" aria-valuemin=\"0\" aria-valuemax=\"100\"></div>");
                html.AppendLine("</div>");
                
                // Step indicators with page titles
                html.AppendLine("<div class=\"progress-steps d-flex justify-content-between align-items-center\">");
                for (int i = 1; i <= totalPages; i++)
                {
                    var page = formStructure.Pages.FirstOrDefault(p => p.PageNumber == i);
                    var pageTitle = page?.Title ?? $"Step {i}";
                    
                    var stepClass = "step";
                    var iconClass = "step-icon";
                    var icon = i.ToString();
                    
                    if (i < currentPage)
                    {
                        stepClass += " completed";
                        iconClass += " bg-success text-white";
                        icon = "✓";
                    }
                    else if (i == currentPage)
                    {
                        stepClass += " active";
                        iconClass += " bg-primary text-white";
                    }
                    else
                    {
                        iconClass += " bg-light text-muted";
                    }
                    
                    html.AppendLine($"<div class=\"{stepClass} text-center\" data-step=\"{i}\">");
                    html.AppendLine($"<div class=\"{iconClass} rounded-circle d-flex align-items-center justify-content-center mx-auto mb-1\" style=\"width: 32px; height: 32px; font-size: 14px; font-weight: bold;\">{icon}</div>");
                    html.AppendLine($"<small class=\"step-label d-block text-truncate\" style=\"max-width: 80px;\" title=\"{pageTitle}\">{pageTitle}</small>");
                    html.AppendLine("</div>");
                    
                    // Add connector line between steps (except last step)
                    if (i < totalPages)
                    {
                        var lineClass = i < currentPage ? "bg-success" : "bg-light";
                        html.AppendLine($"<div class=\"step-connector flex-grow-1 {lineClass}\" style=\"height: 2px; margin: 0 8px;\"></div>");
                    }
                }
                html.AppendLine("</div>");
                
                // Save progress indicator
                if (submission != null && submission.Status == FormStatus.Draft)
                {
                    html.AppendLine("<div class=\"save-indicator mt-2\">");
                    html.AppendLine("<small class=\"text-muted\"><i class=\"fas fa-save\"></i> Progress is automatically saved</small>");
                    html.AppendLine("</div>");
                }
                
                html.AppendLine("</div>");
                
                return html.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating form progress: {FormId}", formTemplate.Id);
                return "";
            }
        }

        public async Task<bool> ValidateFormDataAsync(FormTemplate formTemplate, string formDataJson)
        {
            try
            {
                var formStructure = JsonSerializer.Deserialize<FormStructure>(formTemplate.StructureJson);
                var formData = JsonSerializer.Deserialize<Dictionary<string, object>>(formDataJson);
                
                if (formStructure == null || formData == null)
                {
                    return false;
                }

                // Validate required fields
                foreach (var page in formStructure.Pages)
                {
                    foreach (var field in page.Fields)
                    {
                        if (field.Required)
                        {
                            if (!formData.ContainsKey(field.Id) || formData[field.Id] == null || 
                                string.IsNullOrWhiteSpace(formData[field.Id].ToString()))
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string RenderField(Field field, Dictionary<string, object> submissionData)
        {
            var fieldId = field.Id ?? "";
            var value = !string.IsNullOrEmpty(fieldId) && submissionData.ContainsKey(fieldId) ? submissionData[fieldId]?.ToString() : "";
            var required = field.Required ? "required" : "";
            var requiredClass = field.Required ? "required" : "";
            
            if (!string.IsNullOrEmpty(value))
            {
                _logger.LogInformation("Rendering field {FieldId} with existing value: {Value}", fieldId, value);
            }

            var html = new StringBuilder();
            html.AppendLine($"<div class=\"form-group {requiredClass}\">");
            
            // Label
            html.AppendLine($"<label for=\"{fieldId}\" class=\"form-label\">{field.Label}");
            if (field.Required)
            {
                html.AppendLine("<span class=\"required-mark\">*</span>");
            }
            html.AppendLine("</label>");
            
            // Input field
            switch (field.Type?.ToLower())
            {
                case "email":
                    html.AppendLine($"<input type=\"email\" id=\"{fieldId}\" name=\"{fieldId}\" class=\"form-control\" value=\"{value}\" {required} />");
                    break;
                case "phone":
                    html.AppendLine($"<input type=\"tel\" id=\"{fieldId}\" name=\"{fieldId}\" class=\"form-control\" value=\"{value}\" {required} />");
                    break;
                case "date":
                    html.AppendLine($"<input type=\"date\" id=\"{fieldId}\" name=\"{fieldId}\" class=\"form-control\" value=\"{value}\" {required} />");
                    break;
                case "number":
                    html.AppendLine($"<input type=\"number\" id=\"{fieldId}\" name=\"{fieldId}\" class=\"form-control\" value=\"{value}\" {required} />");
                    break;
                case "textarea":
                    html.AppendLine($"<textarea id=\"{fieldId}\" name=\"{fieldId}\" class=\"form-control\" rows=\"3\" {required}>{value}</textarea>");
                    break;
                case "select":
                    html.AppendLine($"<select id=\"{fieldId}\" name=\"{fieldId}\" class=\"form-select\" {required}>");
                    html.AppendLine("<option value=\"\">Select an option</option>");
                    if (field.Validation?.Options != null)
                    {
                        foreach (var option in field.Validation.Options)
                        {
                            var selected = value == option ? "selected" : "";
                            html.AppendLine($"<option value=\"{option}\" {selected}>{option}</option>");
                        }
                    }
                    html.AppendLine("</select>");
                    break;
                case "radio":
                    if (field.Validation?.Options != null)
                    {
                        foreach (var option in field.Validation.Options)
                        {
                            var checkedAttr = value == option ? "checked" : "";
                            html.AppendLine($"<div class=\"form-check\">");
                            html.AppendLine($"<input type=\"radio\" id=\"{fieldId}_{option}\" name=\"{fieldId}\" value=\"{option}\" class=\"form-check-input\" {checkedAttr} {required} />");
                            html.AppendLine($"<label class=\"form-check-label\" for=\"{fieldId}_{option}\">{option}</label>");
                            html.AppendLine("</div>");
                        }
                    }
                    break;
                case "checkbox":
                    if (field.Validation?.Options != null && field.Validation.Options.Count > 0)
                    {
                        // Multiple checkbox options
                        var selectedValues = new List<string>();
                        if (!string.IsNullOrEmpty(value))
                        {
                            try
                            {
                                selectedValues = JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>();
                            }
                            catch
                            {
                                // If not JSON, treat as single value
                                selectedValues = new List<string> { value };
                            }
                        }
                        
                        foreach (var option in field.Validation.Options)
                        {
                            var checkedAttr = selectedValues.Contains(option) ? "checked" : "";
                            html.AppendLine($"<div class=\"form-check\">");
                            html.AppendLine($"<input type=\"checkbox\" id=\"{fieldId}_{option}\" name=\"{fieldId}\" value=\"{option}\" class=\"form-check-input\" {checkedAttr} />");
                            html.AppendLine($"<label class=\"form-check-label\" for=\"{fieldId}_{option}\">{option}</label>");
                            html.AppendLine("</div>");
                        }
                    }
                    else
                    {
                        // Single checkbox
                        html.AppendLine($"<div class=\"form-check\">");
                        html.AppendLine($"<input type=\"checkbox\" id=\"{fieldId}\" name=\"{fieldId}\" class=\"form-check-input\" value=\"true\" {(value == "true" || value == "True" ? "checked" : "")} {required} />");
                        html.AppendLine($"<label class=\"form-check-label\" for=\"{fieldId}\">{field.Label}</label>");
                        html.AppendLine("</div>");
                    }
                    break;
                default:
                    html.AppendLine($"<input type=\"text\" id=\"{fieldId}\" name=\"{fieldId}\" class=\"form-control\" value=\"{value}\" {required} />");
                    break;
            }
            
            // Validation message
            html.AppendLine($"<div class=\"invalid-feedback\" id=\"{fieldId}-error\"></div>");
            
            html.AppendLine("</div>");
            
            return html.ToString();
        }

        private string GenerateNavigationButtons(int totalPages)
        {
            var html = new StringBuilder();
            html.AppendLine("<div class=\"form-navigation\">");
            html.AppendLine("<div class=\"d-flex justify-content-between\">");
            html.AppendLine("<button type=\"button\" class=\"btn btn-secondary\" id=\"prev-btn\" style=\"display: none;\">Previous</button>");
            html.AppendLine("<button type=\"button\" class=\"btn btn-primary\" id=\"next-btn\">Next</button>");
            html.AppendLine("<button type=\"button\" class=\"btn btn-success\" id=\"submit-btn\" style=\"display: none;\">Submit Form</button>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            return html.ToString();
        }

        private string GenerateFormJavaScript(int formId, int totalPages)
        {
            return $@"
<script>
document.addEventListener('DOMContentLoaded', function() {{
    const formContainer = document.querySelector('[data-form-id=""{formId}""]');
    const pages = formContainer.querySelectorAll('.form-page');
    const progressSteps = formContainer.querySelectorAll('.progress-steps .step');
    const prevBtn = document.getElementById('prev-btn');
    const nextBtn = document.getElementById('next-btn');
    const submitBtn = document.getElementById('submit-btn');
    
    let currentPage = 1;
    const totalPages = {totalPages};
    
    // Initialize form
    updateNavigation();
    updateProgress();
    
    // Event listeners
    prevBtn.addEventListener('click', goToPreviousPage);
    nextBtn.addEventListener('click', goToNextPage);
    submitBtn.addEventListener('click', submitForm);
    
    // Auto-save on field change
    const formFields = formContainer.querySelectorAll('input, select, textarea');
    formFields.forEach(field => {{
        field.addEventListener('change', autoSave);
    }});
    
    function goToPreviousPage() {{
        if (currentPage > 1) {{
            pages[currentPage - 1].classList.remove('active');
            currentPage--;
            pages[currentPage - 1].classList.add('active');
            updateNavigation();
            updateProgress();
            autoSave();
        }}
    }}
    
    function goToNextPage() {{
        if (validateCurrentPage()) {{
            if (currentPage < totalPages) {{
                pages[currentPage - 1].classList.remove('active');
                currentPage++;
                pages[currentPage - 1].classList.add('active');
                updateNavigation();
                updateProgress();
                autoSave();
            }}
        }}
    }}
    
    function updateNavigation() {{
        prevBtn.style.display = currentPage > 1 ? 'block' : 'none';
        nextBtn.style.display = currentPage < totalPages ? 'block' : 'none';
        submitBtn.style.display = currentPage === totalPages ? 'block' : 'none';
    }}
    
    function updateProgress() {{
        progressSteps.forEach((step, index) => {{
            if (index + 1 <= currentPage) {{
                step.classList.add('completed');
            }} else {{
                step.classList.remove('completed');
            }}
        }});
    }}
    
    function validateCurrentPage() {{
        const currentPageElement = pages[currentPage - 1];
        const requiredFields = currentPageElement.querySelectorAll('[required]');
        let isValid = true;
        
        requiredFields.forEach(field => {{
            if (!field.value.trim()) {{
                field.classList.add('is-invalid');
                isValid = false;
            }} else {{
                field.classList.remove('is-invalid');
            }}
        }});
        
        return isValid;
    }}
    
    function autoSave() {{
        const formData = new FormData();
        const fields = formContainer.querySelectorAll('input, select, textarea');
        
        fields.forEach(field => {{
            if (field.type === 'checkbox') {{
                formData.append(field.name, field.checked);
            }} else {{
                formData.append(field.name, field.value);
            }}
        }});
        
        formData.append('currentPage', currentPage);
        
        // Send auto-save request
        fetch('/api/forms/autosave', {{
            method: 'POST',
            body: formData
        }}).catch(error => console.error('Auto-save failed:', error));
    }}
    
    function submitForm() {{
        if (validateCurrentPage()) {{
            const formData = new FormData();
            const fields = formContainer.querySelectorAll('input, select, textarea');
            
            fields.forEach(field => {{
                if (field.type === 'checkbox') {{
                    formData.append(field.name, field.checked);
                }} else {{
                    formData.append(field.name, field.value);
                }}
            }});
            
            // Submit form
            fetch('/api/forms/submit', {{
                method: 'POST',
                body: formData
            }})
            .then(response => response.json())
            .then(data => {{
                if (data.success) {{
                    alert('Form submitted successfully!');
                    window.location.href = '/forms';
                }} else {{
                    alert('Error submitting form: ' + data.message);
                }}
            }})
            .catch(error => {{
                console.error('Submit failed:', error);
                alert('Error submitting form');
            }});
        }}
    }}
}});
</script>";
        }

        // Form structure models
        private class FormStructure
        {
            [JsonPropertyName("formName")]
            public string? FormName { get; set; }
            
            [JsonPropertyName("description")]
            public string? Description { get; set; }
            
            [JsonPropertyName("pages")]
            public List<Page> Pages { get; set; } = new List<Page>();
        }

        private class Page
        {
            [JsonPropertyName("pageNumber")]
            public int PageNumber { get; set; }
            
            [JsonPropertyName("title")]
            public string? Title { get; set; }
            
            [JsonPropertyName("fields")]
            public List<Field> Fields { get; set; } = new List<Field>();
        }

        private class Field
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            
            [JsonPropertyName("label")]
            public string? Label { get; set; }
            
            [JsonPropertyName("placeholder")]
            public string? Placeholder { get; set; }
            
            [JsonPropertyName("required")]
            public bool Required { get; set; }
            
            [JsonPropertyName("validation")]
            public Validation? Validation { get; set; }
            
            [JsonPropertyName("position")]
            public Position? Position { get; set; }
        }

        private class Validation
        {
            [JsonPropertyName("minLength")]
            public int? MinLength { get; set; }
            
            [JsonPropertyName("maxLength")]
            public int? MaxLength { get; set; }
            
            [JsonPropertyName("pattern")]
            public string? Pattern { get; set; }
            
            [JsonPropertyName("options")]
            public List<string>? Options { get; set; }
        }

        private class Position
        {
            [JsonPropertyName("x")]
            public int X { get; set; }
            
            [JsonPropertyName("y")]
            public int Y { get; set; }
            
            [JsonPropertyName("width")]
            public int Width { get; set; }
            
            [JsonPropertyName("height")]
            public int Height { get; set; }
        }
    }
}
