using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YourApp.Models;

namespace YourApp.Services
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
                var formStructure = JsonSerializer.Deserialize<FormStructure>(formTemplate.StructureJson);
                if (formStructure == null || pageNumber > formStructure.Pages.Count)
                {
                    throw new InvalidOperationException("Invalid page number or form structure");
                }

                var page = formStructure.Pages[pageNumber - 1];
                var submissionData = submission != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(submission.DataJson) : new Dictionary<string, object>();

                var html = new StringBuilder();
                
                // Page header
                html.AppendLine($"<div class=\"page-header\">");
                html.AppendLine($"<h3 class=\"page-title\">{page.Title}</h3>");
                html.AppendLine($"<p class=\"page-description\">Page {pageNumber} of {formStructure.Pages.Count}</p>");
                html.AppendLine("</div>");
                
                // Form fields
                html.AppendLine("<div class=\"form-fields\">");
                foreach (var field in page.Fields)
                {
                    html.AppendLine(RenderField(field, submissionData));
                }
                html.AppendLine("</div>");
                
                return html.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering form page: {FormId}, Page: {PageNumber}", formTemplate.Id, pageNumber);
                throw;
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
                var currentPage = submission.CurrentPage;
                var progressPercentage = (currentPage * 100) / totalPages;

                var html = new StringBuilder();
                html.AppendLine("<div class=\"form-progress\">");
                html.AppendLine("<div class=\"progress-container\">");
                
                // Progress bar
                html.AppendLine($"<div class=\"progress-bar\" role=\"progressbar\" style=\"width: {progressPercentage}%\" aria-valuenow=\"{progressPercentage}\" aria-valuemin=\"0\" aria-valuemax=\"100\"></div>");
                
                // Progress steps
                html.AppendLine("<div class=\"progress-steps\">");
                for (int i = 1; i <= totalPages; i++)
                {
                    var stepClass = i <= currentPage ? "step completed" : "step";
                    var stepIcon = i < currentPage ? "✓" : i.ToString();
                    html.AppendLine($"<div class=\"{stepClass}\" data-step=\"{i}\">");
                    html.AppendLine($"<span class=\"step-icon\">{stepIcon}</span>");
                    html.AppendLine($"<span class=\"step-label\">Step {i}</span>");
                    html.AppendLine("</div>");
                }
                html.AppendLine("</div>");
                
                html.AppendLine("</div>");
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
            var value = submissionData.ContainsKey(field.Id) ? submissionData[field.Id]?.ToString() : "";
            var required = field.Required ? "required" : "";
            var requiredClass = field.Required ? "required" : "";

            var html = new StringBuilder();
            html.AppendLine($"<div class=\"form-group {requiredClass}\">");
            
            // Label
            html.AppendLine($"<label for=\"{field.Id}\" class=\"form-label\">{field.Label}");
            if (field.Required)
            {
                html.AppendLine("<span class=\"required-mark\">*</span>");
            }
            html.AppendLine("</label>");
            
            // Input field
            switch (field.Type?.ToLower())
            {
                case "email":
                    html.AppendLine($"<input type=\"email\" id=\"{field.Id}\" name=\"{field.Id}\" class=\"form-control\" value=\"{value}\" {required} />");
                    break;
                case "phone":
                    html.AppendLine($"<input type=\"tel\" id=\"{field.Id}\" name=\"{field.Id}\" class=\"form-control\" value=\"{value}\" {required} />");
                    break;
                case "date":
                    html.AppendLine($"<input type=\"date\" id=\"{field.Id}\" name=\"{field.Id}\" class=\"form-control\" value=\"{value}\" {required} />");
                    break;
                case "number":
                    html.AppendLine($"<input type=\"number\" id=\"{field.Id}\" name=\"{field.Id}\" class=\"form-control\" value=\"{value}\" {required} />");
                    break;
                case "textarea":
                    html.AppendLine($"<textarea id=\"{field.Id}\" name=\"{field.Id}\" class=\"form-control\" rows=\"3\" {required}>{value}</textarea>");
                    break;
                case "select":
                    html.AppendLine($"<select id=\"{field.Id}\" name=\"{field.Id}\" class=\"form-select\" {required}>");
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
                            html.AppendLine($"<input type=\"radio\" id=\"{field.Id}_{option}\" name=\"{field.Id}\" value=\"{option}\" class=\"form-check-input\" {checkedAttr} {required} />");
                            html.AppendLine($"<label class=\"form-check-label\" for=\"{field.Id}_{option}\">{option}</label>");
                            html.AppendLine("</div>");
                        }
                    }
                    break;
                case "checkbox":
                    html.AppendLine($"<div class=\"form-check\">");
                    html.AppendLine($"<input type=\"checkbox\" id=\"{field.Id}\" name=\"{field.Id}\" class=\"form-check-input\" value=\"true\" {(value == "true" ? "checked" : "")} {required} />");
                    html.AppendLine($"<label class=\"form-check-label\" for=\"{field.Id}\">{field.Label}</label>");
                    html.AppendLine("</div>");
                    break;
                default:
                    html.AppendLine($"<input type=\"text\" id=\"{field.Id}\" name=\"{field.Id}\" class=\"form-control\" value=\"{value}\" {required} />");
                    break;
            }
            
            // Validation message
            html.AppendLine($"<div class=\"invalid-feedback\" id=\"{field.Id}-error\"></div>");
            
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
            public string? FormName { get; set; }
            public string? Description { get; set; }
            public List<Page> Pages { get; set; } = new List<Page>();
        }

        private class Page
        {
            public int PageNumber { get; set; }
            public string? Title { get; set; }
            public List<Field> Fields { get; set; } = new List<Field>();
        }

        private class Field
        {
            public string? Id { get; set; }
            public string? Type { get; set; }
            public string? Label { get; set; }
            public string? Placeholder { get; set; }
            public bool Required { get; set; }
            public Validation? Validation { get; set; }
            public Position? Position { get; set; }
        }

        private class Validation
        {
            public int? MinLength { get; set; }
            public int? MaxLength { get; set; }
            public string? Pattern { get; set; }
            public List<string>? Options { get; set; }
        }

        private class Position
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}
