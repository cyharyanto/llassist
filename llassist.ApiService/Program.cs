using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using llassist.Common;
using llassist.Common.Models;
using llassist.ApiService.Controllers;
using llassist.ApiService.Repositories;
using llassist.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using llassist.ApiService.Repositories.Specifications;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using llassist.ApiService.Executors;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using llassist.Common.Models.Responses;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire components.
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddProblemDetails();

        // Register the persistence repository.
        var dbConnectionString = builder.Configuration.GetConnectionString("LlassistAppDatabase");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(dbConnectionString);
        });
        builder.Services.AddScoped<ICRUDRepository<Ulid, Project, BaseSearchSpec>, ProjectRepository>();
        builder.Services.AddScoped<ICRUDRepository<Ulid, Article, ArticleSearchSpec>, ArticleRepository>();
        builder.Services.AddScoped<ICRUDRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec>, EstimateRelevanceJobRepository>();

        // Register background task processing
        ConfigureQueue(builder.Services, builder.Configuration);
        builder.Services.AddScoped<IArticleRelevanceService, ArticleRelevanceService>();
        builder.Services.AddScoped<IEstimateRelevanceService, EstimateRelevanceService>();
        builder.Services.AddSingleton<BackgroundTaskExecutor>();

        // Register the Services
        builder.Services.AddScoped<ProjectService>();
        builder.Services.AddScoped<ArticleService>();
        builder.Services.AddScoped<LLMService>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var dbContext = provider.GetRequiredService<ApplicationDbContext>();
            
            // Try to get API key from database first
            var apiKeyConfig = dbContext.AppSettings
                .FirstOrDefault(c => c.Key == AppSettingKeys.OpenAIApiKey);
            
            var openAIAPIKey = apiKeyConfig?.Value ?? 
                configuration["OpenAI:ApiKey"] ??
                Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrEmpty(openAIAPIKey))
            {
                throw new InvalidOperationException("OpenAI API key not found in database, configuration or environment variables");
            }

            return new LLMService(openAIAPIKey);
        });
        builder.Services.AddScoped<INLPService, NLPService>();
        builder.Services.AddScoped<IAppSettingService, AppSettingService>();

        // Register the Controllers
        builder.Services.AddControllers();

        // Register Swagger services
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "LLAssist API", Version = "v1" });
        });

        var app = builder.Build();

        // Immediately initiate the queue consumer
        var backgroundTaskExecutor = app.Services.GetRequiredService<BackgroundTaskExecutor>();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                
                var exception = context.Features.Get<IExceptionHandlerFeature>();
                if (exception != null)
                {
                    var error = new ApiErrorResponse
                    {
                        HttpStatusCode = context.Response.StatusCode,
                        Message = "An unexpected error occurred",
                        Details = exception.Error.Message,
                        Timestamp = DateTime.UtcNow
                    };
                    await context.Response.WriteAsJsonAsync(error);
                }
            });
        });

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

    private static void ConfigureQueue(IServiceCollection services, IConfiguration configuration)
    {
        // Register queue options
        var queueConfiguration = new QueueOptions();
        configuration.GetSection(QueueOptions.SectionName).Bind(queueConfiguration);
        services.Configure<QueueOptions>(configuration.GetSection(QueueOptions.SectionName));

        var queueName = queueConfiguration.QueueName;
        var dbConnectionString = configuration.GetConnectionString("LlassistAppDatabase");
        var queueConnection = new QueueConnection(queueName, dbConnectionString);

        // Register queue container, create the queue if it doesn't exist
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Program>>();

            using (var createQueueContainer = new QueueCreationContainer<PostgreSqlMessageQueueInit>())
            {
                using var createQueue = createQueueContainer.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                if (!createQueue.QueueExists)
                {
                    logger.LogInformation("Queue {QueueName} does not exist. Creating queue...", queueName);

                    createQueue.Options.EnableDelayedProcessing = queueConfiguration.EnableDelayedProcessing;
                    createQueue.Options.EnableHeartBeat = queueConfiguration.EnableHeartBeat;
                    createQueue.Options.EnableMessageExpiration = queueConfiguration.EnableMessageExpiration;
                    createQueue.Options.EnableStatus = queueConfiguration.EnableStatus;

                    logger.LogInformation("Queue options set: EnableDelayedProcessing={EnableDelayedProcessing}, EnableHeartBeat={EnableHeartBeat}, EnableMessageExpiration={EnableMessageExpiration}, EnableStatus={EnableStatus}",
                        createQueue.Options.EnableDelayedProcessing,
                        createQueue.Options.EnableHeartBeat,
                        createQueue.Options.EnableMessageExpiration,
                        createQueue.Options.EnableStatus);

                    createQueue.CreateQueue();
                    logger.LogInformation("Queue {QueueName} created successfully", queueName);
                }
            }

            var queueContainer = new QueueContainer<PostgreSqlMessageQueueInit>();
            return queueContainer;
        });

        // Register queue producer
        services.AddSingleton(sp =>
        {
            var queueContainer = sp.GetRequiredService<QueueContainer<PostgreSqlMessageQueueInit>>();
            return queueContainer.CreateProducer<BackgroundTask>(queueConnection);
        });
        services.AddScoped<ProjectProcessingService>();

        // Register queue consumer
        services.AddSingleton(sp =>
        {
            var queueContainer = sp.GetRequiredService<QueueContainer<PostgreSqlMessageQueueInit>>();
            var consumerQueue = queueContainer.CreateConsumer(queueConnection);
            return consumerQueue;
        });
    }
}
