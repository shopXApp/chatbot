using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using XlncChatBackend.Models;
using XlncChatBackend.Services.Interfaces;

namespace XlncChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("GlobalPolicy")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IEmailService _emailService;
    private readonly ISecurityService _securityService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IAnalyticsService analyticsService,
        IEmailService emailService,
        ISecurityService securityService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _analyticsService = analyticsService;
        _emailService = emailService;
        _securityService = securityService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate a new chat session
    /// </summary>
    [HttpPost("authenticate")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<ActionResult<AuthenticationResponse>> Authenticate([FromBody] AuthenticationRequest request)
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var result = await _chatService.AuthenticateAsync(request, apiKey, ipAddress, userAgent);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authenticate endpoint");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Send a message and get AI response
    /// </summary>
    [HttpPost("message")]
    [EnableRateLimiting("MessagePolicy")]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage([FromBody] ChatMessageRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            var ipAddress = GetClientIpAddress();
            var result = await _chatService.ProcessMessageAsync(request, apiKey, ipAddress);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in message endpoint");
            return StatusCode(500, new ChatMessageResponse
            {
                Success = false,
                Error = "Internal server error",
                Response = "I apologize, but I'm experiencing technical difficulties. Please try again later."
            });
        }
    }

    /// <summary>
    /// Get chat history for a session
    /// </summary>
    [HttpGet("history/{sessionId}")]
    public async Task<ActionResult<List<ChatMessage>>> GetChatHistory(
        string sessionId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            // Validate session belongs to API key
            var session = await _chatService.GetSessionAsync(sessionId);
            if (session == null || session.ApiKey != apiKey)
            {
                return NotFound(new { error = "Session not found" });
            }

            var history = await _chatService.GetChatHistoryAsync(sessionId, skip, take);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// End a chat session
    /// </summary>
    [HttpPost("end")]
    public async Task<ActionResult> EndSession([FromBody] EndSessionRequest request)
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            // Validate session belongs to API key
            var session = await _chatService.GetSessionAsync(request.SessionId);
            if (session == null || session.ApiKey != apiKey)
            {
                return NotFound(new { error = "Session not found" });
            }

            var success = await _chatService.EndSessionAsync(request.SessionId);
            if (!success)
            {
                return BadRequest(new { error = "Failed to end session" });
            }

            return Ok(new { message = "Session ended successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending session: {SessionId}", request?.SessionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Email chat transcript to user
    /// </summary>
    [HttpPost("email-transcript")]
    public async Task<ActionResult> EmailTranscript([FromBody] EmailTranscriptRequest request)
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            // Validate session belongs to API key
            var session = await _chatService.GetSessionAsync(request.SessionId);
            if (session == null || session.ApiKey != apiKey)
            {
                return NotFound(new { error = "Session not found" });
            }

            // Get chat history
            var messages = await _chatService.GetChatHistoryAsync(request.SessionId);
            
            // Send email
            var success = await _emailService.SendChatTranscriptAsync(request.Email, messages, request.SessionId);
            
            if (!success)
            {
                return BadRequest(new { error = "Failed to send transcript" });
            }

            // Track email transcript event
            await _analyticsService.TrackEventAsync(request.SessionId, "transcript_emailed", new Dictionary<string, object>
            {
                ["email"] = request.Email,
                ["messageCount"] = messages.Count
            });

            return Ok(new { message = "Transcript sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transcript for session: {SessionId}", request?.SessionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get session information
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult> GetSession(string sessionId)
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            var session = await _chatService.GetSessionAsync(sessionId);
            if (session == null || session.ApiKey != apiKey)
            {
                return NotFound(new { error = "Session not found" });
            }

            var summary = await _chatService.GetSessionSummaryAsync(sessionId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user preferences (sound settings, etc.)
    /// </summary>
    [HttpPost("preferences")]
    public async Task<ActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            // Validate session belongs to API key
            var session = await _chatService.GetSessionAsync(request.SessionId);
            if (session == null || session.ApiKey != apiKey)
            {
                return NotFound(new { error = "Session not found" });
            }

            // Track preference changes
            await _analyticsService.TrackEventAsync(request.SessionId, "preferences_updated", new Dictionary<string, object>
            {
                ["preferences"] = request.Preferences
            });

            return Ok(new { message = "Preferences updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for session: {SessionId}", request?.SessionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Submit user satisfaction survey
    /// </summary>
    [HttpPost("satisfaction")]
    public async Task<ActionResult> SubmitSatisfactionSurvey([FromBody] UserSatisfactionSurvey survey)
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            // Validate session belongs to API key
            var session = await _chatService.GetSessionAsync(survey.SessionId);
            if (session == null || session.ApiKey != apiKey)
            {
                return NotFound(new { error = "Session not found" });
            }

            await _analyticsService.RecordUserSatisfactionAsync(survey);

            return Ok(new { message = "Thank you for your feedback!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting satisfaction survey");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get popular queries for analytics
    /// </summary>
    [HttpGet("popular-queries")]
    public async Task<ActionResult<List<string>>> GetPopularQueries(
        [FromQuery] int days = 7,
        [FromQuery] int limit = 10)
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            var queries = await _chatService.GetPopularQueriesAsync(apiKey, days, limit);
            return Ok(queries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving popular queries");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get API status and configuration
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            var config = await _securityService.GetApiConfigurationAsync(apiKey);
            if (config == null)
            {
                return NotFound(new { error = "API configuration not found" });
            }

            var activeSessionsCount = (await _chatService.GetActiveSessionsAsync(apiKey)).Count;

            return Ok(new
            {
                online = true,
                agentsAvailable = 1, // This would come from agent management system
                averageResponseTime = "< 1 minute",
                activeSessions = activeSessionsCount,
                apiKey = apiKey,
                companyName = config.CompanyName,
                chatSettings = config.ChatSettings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Transfer session to human agent
    /// </summary>
    [HttpPost("transfer")]
    public async Task<ActionResult> TransferSession([FromBody] TransferSessionRequest request)
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            // Validate session belongs to API key
            var session = await _chatService.GetSessionAsync(request.SessionId);
            if (session == null || session.ApiKey != apiKey)
            {
                return NotFound(new { error = "Session not found" });
            }

            var success = await _chatService.TransferSessionAsync(
                request.SessionId, 
                request.AgentId ?? "default_agent", 
                request.Reason ?? "User requested transfer");

            if (!success)
            {
                return BadRequest(new { error = "Failed to transfer session" });
            }

            return Ok(new { message = "Session transferred successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring session: {SessionId}", request?.SessionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update session activity (heartbeat)
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<ActionResult> UpdateActivity([FromBody] HeartbeatRequest request)
    {
        try
        {
            var apiKey = GetApiKeyFromHeader();
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            // Validate session belongs to API key
            var session = await _chatService.GetSessionAsync(request.SessionId);
            if (session == null || session.ApiKey != apiKey)
            {
                return NotFound(new { error = "Session not found" });
            }

            await _chatService.UpdateSessionActivityAsync(request.SessionId);
            return Ok(new { message = "Activity updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating activity for session: {SessionId}", request?.SessionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private string GetApiKeyFromHeader()
    {
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authValue = authHeader.FirstOrDefault();
            if (!string.IsNullOrEmpty(authValue) && authValue.StartsWith("Bearer "))
            {
                return authValue.Substring("Bearer ".Length);
            }
        }

        // Also check X-API-Key header
        if (Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            return apiKeyHeader.FirstOrDefault() ?? string.Empty;
        }

        return string.Empty;
    }

    private string GetClientIpAddress()
    {
        // Check for forwarded IP first (for load balancers/proxies)
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var firstIp = forwardedFor.FirstOrDefault()?.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(firstIp))
            {
                return firstIp;
            }
        }

        // Check for real IP
        if (Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            var ip = realIp.FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        // Fallback to connection remote IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

// Request/Response DTOs
public class EndSessionRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;
}

public class EmailTranscriptRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class UpdatePreferencesRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;
    
    public Dictionary<string, object> Preferences { get; set; } = new();
}

public class TransferSessionRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;
    
    public string? AgentId { get; set; }
    public string? Reason { get; set; }
}

public class HeartbeatRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;
}