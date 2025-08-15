using System.Threading.Tasks;
using YourApp.Models;

namespace YourApp.Services
{
    public interface IAIFormGenerationService
    {
        Task<FormTemplate> GenerateFormFromImageAsync(byte[] imageData, string fileName, string generatedBy);
        Task<string> GenerateFormHtmlAsync(string formStructureJson);
        Task<bool> ValidateFormStructureAsync(string formStructureJson);
    }
}
