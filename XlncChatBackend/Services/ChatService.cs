using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using XlncChatBackend.Configuration;
using XlncChatBackend.Data;
using XlncChatBackend.Models;
using XlncChatBackend.Services.Interfaces;

namespace XlncChatBackend.Services;

public class ChatService : IChatService
{
    private readonly ChatDbContext _context;
    private readonly IAIService _aiService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ISecurityService _securityService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ApiOptions _options;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        ChatDbContext context,
        IAIService aiService,
        IAnalyticsService analyticsService,
        ISecurityService securityService,
        IRateLimitingService rateLimitingService,
        IOptions<ApiOptions> options,
        ILogger<ChatService> logger)
    {
        _context = context;
        _aiService = aiService;
        _analyticsService = analyticsService;
        _securityService = securityService;
        _rateLimitingService = rateLimitingService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AuthenticationResponse> AuthenticateAsync(
        AuthenticationRequest request, 
        string apiKey, 
        string ipAddress, 
        string? userAgent)
    {
        try
        {
            _logger.LogInformation("Authenticating user session for API key: {ApiKey}", apiKey);

            // Validate API key
            var apiConfig = await _securityService.GetApiConfigurationAsync(apiKey);
            if (apiConfig == null || !apiConfig.IsActive)
            {
                _logger.LogWarning("Invalid or inactive API key: {ApiKey}", apiKey);
                return new AuthenticationResponse
                {
                    Success = false,
                    Error = "Invalid API key"
                };
            }

            // Check rate limits
            var rateLimitResult = await _rateLimitingService.CheckRateLimitAsync(
                ipAddress, 
                "authenticate", 
                apiConfig.RateLimits);

            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", ipAddress);
                return new AuthenticationResponse
                {
                    Success = false,
                    Error = "Rate limit exceeded. Please try again later."
                };
            }

            // Create new chat session
            var session = new ChatSession
            {
                ApiKey = apiKey,
                UserName = request.UserInfo.Name,
                UserEmail = request.UserInfo.Email,
                UserPhone = request.UserInfo.Phone,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Referrer = request.Referrer,
                Status = ChatSessionStatus.Active
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            // Track authentication event
            await _analyticsService.TrackEventAsync(session.Id, "session_start", new Dictionary<string, object>
            {
                ["userAgent"] = userAgent ?? "",
                ["ipAddress"] = ipAddress,
                ["referrer"] = request.Referrer ?? "",
                ["apiKey"] = apiKey
            });

            _logger.LogInformation("User authenticated successfully. Session ID: {SessionId}", session.Id);

            return new AuthenticationResponse
            {
                SessionId = session.Id,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication");
            return new AuthenticationResponse
            {
                Success = false,
                Error = "Authentication failed due to an internal error"
            };
        }
    }

    public async Task<ChatMessageResponse> ProcessMessageAsync(
        ChatMessageRequest request, 
        string apiKey, 
        string ipAddress)
    {
        try
        {
            _logger.LogInformation("Processing message for session: {SessionId}", request.SessionId);

            // Validate session
            var session = await GetSessionAsync(request.SessionId);
            if (session == null)
            {
                return new ChatMessageResponse
                {
                    Success = false,
                    Error = "Invalid session ID"
                };
            }

            if (session.ApiKey != apiKey)
            {
                return new ChatMessageResponse
                {
                    Success = false,
                    Error = "Session does not belong to this API key"
                };
            }

            // Get API configuration for rate limiting
            var apiConfig = await _securityService.GetApiConfigurationAsync(apiKey);
            if (apiConfig == null)
            {
                return new ChatMessageResponse
                {
                    Success = false,
                    Error = "Invalid API configuration"
                };
            }

            // Check rate limits
            var rateLimitResult = await _rateLimitingService.CheckRateLimitAsync(
                ipAddress, 
                "message", 
                apiConfig.RateLimits);

            if (!rateLimitResult.IsAllowed)
            {
                return new ChatMessageResponse
                {
                    Success = false,
                    Error = "Rate limit exceeded. Please slow down."
                };
            }

            // Save user message
            var userMessage = new ChatMessage
            {
                SessionId = request.SessionId,
                Content = request.Message,
                Sender = MessageSender.User,
                Type = MessageType.Text,
                Status = MessageStatus.Delivered
            };

            _context.ChatMessages.Add(userMessage);
            await _context.SaveChangesAsync();

            // Update session activity
            await UpdateSessionActivityAsync(request.SessionId);

            // Track message event
            await _analyticsService.TrackEventAsync(request.SessionId, "message_sent", new Dictionary<string, object>
            {
                ["messageLength"] = request.Message.Length,
                ["sender"] = "user",
                ["ipAddress"] = ipAddress
            });

            // Generate AI response
            var aiResponse = await _aiService.GenerateResponseAsync(
                request.Message, 
                request.SessionId, 
                apiKey, 
                request.Context);

            // Save bot response
            var botMessage = new ChatMessage
            {
                SessionId = request.SessionId,
                Content = aiResponse.Response,
                Sender = MessageSender.Bot,
                Type = MessageType.Text,
                Status = MessageStatus.Sent,
                ResponseSource = aiResponse.Source,
                Confidence = aiResponse.Confidence,
                Actions = aiResponse.Actions
            };

            _context.ChatMessages.Add(botMessage);
            await _context.SaveChangesAsync();

            // Track bot response event
            await _analyticsService.TrackEventAsync(request.SessionId, "response_generated", new Dictionary<string, object>
            {
                ["source"] = aiResponse.Source,
                ["confidence"] = aiResponse.Confidence,
                ["responseLength"] = aiResponse.Response.Length,
                ["hasActions"] = aiResponse.Actions?.Any() == true
            });

            _logger.LogInformation("Message processed successfully for session: {SessionId}", request.SessionId);

            return aiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for session: {SessionId}", request.SessionId);
            
            // Track error event
            await _analyticsService.TrackEventAsync(request.SessionId, "message_processing_error", new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["messageLength"] = request.Message?.Length ?? 0
            });

            return new ChatMessageResponse
            {
                Success = false,
                Error = "Failed to process message. Please try again.",
                Response = "I apologize, but I'm experiencing technical difficulties. Please try again or contact our support team for immediate assistance."
            };
        }
    }

    public async Task<ChatSession?> GetSessionAsync(string sessionId)
    {
        try
        {
            return await _context.ChatSessions
                .Include(s => s.Messages.OrderBy(m => m.Timestamp))
                .Include(s => s.Analytics)
                .Include(s => s.Meetings)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<List<ChatMessage>> GetChatHistoryAsync(string sessionId, int skip = 0, int take = 50)
    {
        try
        {
            return await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.Timestamp)
                .Skip(skip)
                .Take(Math.Min(take, _options.MaxPageSize))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history for session: {SessionId}", sessionId);
            return new List<ChatMessage>();
        }
    }

    public async Task<bool> EndSessionAsync(string sessionId)
    {
        try
        {
            var session = await _context.ChatSessions.FindAsync(sessionId);
            if (session == null)
            {
                return false;
            }

            session.Status = ChatSessionStatus.Ended;
            session.LastActivityAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Track session end event
            await _analyticsService.TrackEventAsync(sessionId, "session_end", new Dictionary<string, object>
            {
                ["duration"] = (DateTime.UtcNow - session.CreatedAt).TotalMinutes,
                ["messageCount"] = await _context.ChatMessages.CountAsync(m => m.SessionId == sessionId)
            });

            _logger.LogInformation("Session ended: {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending session: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task UpdateSessionActivityAsync(string sessionId)
    {
        try
        {
            var session = await _context.ChatSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating session activity: {SessionId}", sessionId);
        }
    }

    public async Task<List<ChatSession>> GetActiveSessionsAsync(string apiKey, int minutes = 30)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-minutes);
            
            return await _context.ChatSessions
                .Where(s => s.ApiKey == apiKey && 
                           s.Status == ChatSessionStatus.Active && 
                           s.LastActivityAt >= cutoffTime)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions for API key: {ApiKey}", apiKey);
            return new List<ChatSession>();
        }
    }

    public async Task<Dictionary<string, object>> GetSessionSummaryAsync(string sessionId)
    {
        try
        {
            var session = await GetSessionAsync(sessionId);
            if (session == null)
            {
                return new Dictionary<string, object>();
            }

            var messageCount = session.Messages.Count;
            var userMessages = session.Messages.Count(m => m.Sender == MessageSender.User);
            var botMessages = session.Messages.Count(m => m.Sender == MessageSender.Bot);
            var duration = DateTime.UtcNow - session.CreatedAt;
            var avgConfidence = session.Messages
                .Where(m => m.Confidence.HasValue)
                .Average(m => m.Confidence) ?? 0;

            return new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["status"] = session.Status.ToString(),
                ["duration"] = duration.TotalMinutes,
                ["messageCount"] = messageCount,
                ["userMessages"] = userMessages,
                ["botMessages"] = botMessages,
                ["averageConfidence"] = avgConfidence,
                ["userName"] = session.UserName,
                ["userEmail"] = session.UserEmail,
                ["createdAt"] = session.CreatedAt,
                ["lastActivity"] = session.LastActivityAt,
                ["meetingCount"] = session.Meetings.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session summary: {SessionId}", sessionId);
            return new Dictionary<string, object>();
        }
    }

    public async Task<bool> TransferSessionAsync(string sessionId, string agentId, string reason)
    {
        try
        {
            var session = await _context.ChatSessions.FindAsync(sessionId);
            if (session == null)
            {
                return false;
            }

            session.Status = ChatSessionStatus.Transferred;
            session.LastActivityAt = DateTime.UtcNow;

            // Add system message about transfer
            var transferMessage = new ChatMessage
            {
                SessionId = sessionId,
                Content = $"Chat has been transferred to a human agent. Reason: {reason}",
                Sender = MessageSender.Bot,
                Type = MessageType.System,
                Status = MessageStatus.Sent
            };

            _context.ChatMessages.Add(transferMessage);
            await _context.SaveChangesAsync();

            // Track transfer event
            await _analyticsService.TrackEventAsync(sessionId, "session_transferred", new Dictionary<string, object>
            {
                ["agentId"] = agentId,
                ["reason"] = reason,
                ["transferTime"] = DateTime.UtcNow
            });

            _logger.LogInformation("Session transferred: {SessionId} to agent: {AgentId}", sessionId, agentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring session: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<List<string>> GetPopularQueriesAsync(string apiKey, int days = 7, int limit = 10)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            
            var queries = await _context.ChatMessages
                .Join(_context.ChatSessions, 
                      m => m.SessionId, 
                      s => s.Id, 
                      (m, s) => new { Message = m, Session = s })
                .Where(x => x.Session.ApiKey == apiKey && 
                           x.Message.Sender == MessageSender.User && 
                           x.Message.Timestamp >= startDate)
                .GroupBy(x => x.Message.Content.ToLower())
                .OrderByDescending(g => g.Count())
                .Take(limit)
                .Select(g => g.Key)
                .ToListAsync();

            return queries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving popular queries for API key: {ApiKey}", apiKey);
            return new List<string>();
        }
    }

    public async Task<bool> AbandonSessionAsync(string sessionId, string reason)
    {
        try
        {
            var session = await _context.ChatSessions.FindAsync(sessionId);
            if (session == null)
            {
                return false;
            }

            session.Status = ChatSessionStatus.Abandoned;
            session.LastActivityAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Track abandonment event
            await _analyticsService.TrackEventAsync(sessionId, "session_abandoned", new Dictionary<string, object>
            {
                ["reason"] = reason,
                ["duration"] = (DateTime.UtcNow - session.CreatedAt).TotalMinutes,
                ["messageCount"] = await _context.ChatMessages.CountAsync(m => m.SessionId == sessionId)
            });

            _logger.LogInformation("Session abandoned: {SessionId}, Reason: {Reason}", sessionId, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking session as abandoned: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task CleanupInactiveSessionsAsync(int inactiveHours = 24)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-inactiveHours);
            
            var inactiveSessions = await _context.ChatSessions
                .Where(s => s.Status == ChatSessionStatus.Active && 
                           s.LastActivityAt < cutoffTime)
                .ToListAsync();

            foreach (var session in inactiveSessions)
            {
                await AbandonSessionAsync(session.Id, "Inactive timeout");
            }

            _logger.LogInformation("Cleaned up {Count} inactive sessions", inactiveSessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session cleanup");
        }
    }
}