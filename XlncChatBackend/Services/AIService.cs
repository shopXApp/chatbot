using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.Text;
using XlncChatBackend.Configuration;
using XlncChatBackend.Data;
using XlncChatBackend.Models;
using XlncChatBackend.Services.Interfaces;

namespace XlncChatBackend.Services;

public class AIService : IAIService
{
    private readonly OpenAIClient _openAIClient;
    private readonly IVectorDatabaseService _vectorDb;
    private readonly ChatDbContext _context;
    private readonly AIServiceOptions _options;
    private readonly ILogger<AIService> _logger;

    public AIService(
        IOptions<AIServiceOptions> options,
        IVectorDatabaseService vectorDb,
        ChatDbContext context,
        ILogger<AIService> logger)
    {
        _options = options.Value;
        _vectorDb = vectorDb;
        _context = context;
        _logger = logger;
        
        _openAIClient = new OpenAIClient(_options.ApiKey);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var embeddingClient = _openAIClient.GetEmbeddingClient(_options.EmbeddingModel);
            var embedding = await embeddingClient.GenerateEmbeddingAsync(text);
            
            return embedding.Value.Vector.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            throw;
        }
    }

    public async Task<ChatMessageResponse> GenerateResponseAsync(
        string query, 
        string sessionId, 
        string apiKey,
        Dictionary<string, object>? context = null)
    {
        try
        {
            _logger.LogInformation("Generating AI response for session {SessionId}", sessionId);

            // Step 1: Generate embedding for the query
            var queryEmbedding = await GenerateEmbeddingAsync(query);

            // Step 2: Search for relevant content in knowledge base
            var searchResults = await _vectorDb.SearchSimilarAsync(
                queryEmbedding, 
                limit: _options.MaxSearchResults,
                threshold: _options.SimilarityThreshold,
                filters: new Dictionary<string, object> { ["apiKey"] = apiKey }
            );

            // Step 3: Log the query for analytics
            await LogKnowledgeBaseQuery(sessionId, query, searchResults);

            // Step 4: Generate contextual response
            if (searchResults.Any())
            {
                return await GenerateRAGResponseAsync(query, searchResults, sessionId, context);
            }
            else
            {
                return await GenerateFallbackResponseAsync(query, sessionId, context);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response");
            return new ChatMessageResponse
            {
                Response = "I apologize, but I'm experiencing technical difficulties. Please try again later or contact our support team for immediate assistance.",
                Source = "error",
                Success = false,
                Error = "AI service error"
            };
        }
    }

    private async Task<ChatMessageResponse> GenerateRAGResponseAsync(
        string query, 
        List<VectorSearchResult> searchResults,
        string sessionId,
        Dictionary<string, object>? context)
    {
        try
        {
            // Prepare context from search results
            var relevantContent = searchResults
                .OrderByDescending(r => r.Score)
                .Take(_options.MaxContextChunks)
                .Select(r => r.Content)
                .ToList();

            var contextText = string.Join("\n\n", relevantContent);
            
            // Build the prompt for RAG
            var systemPrompt = BuildRAGSystemPrompt();
            var userPrompt = BuildRAGUserPrompt(query, contextText, context);

            // Generate response using OpenAI
            var chatClient = _openAIClient.GetChatClient(_options.ChatModel);
            
            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = _options.Temperature,
                MaxTokens = _options.MaxTokens,
                TopP = _options.TopP
            };

            var response = await chatClient.CompleteChatAsync(chatMessages, chatOptions);
            var generatedResponse = response.Value.Content[0].Text;

            // Format the response as structured paragraphs
            var formattedResponse = FormatResponseAsParagraphs(generatedResponse);

            // Determine if we should offer quick actions
            var actions = DetermineQuickActions(query, searchResults);

            // Calculate confidence based on search results
            var confidence = CalculateConfidence(searchResults);

            return new ChatMessageResponse
            {
                Response = formattedResponse,
                Actions = actions,
                Source = "knowledge_base",
                Confidence = confidence,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RAG response");
            return await GenerateFallbackResponseAsync(query, sessionId, context);
        }
    }

    private async Task<ChatMessageResponse> GenerateFallbackResponseAsync(
        string query, 
        string sessionId,
        Dictionary<string, object>? context)
    {
        try
        {
            var systemPrompt = BuildFallbackSystemPrompt();
            var userPrompt = BuildFallbackUserPrompt(query, context);

            var chatClient = _openAIClient.GetChatClient(_options.ChatModel);
            
            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = _options.Temperature + 0.1f, // Slightly more creative for fallback
                MaxTokens = _options.MaxTokens,
                TopP = _options.TopP
            };

            var response = await chatClient.CompleteChatAsync(chatMessages, chatOptions);
            var generatedResponse = response.Value.Content[0].Text;

            // Format as paragraphs
            var formattedResponse = FormatResponseAsParagraphs(generatedResponse);

            // Offer escalation actions
            var actions = new List<QuickAction>
            {
                new() { Id = "schedule_meeting", Text = "Schedule a Meeting", Icon = "📅" },
                new() { Id = "contact_support", Text = "Contact Support", Icon = "💬" },
                new() { Id = "browse_services", Text = "Browse Our Services", Icon = "🛠️" }
            };

            return new ChatMessageResponse
            {
                Response = formattedResponse,
                Actions = actions,
                Source = "ai",
                Confidence = 0.6, // Lower confidence for fallback
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating fallback response");
            return new ChatMessageResponse
            {
                Response = "I apologize, but I don't have specific information about that topic in my knowledge base. However, I'd be happy to help you schedule a meeting with our experts who can provide detailed assistance with your inquiry.",
                Actions = new List<QuickAction>
                {
                    new() { Id = "schedule_meeting", Text = "Schedule Meeting", Icon = "📅" },
                    new() { Id = "contact_support", Text = "Contact Support", Icon = "💬" }
                },
                Source = "fallback",
                Confidence = 0.3,
                Success = true
            };
        }
    }

    private string BuildRAGSystemPrompt()
    {
        return @"You are an AI assistant for XLNC Technologies, a comprehensive IT services company. Your role is to provide helpful, accurate, and professional responses based on the provided context from our knowledge base.

Guidelines:
1. Use the provided context to answer questions accurately and comprehensively
2. Format your responses in clear, well-structured paragraphs
3. Be professional and helpful in tone
4. If the context doesn't fully answer the question, acknowledge this and offer to connect them with an expert
5. Always aim to be helpful and solution-oriented
6. Use bullet points or numbered lists when appropriate for clarity
7. End with a question or offer additional assistance when relevant

Context will be provided with each query. Base your response primarily on this context while maintaining a natural conversational flow.";
    }

    private string BuildRAGUserPrompt(string query, string contextText, Dictionary<string, object>? additionalContext)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("Context from knowledge base:");
        promptBuilder.AppendLine("---");
        promptBuilder.AppendLine(contextText);
        promptBuilder.AppendLine("---");
        promptBuilder.AppendLine();

        if (additionalContext != null && additionalContext.Any())
        {
            promptBuilder.AppendLine("Additional context:");
            foreach (var kvp in additionalContext)
            {
                promptBuilder.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine($"User question: {query}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Please provide a comprehensive response based on the context above. Format your response in clear paragraphs:");

        return promptBuilder.ToString();
    }

    private string BuildFallbackSystemPrompt()
    {
        return @"You are an AI assistant for XLNC Technologies, a comprehensive IT services company specializing in web development, mobile app development, cloud solutions, AI integration, and digital transformation.

Since specific information wasn't found in our knowledge base, provide a helpful general response that:
1. Acknowledges the question
2. Provides general information about XLNC Technologies' capabilities in that area
3. Offers to connect them with a specialist for detailed information
4. Maintains a professional and helpful tone
5. Formats the response in clear paragraphs

Company services include:
- Web Development (React, Angular, .NET Core)
- Mobile App Development (iOS, Android, Cross-platform)
- Cloud Solutions (AWS, Azure, Google Cloud)
- AI Integration and Machine Learning
- Digital Transformation Consulting
- Enterprise Software Development
- Technical Support and Maintenance";
    }

    private string BuildFallbackUserPrompt(string query, Dictionary<string, object>? context)
    {
        var promptBuilder = new StringBuilder();
        
        if (context != null && context.Any())
        {
            promptBuilder.AppendLine("Additional context:");
            foreach (var kvp in context)
            {
                promptBuilder.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine($"User question: {query}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Since this wasn't found in our specific knowledge base, provide a helpful general response about how XLNC Technologies might be able to assist with this topic. Format your response in clear paragraphs:");

        return promptBuilder.ToString();
    }

    private string FormatResponseAsParagraphs(string response)
    {
        // Clean up the response and ensure proper paragraph formatting
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                           .Select(line => line.Trim())
                           .Where(line => !string.IsNullOrEmpty(line))
                           .ToList();

        var formattedLines = new List<string>();
        var currentParagraph = new StringBuilder();

        foreach (var line in lines)
        {
            // If line starts with bullet point or number, treat as separate line
            if (line.StartsWith("•") || line.StartsWith("-") || line.StartsWith("*") || 
                System.Text.RegularExpressions.Regex.IsMatch(line, @"^\d+\."))
            {
                // Finish current paragraph if exists
                if (currentParagraph.Length > 0)
                {
                    formattedLines.Add(currentParagraph.ToString().Trim());
                    currentParagraph.Clear();
                }
                formattedLines.Add(line);
            }
            else if (line.Length < 50 && (line.EndsWith(":") || line.All(char.IsUpper)))
            {
                // Likely a header or title
                if (currentParagraph.Length > 0)
                {
                    formattedLines.Add(currentParagraph.ToString().Trim());
                    currentParagraph.Clear();
                }
                formattedLines.Add(line);
            }
            else
            {
                // Regular paragraph content
                if (currentParagraph.Length > 0)
                {
                    currentParagraph.Append(" ");
                }
                currentParagraph.Append(line);

                // If this looks like end of sentence/paragraph
                if (line.EndsWith(".") || line.EndsWith("!") || line.EndsWith("?"))
                {
                    formattedLines.Add(currentParagraph.ToString().Trim());
                    currentParagraph.Clear();
                }
            }
        }

        // Add any remaining content
        if (currentParagraph.Length > 0)
        {
            formattedLines.Add(currentParagraph.ToString().Trim());
        }

        return string.Join("\n\n", formattedLines);
    }

    private List<QuickAction>? DetermineQuickActions(string query, List<VectorSearchResult> searchResults)
    {
        var queryLower = query.ToLowerInvariant();
        var actions = new List<QuickAction>();

        // Analyze query intent and search results to suggest actions
        if (queryLower.Contains("price") || queryLower.Contains("cost") || queryLower.Contains("quote"))
        {
            actions.Add(new QuickAction { Id = "get_quote", Text = "Get Custom Quote", Icon = "💰" });
        }

        if (queryLower.Contains("meeting") || queryLower.Contains("call") || queryLower.Contains("discuss"))
        {
            actions.Add(new QuickAction { Id = "schedule_meeting", Text = "Schedule Meeting", Icon = "📅" });
        }

        if (queryLower.Contains("demo") || queryLower.Contains("example") || queryLower.Contains("show"))
        {
            actions.Add(new QuickAction { Id = "request_demo", Text = "Request Demo", Icon = "🎥" });
        }

        // Check search result categories for additional actions
        var categories = searchResults.SelectMany(r => 
            r.Metadata.GetValueOrDefault("category")?.ToString()?.Split(',') ?? Array.Empty<string>())
            .Distinct()
            .ToList();

        if (categories.Contains("technical"))
        {
            actions.Add(new QuickAction { Id = "technical_support", Text = "Technical Support", Icon = "🔧" });
        }

        if (categories.Contains("business"))
        {
            actions.Add(new QuickAction { Id = "business_consultation", Text = "Business Consultation", Icon = "💼" });
        }

        // Always offer general support if no specific actions
        if (!actions.Any())
        {
            actions.Add(new QuickAction { Id = "contact_support", Text = "Contact Support", Icon = "💬" });
        }

        return actions.Any() ? actions : null;
    }

    private double CalculateConfidence(List<VectorSearchResult> searchResults)
    {
        if (!searchResults.Any()) return 0.0;

        var avgScore = searchResults.Average(r => r.Score);
        var maxScore = searchResults.Max(r => r.Score);
        var resultCount = Math.Min(searchResults.Count, 5);

        // Confidence based on search quality and quantity
        var confidence = (avgScore * 0.6) + (maxScore * 0.3) + (resultCount / 10.0 * 0.1);
        
        return Math.Min(confidence, 1.0);
    }

    private async Task LogKnowledgeBaseQuery(
        string sessionId, 
        string query, 
        List<VectorSearchResult> searchResults)
    {
        try
        {
            var kbQuery = new KnowledgeBaseQuery
            {
                SessionId = sessionId,
                Query = query,
                MatchedChunks = searchResults.Select(r => r.Id).ToList(),
                BestMatchScore = searchResults.Any() ? searchResults.Max(r => r.Score) : null
            };

            _context.KnowledgeBaseQueries.Add(kbQuery);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log knowledge base query");
        }
    }

    public async Task<List<string>> GenerateKeywordsAsync(string content)
    {
        try
        {
            var systemPrompt = "Extract the 10 most important keywords and key phrases from the following content. Return only the keywords, one per line, without any additional text or formatting.";
            var userPrompt = $"Content: {content}";

            var chatClient = _openAIClient.GetChatClient(_options.ChatModel);
            
            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.3f,
                MaxTokens = 200
            };

            var response = await chatClient.CompleteChatAsync(chatMessages, chatOptions);
            var keywords = response.Value.Content[0].Text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim().Trim('-', '*', '•').Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Take(10)
                .ToList();

            return keywords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating keywords");
            return new List<string>();
        }
    }

    public async Task<string> SummarizeContentAsync(string content, int maxLength = 500)
    {
        try
        {
            var systemPrompt = $"Summarize the following content in approximately {maxLength} characters. Make it concise but comprehensive, highlighting the key points.";
            var userPrompt = $"Content to summarize: {content}";

            var chatClient = _openAIClient.GetChatClient(_options.ChatModel);
            
            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.3f,
                MaxTokens = Math.Max(maxLength / 3, 100) // Rough token estimation
            };

            var response = await chatClient.CompleteChatAsync(chatMessages, chatOptions);
            return response.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary");
            return content.Length > maxLength ? content.Substring(0, maxLength) + "..." : content;
        }
    }

    public async Task<string> CategorizeContentAsync(string content, List<string> availableCategories)
    {
        try
        {
            var systemPrompt = $"Categorize the following content into one of these categories: {string.Join(", ", availableCategories)}. Return only the category name that best fits the content.";
            var userPrompt = $"Content: {content}";

            var chatClient = _openAIClient.GetChatClient(_options.ChatModel);
            
            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.1f,
                MaxTokens = 50
            };

            var response = await chatClient.CompleteChatAsync(chatMessages, chatOptions);
            var category = response.Value.Content[0].Text.Trim();

            // Validate that the returned category is in the available list
            return availableCategories.FirstOrDefault(c => 
                string.Equals(c, category, StringComparison.OrdinalIgnoreCase)) 
                ?? availableCategories.FirstOrDefault() 
                ?? "general";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error categorizing content");
            return availableCategories.FirstOrDefault() ?? "general";
        }
    }
}