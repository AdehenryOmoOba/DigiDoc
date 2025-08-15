using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DigiDocWebApp.Models;

namespace DigiDocWebApp.Services
{
    public class OpenAIFormGenerationService : IAIFormGenerationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIFormGenerationService> _logger;
        private readonly IDocumentProcessingService _documentProcessor;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAIFormGenerationService(
            IConfiguration configuration, 
            ILogger<OpenAIFormGenerationService> logger,
            IDocumentProcessingService documentProcessor)
        {
            _configuration = configuration;
            _logger = logger;
            _documentProcessor = documentProcessor;
            _apiKey = _configuration["OpenAI:ApiKey"] ?? "your-openai-api-key-here";
        }

        public async Task<FormTemplate> GenerateFormFromImageAsync(byte[] fileData, string fileName, string generatedBy)
        {
            try
            {
                _logger.LogInformation("Starting form generation from file: {FileName}", fileName);

                string formStructureJson;
                var documentType = DocumentTypeHelper.GetDocumentType(fileName);
                
                // Check if OpenAI API key is configured
                if (string.IsNullOrEmpty(_apiKey) || _apiKey == "your-openai-api-key-here")
                {
                    _logger.LogWarning("OpenAI API key not configured. Using demo form structure.");
                    formStructureJson = CreateDemoFormStructure(fileName);
                }
                else
                {
                    try
                    {
                        if (documentType == DocumentType.Image)
                        {
                            // Handle image files
                            var base64Image = Convert.ToBase64String(fileData);
                var prompt = CreateFormGenerationPrompt();
                            formStructureJson = await CallOpenAIVisionAPIAsync(base64Image, prompt);
                        }
                        else if (DocumentTypeHelper.IsDocumentFile(fileName))
                        {
                            // Handle document files (PDF, DOC, DOCX)
                            var extractedText = await _documentProcessor.ExtractTextFromDocumentAsync(fileData, fileName);
                            var prompt = CreateDocumentFormGenerationPrompt();
                            formStructureJson = await CallOpenAITextAPIAsync(extractedText, prompt);
                        }
                        else
                        {
                            throw new NotSupportedException($"File type not supported: {Path.GetExtension(fileName)}");
                        }
                    }
                    catch (Exception apiEx)
                    {
                        _logger.LogWarning(apiEx, "OpenAI API call failed. Using demo form structure.");
                        formStructureJson = CreateDemoFormStructure(fileName);
                    }
                }

                // Validate the generated structure
                if (!await ValidateFormStructureAsync(formStructureJson))
                {
                    _logger.LogWarning("Generated form structure is invalid. Using fallback structure.");
                    formStructureJson = CreateDemoFormStructure(fileName);
                }

                // Create form template
                var formTemplate = new FormTemplate
                {
                    Name = Path.GetFileNameWithoutExtension(fileName),
                    Description = $"AI-generated form from {fileName}",
                    StructureJson = formStructureJson,
                    OriginalImagePath = fileName,
                    GeneratedBy = generatedBy,
                    GeneratedAt = DateTime.UtcNow,
                    CreatedBy = generatedBy,
                    TotalPages = CalculateTotalPages(formStructureJson)
                };

                _logger.LogInformation("Successfully generated form template: {FormName}", formTemplate.Name);
                return formTemplate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating form from image: {FileName}", fileName);
                throw;
            }
        }

        public async Task<string> GenerateFormHtmlAsync(string formStructureJson)
        {
            try
            {
                var prompt = CreateHtmlGenerationPrompt();
                var html = await CallOpenAITextAPIAsync(prompt + "\n\nForm Structure:\n" + formStructureJson);
                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating HTML from form structure");
                throw;
            }
        }

        public async Task<bool> ValidateFormStructureAsync(string formStructureJson)
        {
            try
            {
                // Basic JSON validation
                var formStructure = JsonSerializer.Deserialize<object>(formStructureJson);
                return formStructure != null;
            }
            catch
            {
                return false;
            }
        }

        private string CreateFormGenerationPrompt()
        {
            return @"You are an expert at analyzing form images and converting them to structured JSON format.

Please analyze the provided form image and generate a JSON structure that represents the form fields, layout, and validation rules.

The JSON should follow this structure:
{
  ""formName"": ""string"",
  ""description"": ""string"",
  ""pages"": [
    {
      ""pageNumber"": 1,
      ""title"": ""string"",
      ""fields"": [
        {
          ""id"": ""string"",
          ""type"": ""text|email|phone|date|number|select|radio|checkbox|textarea"",
          ""label"": ""string"",
          ""placeholder"": ""string"",
          ""required"": true|false,
          ""validation"": {
            ""minLength"": number,
            ""maxLength"": number,
            ""pattern"": ""regex"",
            ""options"": [""option1"", ""option2""]
          },
          ""position"": {
            ""x"": number,
            ""y"": number,
            ""width"": number,
            ""height"": number
          }
        }
      ]
    }
  ]
}

Please ensure:
1. All form fields are accurately identified
2. Field types are appropriate (text, email, phone, date, etc.)
3. Required fields are marked
4. Validation rules are included where applicable
5. The structure is valid JSON
6. Field positions are estimated based on visual layout

Return only the JSON structure, no additional text.";
        }

        private string CreateHtmlGenerationPrompt()
        {
            return @"You are an expert at generating modern, responsive HTML forms from JSON structure.

Please convert the provided JSON form structure into clean, modern HTML with the following requirements:

1. Use semantic HTML5 elements
2. Include proper accessibility attributes (aria-labels, etc.)
3. Use modern CSS classes for styling
4. Include client-side validation
5. Make the form responsive
6. Use Bootstrap 5 classes for styling
7. Include proper form structure with labels and inputs
8. Add appropriate input types and attributes
9. Include validation messages
10. Make the form user-friendly and professional

Generate only the HTML code, no additional text or explanations.";
        }

        private async Task<string> CallOpenAIVisionAPIAsync(string base64Image, string prompt)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var requestBody = new
            {
                model = "gpt-4-vision-preview",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = prompt },
                            new
                            {
                                type = "image_url",
                                image_url = new { url = $"data:image/jpeg;base64,{base64Image}" }
                            }
                        }
                    }
                },
                max_tokens = 4000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);

            return responseObject?.Choices?[0]?.Message?.Content?.Trim() ?? "";
        }

        private async Task<string> CallOpenAITextAPIAsync(string prompt)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 4000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);

            return responseObject?.Choices?[0]?.Message?.Content?.Trim() ?? "";
        }

        private int CalculateTotalPages(string formStructureJson)
        {
            try
            {
                var formStructure = JsonSerializer.Deserialize<FormStructure>(formStructureJson);
                return formStructure?.Pages?.Count ?? 1;
            }
            catch
            {
                return 1;
            }
        }

        private string CreateDemoFormStructure(string fileName)
        {
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            return JsonSerializer.Serialize(new
            {
                formName = baseName,
                description = $"Demo form generated from {fileName}",
                pages = new object[]
                {
                    new
                    {
                        pageNumber = 1,
                        title = "Basic Information",
                        fields = new object[]
                        {
                            new
                            {
                                id = "firstName",
                                type = "text",
                                label = "First Name",
                                placeholder = "Enter your first name",
                                required = true,
                                position = new { x = 10, y = 50, width = 200, height = 30 }
                            },
                            new
                            {
                                id = "lastName",
                                type = "text",
                                label = "Last Name",
                                placeholder = "Enter your last name",
                                required = true,
                                position = new { x = 250, y = 50, width = 200, height = 30 }
                            },
                            new
                            {
                                id = "email",
                                type = "email",
                                label = "Email Address",
                                placeholder = "Enter your email",
                                required = true,
                                validation = new { pattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$" },
                                position = new { x = 10, y = 120, width = 300, height = 30 }
                            },
                            new
                            {
                                id = "phone",
                                type = "tel",
                                label = "Phone Number",
                                placeholder = "(555) 123-4567",
                                required = false,
                                position = new { x = 10, y = 190, width = 200, height = 30 }
                            },
                            new
                            {
                                id = "department",
                                type = "select",
                                label = "Department",
                                required = true,
                                validation = new
                                {
                                    options = new[] { "Human Resources", "Finance", "IT", "Marketing", "Operations", "Other" }
                                },
                                position = new { x = 250, y = 190, width = 200, height = 30 }
                            }
                        }
                    },
                    new
                    {
                        pageNumber = 2,
                        title = "Additional Details",
                        fields = new object[]
                        {
                            new
                            {
                                id = "comments",
                                type = "textarea",
                                label = "Comments or Additional Information",
                                placeholder = "Please provide any additional information...",
                                required = false,
                                position = new { x = 10, y = 50, width = 500, height = 100 }
                            },
                            new
                            {
                                id = "agreement",
                                type = "checkbox",
                                label = "I agree to the terms and conditions",
                                required = true,
                                position = new { x = 10, y = 200, width = 300, height = 20 }
                            }
                        }
                    }
                }
            });
        }

        // Response models for OpenAI API
        private class OpenAIResponse
        {
            public Choice[]? Choices { get; set; }
        }

        private class Choice
        {
            public Message? Message { get; set; }
        }

        private class Message
        {
            public string? Content { get; set; }
        }

        private class FormStructure
        {
            public List<Page>? Pages { get; set; }
        }

        private class Page
        {
            public int PageNumber { get; set; }
            public string? Title { get; set; }
            public List<Field>? Fields { get; set; }
        }

        private class Field
        {
            public string? Id { get; set; }
            public string? Type { get; set; }
            public string? Label { get; set; }
            public bool Required { get; set; }
        }

        private string CreateDocumentFormGenerationPrompt()
        {
            return @"You are an expert form designer. Analyze the provided document text (extracted from PDF/Word) and recreate it as a structured JSON format.

The text contains form content with field labels, instructions, and structure. Convert this into a digital form format.

Please return ONLY a valid JSON object with this exact structure:
{
  ""formName"": ""string"",
  ""description"": ""string"",
  ""pages"": [
    {
      ""pageNumber"": 1,
      ""title"": ""string"",
      ""fields"": [
        {
          ""id"": ""string"",
          ""type"": ""text|email|tel|select|radio|checkbox|textarea|date|number"",
          ""label"": ""string"",
          ""placeholder"": ""string (optional)"",
          ""required"": true/false,
          ""validation"": {
            ""pattern"": ""regex (optional)"",
            ""options"": [""array of options for select/radio""]
          },
          ""position"": {
            ""x"": 0,
            ""y"": 0,
            ""width"": 200,
            ""height"": 30
          }
        }
      ]
    }
  ]
}

Requirements:
1. Identify form fields from text patterns like 'Name: ____', checkboxes, etc.
2. Extract field labels and convert to appropriate input types
3. For options/choices, create select or radio fields
4. Group related content into logical pages
5. Use meaningful field IDs (camelCase)
6. Infer required fields from context (*, required, mandatory)
7. Create appropriate validation patterns for emails, phones, etc.
8. Position fields logically in a vertical layout
9. Do not include any text outside the JSON object";
        }

        private async Task<string> CallOpenAITextAPIAsync(string documentText, string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-4",
                    messages = new[]
                    {
                        new { role = "system", content = prompt },
                        new { role = "user", content = $"Here is the document text to analyze:\n\n{documentText}" }
                    },
                    max_tokens = 4000,
                    temperature = 0.1
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await httpClient.PostAsync(_apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API call failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                    throw new HttpRequestException($"OpenAI API call failed: {response.StatusCode}");
                }

                var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);
                var generatedContent = openAIResponse?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrEmpty(generatedContent))
                {
                    throw new InvalidOperationException("OpenAI returned empty response");
                }

                // Extract JSON from the response (in case there's extra text)
                var startIndex = generatedContent.IndexOf('{');
                var lastIndex = generatedContent.LastIndexOf('}');
                
                if (startIndex >= 0 && lastIndex > startIndex)
                {
                    generatedContent = generatedContent.Substring(startIndex, lastIndex - startIndex + 1);
                }

                _logger.LogInformation("Successfully generated form structure from document text");
                return generatedContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI Text API");
                throw;
            }
        }
    }
}
