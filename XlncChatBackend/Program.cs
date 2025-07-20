using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;
using XlncChatBackend.Configuration;
using XlncChatBackend.Data;
using XlncChatBackend.Services;
using XlncChatBackend.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/xlnc-chat-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
var services = builder.Services;
var configuration = builder.Configuration;

// Configuration Options
services.Configure<AIServiceOptions>(configuration.GetSection("AIService"));
services.Configure<VectorDatabaseOptions>(configuration.GetSection("VectorDatabase"));
services.Configure<DocumentProcessingOptions>(configuration.GetSection("DocumentProcessing"));
services.Configure<VirusScanningOptions>(configuration.GetSection("VirusScanning"));
services.Configure<RateLimitingOptions>(configuration.GetSection("RateLimit"));
services.Configure<WhatsAppOptions>(configuration.GetSection("WhatsApp"));
services.Configure<EmailServiceOptions>(configuration.GetSection("EmailService"));
services.Configure<MeetingServiceOptions>(configuration.GetSection("MeetingService"));
services.Configure<CacheOptions>(configuration.GetSection("Cache"));
services.Configure<BackgroundJobOptions>(configuration.GetSection("BackgroundJobs"));
services.Configure<SecurityOptions>(configuration.GetSection("Security"));
services.Configure<AnalyticsOptions>(configuration.GetSection("Analytics"));
services.Configure<NotificationOptions>(configuration.GetSection("Notifications"));
services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
services.Configure<LoggingOptions>(configuration.GetSection("Logging"));
services.Configure<HealthCheckOptions>(configuration.GetSection("HealthChecks"));
services.Configure<FeatureFlagsOptions>(configuration.GetSection("FeatureFlags"));
services.Configure<EnvironmentOptions>(configuration.GetSection("Environment"));
services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
services.Configure<ApiOptions>(configuration.GetSection("Api"));

// Database Configuration
services.AddDbContext<ChatDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
        sqlOptions.CommandTimeout(30);
    });
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Redis Cache Configuration
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
});

// Rate Limiting Configuration
services.AddRateLimiter(options =>
{
    // Global rate limiting
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Authentication endpoint limiting
    options.AddFixedWindowLimiter("AuthPolicy", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 5;
    });

    // Message endpoint limiting
    options.AddFixedWindowLimiter("MessagePolicy", options =>
    {
        options.PermitLimit = 60;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });

    // File upload limiting
    options.AddFixedWindowLimiter("UploadPolicy", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
    };
});

// CORS Configuration
services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    options.AddPolicy("Strict", policy =>
    {
        policy.WithOrigins(configuration.GetSection("Security:AllowedOrigins").Get<string[]>() ?? new[] { "https://localhost" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Authentication & Authorization
services.AddAuthentication()
    .AddJwtBearer("ApiKey", options =>
    {
        // Custom JWT configuration for API key validation
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Extract API key from header
                if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
                {
                    context.Token = apiKey;
                }
                return Task.CompletedTask;
            }
        };
    });

// Background Jobs with Hangfire
services.AddHangfire(configuration =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"));
});

services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount;
});

// Health Checks
services.AddHealthChecks()
    .AddDbContext<ChatDbContext>()
    .AddRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379")
    .AddCheck<VectorDatabaseHealthCheck>("qdrant")
    .AddCheck<AIServiceHealthCheck>("openai")
    .AddCheck<ClamAVHealthCheck>("clamav");

// Register Application Services
services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
services.AddScoped<IVectorDatabaseService, VectorDatabaseService>();
services.AddScoped<IAIService, AIService>();
services.AddScoped<IVirusScanningService, VirusScanningService>();
services.AddScoped<IChatService, ChatService>();
services.AddScoped<IWhatsAppService, WhatsAppService>();
services.AddScoped<IMeetingService, MeetingService>();
services.AddScoped<IEmailService, EmailService>();
services.AddScoped<IAnalyticsService, AnalyticsService>();
services.AddScoped<IRateLimitingService, RateLimitingService>();
services.AddScoped<ISecurityService, SecurityService>();
services.AddScoped<IBackgroundJobService, BackgroundJobService>();
services.AddScoped<ICacheService, CacheService>();
services.AddScoped<INotificationService, NotificationService>();
services.AddScoped<IFileStorageService, FileStorageService>();
services.AddScoped<IConfigurationService, ConfigurationService>();
services.AddScoped<IHealthCheckService, HealthCheckService>();

// HTTP Client Factory
services.AddHttpClient();

// AutoMapper
services.AddAutoMapper(typeof(Program));

// Controllers and API
services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// API Documentation
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "XLNC Chat API",
        Description = "Comprehensive chat widget backend with AI, RAG, and analytics",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "XLNC Technologies",
            Email = "support@xlnc.com",
            Url = new Uri("https://xlnc.com")
        }
    });

    // Add API Key authentication
    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key needed to access the endpoints"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "XLNC Chat API v1");
        options.RoutePrefix = "api-docs";
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:;");
    
    await next();
});

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Health Checks
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                duration = x.Value.Duration.TotalMilliseconds,
                description = x.Value.Description,
                data = x.Value.Data
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Hangfire Dashboard
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

// Map Controllers
app.MapControllers();

// Static Files (for file downloads)
app.UseStaticFiles();

// Custom Error Handling
app.Map("/error", errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            error = "An error occurred processing your request",
            timestamp = DateTime.UtcNow,
            traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
        }));
    });
});

// Database Migration and Seeding
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        
        if (app.Environment.IsDevelopment())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }

        // Seed default data
        await SeedDefaultDataAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
    }
}

// Background Job Initialization
RecurringJob.AddOrUpdate<IChatService>("cleanup-sessions", 
    service => service.CleanupInactiveSessionsAsync(24), 
    Cron.Hourly);

app.Logger.LogInformation("XLNC Chat Backend started successfully");

app.Run();

// Helper Methods
static async Task SeedDefaultDataAsync(IServiceProvider serviceProvider)
{
    var context = serviceProvider.GetRequiredService<ChatDbContext>();
    
    // Seed default API configuration if none exists
    if (!await context.ApiConfigurations.AnyAsync())
    {
        var defaultConfig = new XlncChatBackend.Models.ApiConfiguration
        {
            ApiKey = "demo_api_key_12345",
            CompanyName = "XLNC Technologies Demo",
            ContactEmail = "demo@xlnc.com",
            IsActive = true,
            RateLimits = new XlncChatBackend.Models.RateLimitSettings
            {
                RequestsPerMinute = 60,
                RequestsPerHour = 1000,
                RequestsPerDay = 10000
            },
            ChatSettings = new XlncChatBackend.Models.ChatSettings
            {
                DefaultGreeting = "Hello! I'm here to help you with any questions about XLNC Technologies. How can I assist you today?",
                Theme = "green",
                Position = "bottom-right",
                EnableWhatsApp = true,
                EnableMeetingScheduling = true,
                EnableFileUploads = true
            }
        };

        context.ApiConfigurations.Add(defaultConfig);
        await context.SaveChangesAsync();
    }
}

// Custom Authorization Filter for Hangfire
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, implement proper authorization
        return true; // For development only
    }
}

// Custom Health Checks
public class VectorDatabaseHealthCheck : IHealthCheck
{
    private readonly IVectorDatabaseService _vectorDb;

    public VectorDatabaseHealthCheck(IVectorDatabaseService vectorDb)
    {
        _vectorDb = vectorDb;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var size = await _vectorDb.GetCollectionSizeAsync();
            return HealthCheckResult.Healthy($"Vector database is healthy. Collection size: {size}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Vector database is not responding", ex);
        }
    }
}

public class AIServiceHealthCheck : IHealthCheck
{
    private readonly IAIService _aiService;

    public AIServiceHealthCheck(IAIService aiService)
    {
        _aiService = aiService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test embedding generation with a simple text
            var embedding = await _aiService.GenerateEmbeddingAsync("health check");
            return HealthCheckResult.Healthy($"AI service is healthy. Embedding dimension: {embedding.Length}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("AI service is not responding", ex);
        }
    }
}

public class ClamAVHealthCheck : IHealthCheck
{
    private readonly IVirusScanningService _virusScanner;

    public ClamAVHealthCheck(IVirusScanningService virusScanner)
    {
        _virusScanner = virusScanner;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isAvailable = await _virusScanner.IsServiceAvailableAsync();
            return isAvailable 
                ? HealthCheckResult.Healthy("ClamAV service is available")
                : HealthCheckResult.Degraded("ClamAV service is not available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ClamAV health check failed", ex);
        }
    }
}