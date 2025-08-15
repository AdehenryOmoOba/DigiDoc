using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YourApp.Models;

namespace YourApp.Services
{
    public class OpenAIFormGenerationService : IAIFormGenerationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIFormGenerationService> _logger;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAIFormGenerationService(IConfiguration configuration, ILogger<OpenAIFormGenerationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
        }

        public async Task<FormTemplate> GenerateFormFromImageAsync(byte[] imageData, string fileName, string generatedBy)
        {
            try
            {
                _logger.LogInformation("Starting form generation from image: {FileName}", fileName);

                // Convert image to base64
                var base64Image = Convert.ToBase64String(imageData);

                // Create the prompt for GPT Vision
                var prompt = CreateFormGenerationPrompt();

                // Call OpenAI API
                var formStructureJson = await CallOpenAIVisionAPIAsync(base64Image, prompt);

                // Validate the generated structure
                if (!await ValidateFormStructureAsync(formStructureJson))
                {
                    throw new InvalidOperationException("Generated form structure is invalid");
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
    }
}
