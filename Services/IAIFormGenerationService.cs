using System.Threading.Tasks;
using DigiDocWebApp.Models;

namespace DigiDocWebApp.Services
{
    public interface IAIFormGenerationService
    {
        Task<FormTemplate> GenerateFormFromImageAsync(byte[] imageData, string fileName, string generatedBy);
        Task<string> GenerateFormHtmlAsync(string formStructureJson);
        Task<bool> ValidateFormStructureAsync(string formStructureJson);
    }
}
