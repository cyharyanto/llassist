using llassist.Common.Models;

namespace llassist.ApiService.Services.Document;

public interface IDocumentProcessingService
{
    Task<DocumentProcessingJob> CreateProcessingJobAsync(Stream documentStream, string fileName, string contentType);
    Task ExecuteTaskAsync(DocumentProcessingTask task);
}
