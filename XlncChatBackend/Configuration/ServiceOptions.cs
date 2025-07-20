namespace XlncChatBackend.Configuration;

// AI Service Configuration
public class AIServiceOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 1000;
    public float TopP { get; set; } = 1.0f;
    public int MaxSearchResults { get; set; } = 10;
    public double SimilarityThreshold { get; set; } = 0.7;
    public int MaxContextChunks { get; set; } = 5;
}

// Vector Database Configuration
public class VectorDatabaseOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6333;
    public bool UseHttps { get; set; } = false;
    public string? ApiKey { get; set; }
    public int VectorSize { get; set; } = 1536; // For text-embedding-3-small
    public string CollectionName { get; set; } = "knowledge_base";
}

// Document Processing Configuration
public class DocumentProcessingOptions
{
    public string UploadDirectory { get; set; } = "uploads";
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public List<string> AllowedExtensions { get; set; } = new() { "pdf", "doc", "docx" };
    public int ChunkSize { get; set; } = 1000;
    public int OverlapSize { get; set; } = 200;
    public bool EnableVirusScanning { get; set; } = true;
    public bool EnableParallelProcessing { get; set; } = true;
}

// Virus Scanning Configuration
public class VirusScanningOptions
{
    public string ClamAVHost { get; set; } = "localhost";
    public int ClamAVPort { get; set; } = 3310;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableScanning { get; set; } = true;
    public bool QuarantineInfectedFiles { get; set; } = true;
    public string QuarantineDirectory { get; set; } = "quarantine";
}

// Rate Limiting Configuration
public class RateLimitingOptions
{
    public bool EnableRateLimiting { get; set; } = true;
    public bool UseRedisStore { get; set; } = true;
    public string RedisConnectionString { get; set; } = "localhost:6379";
    public GlobalRateLimits GlobalLimits { get; set; } = new();
    public Dictionary<string, EndpointRateLimits> EndpointLimits { get; set; } = new();
}

public class GlobalRateLimits
{
    public int RequestsPerMinute { get; set; } = 100;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
}

public class EndpointRateLimits
{
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 500;
    public int RequestsPerDay { get; set; } = 5000;
}

// WhatsApp Configuration
public class WhatsAppOptions
{
    public string TwilioAccountSid { get; set; } = string.Empty;
    public string TwilioAuthToken { get; set; } = string.Empty;
    public string TwilioPhoneNumber { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public bool EnableWhatsApp { get; set; } = true;
    public int MessageRetryAttempts { get; set; } = 3;
    public int MessageTimeoutSeconds { get; set; } = 30;
}

// Email Service Configuration
public class EmailServiceOptions
{
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@xlnc.com";
    public string FromName { get; set; } = "XLNC Technologies";
    public bool EnableEmailService { get; set; } = true;
    public string TranscriptTemplate { get; set; } = "chat-transcript";
    public string MeetingInviteTemplate { get; set; } = "meeting-invite";
    public string WelcomeTemplate { get; set; } = "welcome";
}

// Meeting Service Configuration
public class MeetingServiceOptions
{
    public string CalendarProvider { get; set; } = "Graph"; // Graph, Google, Outlook
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string DefaultTimeZone { get; set; } = "UTC";
    public int DefaultMeetingDuration { get; set; } = 30; // minutes
    public List<string> AvailableTimeZones { get; set; } = new() { "UTC", "EST", "PST", "GMT" };
    public string MeetingRoomEmail { get; set; } = string.Empty;
}

// Cache Configuration
public class CacheOptions
{
    public string RedisConnectionString { get; set; } = "localhost:6379";
    public bool UseRedis { get; set; } = true;
    public int DefaultExpirationMinutes { get; set; } = 60;
    public string KeyPrefix { get; set; } = "xlnc_chat:";
    public bool EnableCompression { get; set; } = true;
}

// Background Jobs Configuration
public class BackgroundJobOptions
{
    public string HangfireConnectionString { get; set; } = string.Empty;
    public int WorkerCount { get; set; } = Environment.ProcessorCount;
    public bool EnableDashboard { get; set; } = true;
    public string DashboardPath { get; set; } = "/hangfire";
    public int RetryAttempts { get; set; } = 3;
    public int JobTimeoutMinutes { get; set; } = 30;
}

// Security Configuration
public class SecurityOptions
{
    public bool EnableApiKeyValidation { get; set; } = true;
    public bool RequireHttps { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new() { "*" };
    public int ApiKeyLength { get; set; } = 32;
    public string[] TrustedProxies { get; set; } = Array.Empty<string>();
    public bool EnableSecurityHeaders { get; set; } = true;
}

// Analytics Configuration
public class AnalyticsOptions
{
    public bool EnableAnalytics { get; set; } = true;
    public bool TrackUserInteractions { get; set; } = true;
    public bool TrackPerformanceMetrics { get; set; } = true;
    public int MetricsRetentionDays { get; set; } = 90;
    public string ApplicationInsightsKey { get; set; } = string.Empty;
    public bool EnableRealTimeMetrics { get; set; } = true;
}

// Notification Configuration
public class NotificationOptions
{
    public bool EnableNotifications { get; set; } = true;
    public List<NotificationChannel> Channels { get; set; } = new();
    public Dictionary<string, string> SlackWebhooks { get; set; } = new();
    public Dictionary<string, string> TeamsWebhooks { get; set; } = new();
    public string NotificationEmail { get; set; } = string.Empty;
}

public class NotificationChannel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Email, Slack, Teams, SMS
    public string Endpoint { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string> AlertTypes { get; set; } = new();
}

// File Storage Configuration
public class FileStorageOptions
{
    public string StorageType { get; set; } = "Local"; // Local, Azure, AWS
    public string LocalStoragePath { get; set; } = "storage";
    public string AzureConnectionString { get; set; } = string.Empty;
    public string AzureContainerName { get; set; } = "chat-files";
    public string AwsAccessKey { get; set; } = string.Empty;
    public string AwsSecretKey { get; set; } = string.Empty;
    public string AwsBucket { get; set; } = string.Empty;
    public string AwsRegion { get; set; } = "us-east-1";
}

// Logging Configuration
public class LoggingOptions
{
    public string LogLevel { get; set; } = "Information";
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnableFileLogging { get; set; } = true;
    public bool EnableSeqLogging { get; set; } = false;
    public string SeqUrl { get; set; } = string.Empty;
    public string SeqApiKey { get; set; } = string.Empty;
    public string LogFilePath { get; set; } = "logs/xlnc-chat-.txt";
    public int MaxLogFileSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MaxLogFiles { get; set; } = 10;
}

// Health Check Configuration
public class HealthCheckOptions
{
    public bool EnableHealthChecks { get; set; } = true;
    public string HealthCheckPath { get; set; } = "/health";
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableDetailedErrors { get; set; } = false;
    public Dictionary<string, HealthCheckEndpoint> Endpoints { get; set; } = new();
}

public class HealthCheckEndpoint
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;
    public bool Critical { get; set; } = true;
}

// Feature Flags Configuration
public class FeatureFlagsOptions
{
    public bool EnableDocumentUpload { get; set; } = true;
    public bool EnableWhatsAppIntegration { get; set; } = true;
    public bool EnableMeetingScheduling { get; set; } = true;
    public bool EnableAnalytics { get; set; } = true;
    public bool EnableVirusScanning { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public bool EnableCaching { get; set; } = true;
    public bool EnableBackgroundJobs { get; set; } = true;
    public bool EnableRealTimeFeatures { get; set; } = true;
    public bool EnableAdvancedSecurity { get; set; } = true;
}

// Environment Configuration
public class EnvironmentOptions
{
    public string Environment { get; set; } = "Development";
    public bool IsDevelopment => Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
    public bool IsProduction => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
    public bool IsStaging => Environment.Equals("Staging", StringComparison.OrdinalIgnoreCase);
    public string Version { get; set; } = "1.0.0";
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}

// Database Configuration
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Provider { get; set; } = "SqlServer"; // SqlServer, PostgreSQL, MySQL
    public bool EnableRetryOnFailure { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
}

// API Configuration
public class ApiOptions
{
    public string BaseUrl { get; set; } = "https://api.xlnc.com";
    public string Version { get; set; } = "v1";
    public bool EnableSwagger { get; set; } = true;
    public bool EnableVersioning { get; set; } = true;
    public int DefaultPageSize { get; set; } = 50;
    public int MaxPageSize { get; set; } = 1000;
    public string[] SupportedMediaTypes { get; set; } = { "application/json", "application/xml" };
}