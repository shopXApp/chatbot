using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using XlncChatBackend.Models;

namespace XlncChatBackend.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    // Core Chat Tables
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatAnalytics> ChatAnalytics { get; set; }

    // Knowledge Base Tables
    public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }
    public DbSet<DocumentChunk> DocumentChunks { get; set; }
    public DbSet<KnowledgeBaseQuery> KnowledgeBaseQueries { get; set; }

    // Meeting and Calendar Tables
    public DbSet<Meeting> Meetings { get; set; }

    // Analytics Tables
    public DbSet<UserSatisfactionSurvey> UserSatisfactionSurveys { get; set; }
    public DbSet<SystemMetrics> SystemMetrics { get; set; }

    // WhatsApp Integration Tables
    public DbSet<WhatsAppMessage> WhatsAppMessages { get; set; }

    // Security and Monitoring Tables
    public DbSet<SecurityAlert> SecurityAlerts { get; set; }
    public DbSet<RateLimitViolation> RateLimitViolations { get; set; }

    // Background Job Tables
    public DbSet<ProcessingJob> ProcessingJobs { get; set; }

    // Configuration Tables
    public DbSet<ApiConfiguration> ApiConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ChatSession
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.ApiKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(100);
            entity.Property(e => e.UserEmail).HasMaxLength(255);
            entity.Property(e => e.UserPhone).HasMaxLength(20);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Referrer).HasMaxLength(500);
            
            // JSON column for metadata
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            // Indexes
            entity.HasIndex(e => e.ApiKey);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
        });

        // Configure ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ResponseSource).HasMaxLength(50);
            
            // JSON columns
            entity.Property(e => e.Actions)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => v == null ? null : JsonSerializer.Deserialize<List<QuickAction>>(v, (JsonSerializerOptions?)null));

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            // Relationships
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Sender);
        });

        // Configure KnowledgeDocument
        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.ApiKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ProcessingError).HasMaxLength(1000);
            entity.Property(e => e.VirusScanResult).HasMaxLength(100);

            // JSON columns
            entity.Property(e => e.Categories)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            // Indexes
            entity.HasIndex(e => e.ApiKey);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UploadedAt);
        });

        // Configure DocumentChunk
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.DocumentId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.VectorId).HasMaxLength(100);

            // JSON columns
            entity.Property(e => e.Keywords)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

            entity.Property(e => e.Embedding)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => v == null ? null : JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions?)null));

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            // Relationships
            entity.HasOne(e => e.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.VectorId);
        });

        // Configure KnowledgeBaseQuery
        modelBuilder.Entity<KnowledgeBaseQuery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Query).IsRequired();
            entity.Property(e => e.GeneratedResponse);
            entity.Property(e => e.UserFeedback).HasMaxLength(1000);

            // JSON column
            entity.Property(e => e.MatchedChunks)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

            // Indexes
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure Meeting
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.AttendeeEmail).HasMaxLength(255).IsRequired();
            entity.Property(e => e.AttendeeName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AttendeePhone).HasMaxLength(20);
            entity.Property(e => e.MeetingLink).HasMaxLength(500);
            entity.Property(e => e.CalendarEventId).HasMaxLength(100);

            // JSON column
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            // Relationships
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Meetings)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.Status);
        });

        // Configure ChatAnalytics
        modelBuilder.Entity<ChatAnalytics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();

            // JSON column
            entity.Property(e => e.Properties)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            // Relationships
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Analytics)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure UserSatisfactionSurvey
        modelBuilder.Entity<UserSatisfactionSurvey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Feedback).HasMaxLength(2000);

            // JSON column
            entity.Property(e => e.ImprovementSuggestions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

            // Indexes
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.SubmittedAt);
        });

        // Configure SystemMetrics
        modelBuilder.Entity<SystemMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.MetricType).HasMaxLength(100).IsRequired();

            // JSON column
            entity.Property(e => e.Properties)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            // Indexes
            entity.HasIndex(e => e.MetricType);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure WhatsAppMessage
        modelBuilder.Entity<WhatsAppMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ExternalMessageId).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

            // Indexes
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.PhoneNumber);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure SecurityAlert
        modelBuilder.Entity<SecurityAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.SessionId).HasMaxLength(50);

            // JSON column
            entity.Property(e => e.Details)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            // Indexes
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.IsResolved);
        });

        // Configure RateLimitViolation
        modelBuilder.Entity<RateLimitViolation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45).IsRequired();
            entity.Property(e => e.ApiKey).HasMaxLength(100);
            entity.Property(e => e.Endpoint).HasMaxLength(255).IsRequired();
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            // Indexes
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => e.ApiKey);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure ProcessingJob
        modelBuilder.Entity<ProcessingJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.DocumentId).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            // JSON columns
            entity.Property(e => e.Parameters)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            entity.Property(e => e.Result)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            // Indexes
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure ApiConfiguration
        modelBuilder.Entity<ApiConfiguration>(entity =>
        {
            entity.HasKey(e => e.ApiKey);
            entity.Property(e => e.ApiKey).HasMaxLength(100);
            entity.Property(e => e.CompanyName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContactEmail).HasMaxLength(255).IsRequired();

            // JSON columns
            entity.Property(e => e.RateLimits)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<RateLimitSettings>(v, (JsonSerializerOptions?)null) ?? new());

            entity.Property(e => e.ChatSettings)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<ChatSettings>(v, (JsonSerializerOptions?)null) ?? new());

            entity.Property(e => e.CustomSettings)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            // Indexes
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Add table constraints and additional configurations
        ConfigureTableConstraints(modelBuilder);
    }

    private static void ConfigureTableConstraints(ModelBuilder modelBuilder)
    {
        // Add check constraints for ratings
        modelBuilder.Entity<UserSatisfactionSurvey>()
            .HasCheckConstraint("CK_OverallRating", "[OverallRating] >= 1 AND [OverallRating] <= 5");
        
        modelBuilder.Entity<UserSatisfactionSurvey>()
            .HasCheckConstraint("CK_ResponseTime", "[ResponseTime] >= 1 AND [ResponseTime] <= 5");
        
        modelBuilder.Entity<UserSatisfactionSurvey>()
            .HasCheckConstraint("CK_HelpfulnessRating", "[HelpfulnessRating] >= 1 AND [HelpfulnessRating] <= 5");

        // Add check constraints for meeting duration
        modelBuilder.Entity<Meeting>()
            .HasCheckConstraint("CK_DurationMinutes", "[DurationMinutes] > 0 AND [DurationMinutes] <= 480"); // Max 8 hours

        // Add check constraints for file size
        modelBuilder.Entity<KnowledgeDocument>()
            .HasCheckConstraint("CK_FileSize", "[FileSize] > 0 AND [FileSize] <= 104857600"); // Max 100MB

        // Add check constraints for confidence scores
        modelBuilder.Entity<ChatMessage>()
            .HasCheckConstraint("CK_Confidence", "[Confidence] IS NULL OR ([Confidence] >= 0.0 AND [Confidence] <= 1.0)");

        modelBuilder.Entity<KnowledgeBaseQuery>()
            .HasCheckConstraint("CK_BestMatchScore", "[BestMatchScore] IS NULL OR ([BestMatchScore] >= 0.0 AND [BestMatchScore] <= 1.0)");
    }

    // Override SaveChanges to add automatic timestamp updates
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is ChatSession && e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is ChatSession session)
            {
                session.LastActivityAt = DateTime.UtcNow;
            }
        }
    }
}