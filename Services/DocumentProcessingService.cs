using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DigiDocWebApp.Services
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly ILogger<DocumentProcessingService> _logger;
        private readonly string[] _supportedExtensions = { ".pdf", ".doc", ".docx" };

        public DocumentProcessingService(ILogger<DocumentProcessingService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ExtractTextFromDocumentAsync(byte[] fileData, string fileName)
        {
            try
            {
                var documentType = DocumentTypeHelper.GetDocumentType(fileName);
                
                return documentType switch
                {
                    DocumentType.Pdf => await ExtractTextFromPdfAsync(fileData),
                    DocumentType.WordDocx => await ExtractTextFromDocxAsync(fileData),
                    DocumentType.WordDoc => await ExtractTextFromDocAsync(fileData),
                    _ => throw new NotSupportedException($"Document type not supported: {Path.GetExtension(fileName)}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from document: {FileName}", fileName);
                throw;
            }
        }

        public bool IsDocumentSupported(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _supportedExtensions.Contains(extension);
        }

        public string[] GetSupportedExtensions()
        {
            return _supportedExtensions.ToArray();
        }

        private async Task<string> ExtractTextFromPdfAsync(byte[] fileData)
        {
            try
            {
                using var memoryStream = new MemoryStream(fileData);
                using var pdfReader = new PdfReader(memoryStream);
                using var pdfDocument = new PdfDocument(pdfReader);

                var textBuilder = new StringBuilder();
                var strategy = new SimpleTextExtractionStrategy();

                for (int pageNumber = 1; pageNumber <= pdfDocument.GetNumberOfPages(); pageNumber++)
                {
                    var page = pdfDocument.GetPage(pageNumber);
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        textBuilder.AppendLine($"--- Page {pageNumber} ---");
                        textBuilder.AppendLine(pageText);
                        textBuilder.AppendLine();
                    }
                }

                var extractedText = textBuilder.ToString();
                _logger.LogInformation("Successfully extracted {Length} characters from PDF document", extractedText.Length);
                
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                throw new InvalidOperationException("Failed to extract text from PDF document", ex);
            }
        }

        private async Task<string> ExtractTextFromDocxAsync(byte[] fileData)
        {
            try
            {
                using var memoryStream = new MemoryStream(fileData);
                using var wordDocument = WordprocessingDocument.Open(memoryStream, false);

                var body = wordDocument.MainDocumentPart?.Document?.Body;
                if (body == null)
                {
                    return string.Empty;
                }

                var textBuilder = new StringBuilder();
                
                // Extract text from paragraphs
                var paragraphs = body.Elements<Paragraph>();
                foreach (var paragraph in paragraphs)
                {
                    var paragraphText = GetTextFromParagraph(paragraph);
                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        textBuilder.AppendLine(paragraphText);
                    }
                }

                // Extract text from tables
                var tables = body.Elements<Table>();
                foreach (var table in tables)
                {
                    textBuilder.AppendLine("--- Table ---");
                    
                    foreach (var row in table.Elements<TableRow>())
                    {
                        var rowTexts = new List<string>();
                        foreach (var cell in row.Elements<TableCell>())
                        {
                            var cellText = string.Join(" ", cell.Elements<Paragraph>()
                                .Select(p => GetTextFromParagraph(p))
                                .Where(t => !string.IsNullOrWhiteSpace(t)));
                            rowTexts.Add(cellText);
                        }
                        
                        if (rowTexts.Any(t => !string.IsNullOrWhiteSpace(t)))
                        {
                            textBuilder.AppendLine(string.Join(" | ", rowTexts));
                        }
                    }
                    
                    textBuilder.AppendLine();
                }

                var extractedText = textBuilder.ToString();
                _logger.LogInformation("Successfully extracted {Length} characters from DOCX document", extractedText.Length);
                
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from DOCX");
                throw new InvalidOperationException("Failed to extract text from DOCX document", ex);
            }
        }

        private async Task<string> ExtractTextFromDocAsync(byte[] fileData)
        {
            // For .DOC files (older Word format), we'll need a different approach
            // Since DocumentFormat.OpenXml doesn't support .DOC, we'll provide a fallback
            _logger.LogWarning("DOC format extraction not fully implemented. Consider converting to DOCX for better results.");
            
            try
            {
                // Basic text extraction attempt - this is a simplified approach
                // In a production environment, you might want to use a more robust library
                // or convert DOC to DOCX first using a conversion service
                
                var text = Encoding.UTF8.GetString(fileData);
                
                // Simple heuristic to extract readable text from DOC binary format
                var cleanText = new StringBuilder();
                bool inText = false;
                
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    
                    if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c))
                    {
                        cleanText.Append(c);
                        inText = true;
                    }
                    else if (inText && (c == '\0' || c == '\r' || c == '\n'))
                    {
                        cleanText.Append(' ');
                    }
                    else if (inText)
                    {
                        cleanText.Append(' ');
                        inText = false;
                    }
                }
                
                var extractedText = cleanText.ToString();
                
                // Clean up multiple spaces and normalize
                extractedText = System.Text.RegularExpressions.Regex.Replace(extractedText, @"\s+", " ");
                extractedText = extractedText.Trim();
                
                _logger.LogInformation("Extracted {Length} characters from DOC document (basic extraction)", extractedText.Length);
                
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from DOC");
                throw new InvalidOperationException("Failed to extract text from DOC document. Consider converting to DOCX format.", ex);
            }
        }

        private string GetTextFromParagraph(Paragraph paragraph)
        {
            var textBuilder = new StringBuilder();
            
            foreach (var run in paragraph.Elements<Run>())
            {
                foreach (var text in run.Elements<Text>())
                {
                    textBuilder.Append(text.Text);
                }
                
                foreach (var text in run.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>())
                {
                    textBuilder.Append(text.Text);
                }
            }
            
            return textBuilder.ToString();
        }
    }
} 