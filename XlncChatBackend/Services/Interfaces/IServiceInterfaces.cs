using XlncChatBackend.Models;

namespace XlncChatBackend.Services.Interfaces;

// Document Processing Service Interface
public interface IDocumentProcessingService
{
    Task<DocumentUploadResponse> ProcessDocumentAsync(IFormFile file, string apiKey, List<string> categories, string? title = null);
    Task<List<KnowledgeDocument>> GetDocumentsAsync(string apiKey, int skip = 0, int take = 50);
    Task<bool> DeleteDocumentAsync(string documentId, string apiKey);
}

// Vector Database Service Interface
public interface IVectorDatabaseService : IDisposable
{
    Task<string> StoreVectorAsync(float[] embedding, string content, Dictionary<string, object> metadata);
    Task<List<VectorSearchResult>> SearchSimilarAsync(float[] queryEmbedding, int limit = 10, double threshold = 0.7, Dictionary<string, object>? filters = null);
    Task<bool> DeleteVectorAsync(string vectorId);
    Task<bool> DeleteVectorsByFilterAsync(Dictionary<string, object> filters);
    Task<VectorSearchResult?> GetVectorAsync(string vectorId);
    Task<long> GetCollectionSizeAsync();
    Task<bool> UpdateVectorMetadataAsync(string vectorId, Dictionary<string, object> metadata);
    Task<List<string>> GetVectorsByDocumentIdAsync(string documentId);
}

// AI Service Interface
public interface IAIService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<ChatMessageResponse> GenerateResponseAsync(string query, string sessionId, string apiKey, Dictionary<string, object>? context = null);
    Task<List<string>> GenerateKeywordsAsync(string content);
    Task<string> SummarizeContentAsync(string content, int maxLength = 500);
    Task<string> CategorizeContentAsync(string content, List<string> availableCategories);
}

// Virus Scanning Service Interface
public interface IVirusScanningService
{
    Task<VirusScanResult> ScanFileAsync(string filePath);
    Task<VirusScanResult> ScanStreamAsync(Stream stream);
    Task<bool> IsServiceAvailableAsync();
}

// Chat Service Interface
public interface IChatService
{
    Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, string apiKey, string ipAddress, string? userAgent);
    Task<ChatMessageResponse> ProcessMessageAsync(ChatMessageRequest request, string apiKey, string ipAddress);
    Task<ChatSession?> GetSessionAsync(string sessionId);
    Task<List<ChatMessage>> GetChatHistoryAsync(string sessionId, int skip = 0, int take = 50);
    Task<bool> EndSessionAsync(string sessionId);
    Task UpdateSessionActivityAsync(string sessionId);
}

// WhatsApp Service Interface
public interface IWhatsAppService
{
    Task<bool> SendMessageAsync(string phoneNumber, string message, string sessionId);
    Task<bool> SendTemplateMessageAsync(string phoneNumber, string templateName, Dictionary<string, string> parameters, string sessionId);
    Task<WhatsAppMessage?> ProcessIncomingMessageAsync(object webhookPayload);
    Task<bool> IsPhoneNumberValidAsync(string phoneNumber);
}

// Meeting Service Interface
public interface IMeetingService
{
    Task<MeetingScheduleResponse> ScheduleMeetingAsync(MeetingScheduleRequest request, string apiKey);
    Task<List<Meeting>> GetMeetingsAsync(string sessionId);
    Task<bool> CancelMeetingAsync(string meetingId, string apiKey);
    Task<bool> UpdateMeetingStatusAsync(string meetingId, MeetingStatus status);
    Task<List<AvailableTimeSlot>> GetAvailableSlotsAsync(DateTime date, string timeZone);
}

// Email Service Interface
public interface IEmailService
{
    Task<bool> SendChatTranscriptAsync(string email, List<ChatMessage> messages, string sessionId);
    Task<bool> SendMeetingInviteAsync(Meeting meeting);
    Task<bool> SendWelcomeEmailAsync(string email, string name, string companyName);
    Task<bool> SendSupportNotificationAsync(string issue, string details, string sessionId);
}

// Analytics Service Interface
public interface IAnalyticsService
{
    Task TrackEventAsync(string sessionId, string eventType, Dictionary<string, object> properties);
    Task<AnalyticsResponse> GetAnalyticsAsync(string apiKey, DateTime startDate, DateTime endDate);
    Task<Dictionary<string, object>> GetDashboardMetricsAsync(string apiKey);
    Task<List<SystemMetrics>> GetSystemMetricsAsync(DateTime startDate, DateTime endDate);
    Task RecordUserSatisfactionAsync(UserSatisfactionSurvey survey);
}

// Rate Limiting Service Interface
public interface IRateLimitingService
{
    Task<RateLimitResult> CheckRateLimitAsync(string identifier, string endpoint, RateLimitSettings settings);
    Task RecordViolationAsync(string ipAddress, string? apiKey, string endpoint, int requestCount, int limit);
    Task<List<RateLimitViolation>> GetViolationsAsync(DateTime startDate, DateTime endDate);
}

// Security Service Interface
public interface ISecurityService
{
    Task CreateAlertAsync(AlertType type, AlertSeverity severity, string title, string description, Dictionary<string, object> details);
    Task<List<SecurityAlert>> GetActiveAlertsAsync();
    Task<bool> ResolveAlertAsync(string alertId);
    Task<bool> ValidateApiKeyAsync(string apiKey);
    Task<ApiConfiguration?> GetApiConfigurationAsync(string apiKey);
}

// Background Job Service Interface
public interface IBackgroundJobService
{
    Task<string> EnqueueDocumentProcessingAsync(string documentId);
    Task<string> EnqueueEmailTranscriptAsync(string sessionId, string email);
    Task<string> EnqueueWhatsAppMessageAsync(string phoneNumber, string message, string sessionId);
    Task<string> EnqueueVirusScanAsync(string filePath, string documentId);
    Task<ProcessingJob?> GetJobStatusAsync(string jobId);
    Task<List<ProcessingJob>> GetJobsAsync(JobType? type = null, JobStatus? status = null, int skip = 0, int take = 50);
}

// Cache Service Interface
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task RemovePatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);
}

// Notification Service Interface
public interface INotificationService
{
    Task SendAlertNotificationAsync(SecurityAlert alert);
    Task SendSystemNotificationAsync(string title, string message, NotificationLevel level);
    Task SendUserNotificationAsync(string sessionId, string message, NotificationType type);
}

// File Storage Service Interface
public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string directory);
    Task<bool> DeleteFileAsync(string filePath);
    Task<Stream> GetFileStreamAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
    Task<long> GetFileSizeAsync(string filePath);
}

// Configuration Service Interface
public interface IConfigurationService
{
    Task<ApiConfiguration?> GetConfigurationAsync(string apiKey);
    Task<bool> UpdateConfigurationAsync(string apiKey, ApiConfiguration configuration);
    Task<bool> CreateConfigurationAsync(ApiConfiguration configuration);
    Task<List<ApiConfiguration>> GetAllConfigurationsAsync();
}

// Health Check Service Interface
public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckDatabaseHealthAsync();
    Task<HealthCheckResult> CheckVectorDatabaseHealthAsync();
    Task<HealthCheckResult> CheckAIServiceHealthAsync();
    Task<HealthCheckResult> CheckCacheHealthAsync();
    Task<HealthCheckResult> CheckOverallHealthAsync();
}

// Supporting Classes and Enums
public class VirusScanResult
{
    public bool IsClean { get; set; }
    public string? ThreatName { get; set; }
    public string? Details { get; set; }
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;
}

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public TimeSpan RetryAfter { get; set; }
    public string? Message { get; set; }
}

public class AvailableTimeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string? Reason { get; set; }
}

public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string? Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public enum NotificationLevel
{
    Info,
    Warning,
    Error,
    Critical
}

public enum NotificationType
{
    Info,
    Warning,
    Success,
    Error
}