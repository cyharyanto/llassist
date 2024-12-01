namespace llassist.Common.Models.Responses;

public class ApiErrorResponse
{
    public int HttpStatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
