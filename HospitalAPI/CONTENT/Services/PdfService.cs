using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using System.Text;

namespace HospitalAPI.Services
{
   
    public interface IPdfService
    {
        
        Task<string> ExtractTextFromPdfAsync(byte[] pdfBytes);

        bool IsValidPdf(byte[] fileBytes);
    }

  
    public class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;

        public PdfService(ILogger<PdfService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ExtractTextFromPdfAsync(byte[] pdfBytes)
        {
            try
            {
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    throw new ArgumentException("PDF bytes cannot be null or empty");
                }

                if (!IsValidPdf(pdfBytes))
                {
                    throw new InvalidOperationException("Invalid PDF format");
                }

                return await Task.Run(() =>
                {
                    using var memoryStream = new MemoryStream(pdfBytes);
                    using var pdfReader = new PdfReader(memoryStream);
                    using var pdfDocument = new PdfDocument(pdfReader);

                    var textBuilder = new StringBuilder();

                    for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                    {
                        var page = pdfDocument.GetPage(i);
                        var strategy = new LocationTextExtractionStrategy();
                        var currentText = PdfTextExtractor.GetTextFromPage(page, strategy);

                        textBuilder.AppendLine(currentText);
                    }

                    return textBuilder.ToString().Trim();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                throw new PdfProcessingException("Failed to extract text from PDF", ex);
            }
        }

        public bool IsValidPdf(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length < 4)
                return false;

            return fileBytes[0] == 0x25 && // %
                   fileBytes[1] == 0x50 && // P
                   fileBytes[2] == 0x44 && // D
                   fileBytes[3] == 0x46;   // F
        }
    }


    public class PdfProcessingException : Exception
    {
        public PdfProcessingException(string message) : base(message)
        {
        }

        public PdfProcessingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
