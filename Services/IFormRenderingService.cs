using System.Threading.Tasks;
using YourApp.Models;

namespace YourApp.Services
{
    public interface IFormRenderingService
    {
        Task<string> RenderFormAsync(FormTemplate formTemplate, FormSubmission? submission = null);
        Task<string> RenderFormPageAsync(FormTemplate formTemplate, int pageNumber, FormSubmission? submission = null);
        Task<string> GetFormProgressAsync(FormTemplate formTemplate, FormSubmission submission);
        Task<bool> ValidateFormDataAsync(FormTemplate formTemplate, string formDataJson);
    }
}
