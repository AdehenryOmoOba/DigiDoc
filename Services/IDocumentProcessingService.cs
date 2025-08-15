using DigiDocWebApp.Models;

namespace DigiDocWebApp.Services
{
    public interface IDocumentProcessingService
    {
        /// <summary>
        /// Extracts text content from a document file
        /// </summary>
        /// <param name="fileData">The document file data</param>
        /// <param name="fileName">The name of the file</param>
        /// <returns>Extracted text content</returns>
        Task<string> ExtractTextFromDocumentAsync(byte[] fileData, string fileName);

        /// <summary>
        /// Determines if a file type is supported for document processing
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <returns>True if supported, false otherwise</returns>
        bool IsDocumentSupported(string fileName);

        /// <summary>
        /// Gets the supported file extensions
        /// </summary>
        /// <returns>Array of supported extensions</returns>
        string[] GetSupportedExtensions();
    }

    public enum DocumentType
    {
        Image,
        Pdf,
        WordDoc,
        WordDocx,
        Unsupported
    }

    public static class DocumentTypeHelper
    {
        public static DocumentType GetDocumentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" => DocumentType.Image,
                ".pdf" => DocumentType.Pdf,
                ".doc" => DocumentType.WordDoc,
                ".docx" => DocumentType.WordDocx,
                _ => DocumentType.Unsupported
            };
        }

        public static bool IsImageFile(string fileName)
        {
            return GetDocumentType(fileName) == DocumentType.Image;
        }

        public static bool IsDocumentFile(string fileName)
        {
            var type = GetDocumentType(fileName);
            return type == DocumentType.Pdf || type == DocumentType.WordDoc || type == DocumentType.WordDocx;
        }
    }
} 