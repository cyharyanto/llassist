using System.Diagnostics;

namespace llassist.ApiService.Services;

public static class AIInstrumentation
{
    public const string ActivitySourceName = "llassist.ai";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
