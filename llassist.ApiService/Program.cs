using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using llassist.Common;
using llassist.Common.Models;
using llassist.ApiService.Controllers;
using llassist.ApiService.Repositories;
using llassist.ApiService.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire components.
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddProblemDetails();

        // Register the InMemoryRepository.
        builder.Services.AddSingleton<ICRUDRepository<Ulid, Project>, InMemoryRepository<Project>>();

        // Register the Services
        builder.Services.AddScoped<ProjectService>();
        builder.Services.AddScoped<ArticleService>();
        builder.Services.AddSingleton<LLMService>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var openAIAPIKey = configuration["OpenAI:ApiKey"];
            return new LLMService(openAIAPIKey);
        });
        builder.Services.AddScoped<NLPService>();

        // Register the Controllers
        builder.Services.AddControllers();

        // Register Swagger services
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "LLAssist API", Version = "v1" });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();

        // Enable routing
        app.UseRouting();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Companion API V1");
            });
        }

        // Enable endpoints for controllers
        app.MapControllers();
        app.MapDefaultEndpoints();

        app.Run();
    }
}
