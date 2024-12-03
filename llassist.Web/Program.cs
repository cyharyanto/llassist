using llassist.Web;
using llassist.Web.Components;
using Microsoft.AspNetCore.Http.Features;

internal partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire components.
        builder.AddServiceDefaults();
        builder.AddRedisOutputCache("cache");

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Register HttpClient for API services
        builder.Services.AddHttpClient<ProjectApiClient>(client => client.BaseAddress = new Uri("http+https://apiservice"));
        builder.Services.AddHttpClient<AppSettingApiClient>(client => client.BaseAddress = new Uri("http+https://apiservice"));
        builder.Services.AddHttpClient<ArticleApiClient>(client => client.BaseAddress = new Uri("http+https://apiservice"));
        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.UseOutputCache();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapDefaultEndpoints();

        app.Run();
    }
}
