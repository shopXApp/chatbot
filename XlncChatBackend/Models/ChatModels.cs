using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XlncChatBackend.Models;

// Core Chat Models
public class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ApiKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserPhone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Active;
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? Referrer { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Navigation properties
    public List<ChatMessage> Messages { get; set; } = new();
    public List<ChatAnalytics> Analytics { get; set; } = new();
    public List<Meeting> Meetings { get; set; } = new();
}

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageSender Sender { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public string? ResponseSource { get; set; } // "knowledge_base", "ai", "human"
    public double? Confidence { get; set; }
    public List<QuickAction>? Actions { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Navigation properties
    public ChatSession Session { get; set; } = null!;
}

public class QuickAction
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public ActionType Type { get; set; } = ActionType.Button;
    public Dictionary<string, object> Data { get; set; } = new();
}

// Knowledge Base Models
public class KnowledgeDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DocumentStatus Status { get; set; } = DocumentStatus.Processing;
    public string? ProcessingError { get; set; }
    public List<string> Categories { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Processing info
    public DateTime? ProcessedAt { get; set; }
    public int ChunkCount { get; set; }
    public bool IsVirusScanned { get; set; }
    public string? VirusScanResult { get; set; }
    
    // Navigation properties
    public List<DocumentChunk> Chunks { get; set; } = new();
}

public class DocumentChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public string? Category { get; set; }
    public List<string> Keywords { get; set; } = new();
    public float[]? Embedding { get; set; }
    public string? VectorId { get; set; } // Qdrant vector ID
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Navigation properties
    public KnowledgeDocument Document { get; set; } = null!;
}

public class KnowledgeBaseQuery
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<string> MatchedChunks { get; set; } = new();
    public double? BestMatchScore { get; set; }
    public string? GeneratedResponse { get; set; }
    public bool WasHelpful { get; set; }
    public string? UserFeedback { get; set; }
}

// Meeting and Calendar Models
public class Meeting
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string AttendeeEmail { get; set; } = string.Empty;
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeePhone { get; set; } = string.Empty;
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
    public string? MeetingLink { get; set; }
    public string? CalendarEventId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Navigation properties
    public ChatSession Session { get; set; } = null!;
}

// Analytics Models
public class ChatAnalytics
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // "session_start", "message_sent", "meeting_scheduled", etc.
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
    
    // Navigation properties
    public ChatSession Session { get; set; } = null!;
}

public class UserSatisfactionSurvey
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public int OverallRating { get; set; } // 1-5 scale
    public int ResponseTime { get; set; } // 1-5 scale
    public int HelpfulnessRating { get; set; } // 1-5 scale
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public List<string> ImprovementSuggestions { get; set; } = new();
}

public class SystemMetrics
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string MetricType { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

// WhatsApp Integration Models
public class WhatsAppMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public WhatsAppMessageType Type { get; set; } = WhatsAppMessageType.Text;
    public WhatsAppMessageDirection Direction { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public WhatsAppMessageStatus Status { get; set; } = WhatsAppMessageStatus.Pending;
    public string? ExternalMessageId { get; set; }
    public string? ErrorMessage { get; set; }
}

// Security and Monitoring Models
public class SecurityAlert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public bool IsResolved { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public class RateLimitViolation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string IpAddress { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int RequestCount { get; set; }
    public int LimitExceeded { get; set; }
    public string? UserAgent { get; set; }
}

// Background Job Models
public class ProcessingJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public JobType Type { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? DocumentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> Result { get; set; } = new();
}

// Configuration Models
public class ApiConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public RateLimitSettings RateLimits { get; set; } = new();
    public ChatSettings ChatSettings { get; set; } = new();
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

public class RateLimitSettings
{
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
    public bool EnableRateLimiting { get; set; } = true;
}

public class ChatSettings
{
    public string DefaultGreeting { get; set; } = string.Empty;
    public string Theme { get; set; } = "green";
    public string Position { get; set; } = "bottom-right";
    public bool EnableWhatsApp { get; set; } = true;
    public bool EnableMeetingScheduling { get; set; } = true;
    public bool EnableFileUploads { get; set; } = true;
    public List<string> AllowedFileTypes { get; set; } = new() { "pdf", "doc", "docx" };
    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
}

// Enums
public enum ChatSessionStatus
{
    Active,
    Ended,
    Transferred,
    Abandoned
}

public enum MessageSender
{
    User,
    Bot,
    Agent
}

public enum MessageType
{
    Text,
    Image,
    File,
    QuickReply,
    System
}

public enum MessageStatus
{
    Sent,
    Delivered,
    Read,
    Failed
}

public enum ActionType
{
    Button,
    Link,
    Phone,
    Email,
    Schedule
}

public enum DocumentStatus
{
    Processing,
    Completed,
    Failed,
    VirusDetected
}

public enum MeetingStatus
{
    Scheduled,
    Confirmed,
    Cancelled,
    Completed,
    NoShow
}

public enum WhatsAppMessageType
{
    Text,
    Image,
    Document,
    Audio,
    Video
}

public enum WhatsAppMessageDirection
{
    Inbound,
    Outbound
}

public enum WhatsAppMessageStatus
{
    Pending,
    Sent,
    Delivered,
    Read,
    Failed
}

public enum AlertType
{
    VirusDetected,
    RateLimitExceeded,
    ProcessingFailure,
    SecurityThreat,
    SystemError,
    PerformanceIssue
}

public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum JobType
{
    DocumentProcessing,
    VectorEmbedding,
    VirusScanning,
    EmailTranscript,
    WhatsAppMessage,
    MeetingScheduling
}

public enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}

// Request/Response DTOs
public class ChatMessageRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public Dictionary<string, object>? Context { get; set; }
}

public class ChatMessageResponse
{
    public string Response { get; set; } = string.Empty;
    public List<QuickAction>? Actions { get; set; }
    public string Source { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
}

public class AuthenticationRequest
{
    [Required]
    public UserInfo UserInfo { get; set; } = new();
    
    public string? UserAgent { get; set; }
    public string? Referrer { get; set; }
}

public class UserInfo
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Phone { get; set; } = string.Empty;
}

public class AuthenticationResponse
{
    public string SessionId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class DocumentUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public List<string> Categories { get; set; } = new();
    public string? Title { get; set; }
}

public class DocumentUploadResponse
{
    public string DocumentId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? JobId { get; set; }
}

public class MeetingScheduleRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;
    
    [Required]
    public DateTime PreferredDateTime { get; set; }
    
    public string? Purpose { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string? TimeZone { get; set; }
}

public class MeetingScheduleResponse
{
    public string MeetingId { get; set; } = string.Empty;
    public string? MeetingLink { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class AnalyticsResponse
{
    public Dictionary<string, object> Metrics { get; set; } = new();
    public List<ChartData> Charts { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class ChartData
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<DataPoint> Data { get; set; } = new();
}

public class DataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime? Timestamp { get; set; }
}