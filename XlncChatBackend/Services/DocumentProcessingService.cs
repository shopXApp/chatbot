using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using XlncChatBackend.Configuration;
using XlncChatBackend.Data;
using XlncChatBackend.Models;
using XlncChatBackend.Services.Interfaces;

namespace XlncChatBackend.Services;

public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly ChatDbContext _context;
    private readonly IVirusScanningService _virusScanner;
    private readonly IVectorDatabaseService _vectorDb;
    private readonly IAIService _aiService;
    private readonly ILogger<DocumentProcessingService> _logger;
    private readonly DocumentProcessingOptions _options;

    public DocumentProcessingService(
        ChatDbContext context,
        IVirusScanningService virusScanner,
        IVectorDatabaseService vectorDb,
        IAIService aiService,
        ILogger<DocumentProcessingService> logger,
        IOptions<DocumentProcessingOptions> options)
    {
        _context = context;
        _virusScanner = virusScanner;
        _vectorDb = vectorDb;
        _aiService = aiService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<DocumentUploadResponse> ProcessDocumentAsync(
        IFormFile file, 
        string apiKey, 
        List<string> categories, 
        string? title = null)
    {
        try
        {
            _logger.LogInformation("Starting document processing for file: {FileName}", file.FileName);

            // Validate file
            var validationResult = await ValidateFileAsync(file);
            if (!validationResult.IsValid)
            {
                return new DocumentUploadResponse
                {
                    Success = false,
                    Error = validationResult.Error
                };
            }

            // Create document record
            var document = new KnowledgeDocument
            {
                Title = title ?? Path.GetFileNameWithoutExtension(file.FileName),
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                ApiKey = apiKey,
                Categories = categories,
                Status = DocumentStatus.Processing
            };

            // Save file to storage
            var filePath = await SaveFileAsync(file, document.Id);
            document.FilePath = filePath;

            _context.KnowledgeDocuments.Add(document);
            await _context.SaveChangesAsync();

            // Create processing job
            var job = new ProcessingJob
            {
                Type = JobType.DocumentProcessing,
                DocumentId = document.Id,
                Status = JobStatus.Pending,
                Parameters = new Dictionary<string, object>
                {
                    ["documentId"] = document.Id,
                    ["filePath"] = filePath,
                    ["categories"] = categories
                }
            };

            _context.ProcessingJobs.Add(job);
            await _context.SaveChangesAsync();

            // Start background processing
            _ = Task.Run(() => ProcessDocumentInBackgroundAsync(document.Id));

            return new DocumentUploadResponse
            {
                DocumentId = document.Id,
                Success = true,
                JobId = job.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document upload");
            return new DocumentUploadResponse
            {
                Success = false,
                Error = "An error occurred while processing the document"
            };
        }
    }

    private async Task ProcessDocumentInBackgroundAsync(string documentId)
    {
        var document = await _context.KnowledgeDocuments.FindAsync(documentId);
        if (document == null) return;

        var job = await _context.ProcessingJobs
            .FirstOrDefaultAsync(j => j.DocumentId == documentId && j.Type == JobType.DocumentProcessing);

        try
        {
            if (job != null)
            {
                job.Status = JobStatus.Processing;
                job.StartedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Step 1: Virus scanning
            _logger.LogInformation("Starting virus scan for document {DocumentId}", documentId);
            var virusScanResult = await _virusScanner.ScanFileAsync(document.FilePath);
            
            document.IsVirusScanned = true;
            document.VirusScanResult = virusScanResult.IsClean ? "Clean" : virusScanResult.ThreatName;

            if (!virusScanResult.IsClean)
            {
                document.Status = DocumentStatus.VirusDetected;
                await _context.SaveChangesAsync();
                
                await CreateSecurityAlertAsync(AlertType.VirusDetected, AlertSeverity.High,
                    "Virus detected in uploaded document",
                    $"Document {document.OriginalFileName} contains threat: {virusScanResult.ThreatName}",
                    new Dictionary<string, object>
                    {
                        ["documentId"] = documentId,
                        ["fileName"] = document.OriginalFileName,
                        ["threatName"] = virusScanResult.ThreatName ?? "Unknown"
                    });

                if (job != null)
                {
                    job.Status = JobStatus.Failed;
                    job.ErrorMessage = $"Virus detected: {virusScanResult.ThreatName}";
                    job.CompletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return;
            }

            // Step 2: Extract text content
            _logger.LogInformation("Extracting text content from document {DocumentId}", documentId);
            var textContent = await ExtractTextFromDocumentAsync(document.FilePath, document.ContentType);

            if (string.IsNullOrWhiteSpace(textContent))
            {
                throw new InvalidOperationException("No text content could be extracted from the document");
            }

            // Step 3: Chunk the content
            _logger.LogInformation("Chunking document content for document {DocumentId}", documentId);
            var chunks = ChunkText(textContent, document.Categories);

            // Step 4: Generate embeddings and store in vector database
            _logger.LogInformation("Generating embeddings for {ChunkCount} chunks", chunks.Count);
            var documentChunks = new List<DocumentChunk>();

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                
                // Generate embedding
                var embedding = await _aiService.GenerateEmbeddingAsync(chunk.Content);
                
                // Store in vector database
                var vectorId = await _vectorDb.StoreVectorAsync(
                    embedding, 
                    chunk.Content, 
                    new Dictionary<string, object>
                    {
                        ["documentId"] = documentId,
                        ["chunkIndex"] = i,
                        ["category"] = chunk.Category,
                        ["keywords"] = chunk.Keywords
                    });

                var documentChunk = new DocumentChunk
                {
                    DocumentId = documentId,
                    Content = chunk.Content,
                    ChunkIndex = i,
                    StartPosition = chunk.StartPosition,
                    EndPosition = chunk.EndPosition,
                    Category = chunk.Category,
                    Keywords = chunk.Keywords,
                    Embedding = embedding,
                    VectorId = vectorId
                };

                documentChunks.Add(documentChunk);
            }

            // Step 5: Save chunks to database
            _context.DocumentChunks.AddRange(documentChunks);
            
            document.ChunkCount = chunks.Count;
            document.Status = DocumentStatus.Completed;
            document.ProcessedAt = DateTime.UtcNow;

            if (job != null)
            {
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.Result = new Dictionary<string, object>
                {
                    ["chunkCount"] = chunks.Count,
                    ["textLength"] = textContent.Length,
                    ["categories"] = document.Categories
                };
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully processed document {DocumentId} with {ChunkCount} chunks", 
                documentId, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
            
            document.Status = DocumentStatus.Failed;
            document.ProcessingError = ex.Message;

            if (job != null)
            {
                job.Status = JobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await CreateSecurityAlertAsync(AlertType.ProcessingFailure, AlertSeverity.Medium,
                "Document processing failed",
                $"Failed to process document {document.OriginalFileName}: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["documentId"] = documentId,
                    ["fileName"] = document.OriginalFileName,
                    ["error"] = ex.Message
                });
        }
    }

    private async Task<(bool IsValid, string? Error)> ValidateFileAsync(IFormFile file)
    {
        // Check file size
        if (file.Length > _options.MaxFileSizeBytes)
        {
            return (false, $"File size exceeds maximum allowed size of {_options.MaxFileSizeBytes / (1024 * 1024)}MB");
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension.TrimStart('.')))
        {
            return (false, $"File type {extension} is not allowed. Allowed types: {string.Join(", ", _options.AllowedExtensions)}");
        }

        // Validate file signature (magic numbers)
        var isValidSignature = await ValidateFileSignatureAsync(file, extension);
        if (!isValidSignature)
        {
            return (false, "File signature does not match the file extension. This may indicate a security risk.");
        }

        return (true, null);
    }

    private async Task<bool> ValidateFileSignatureAsync(IFormFile file, string extension)
    {
        using var stream = file.OpenReadStream();
        var buffer = new byte[8];
        await stream.ReadAsync(buffer, 0, buffer.Length);
        stream.Position = 0;

        return extension switch
        {
            ".pdf" => buffer.Take(4).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }), // %PDF
            ".doc" => buffer.Take(8).SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }), // OLE2
            ".docx" => buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }) || // ZIP
                      buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x05, 0x06 }) ||
                      buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x07, 0x08 }),
            _ => false
        };
    }

    private async Task<string> SaveFileAsync(IFormFile file, string documentId)
    {
        var uploadsDirectory = Path.Combine(_options.UploadDirectory, DateTime.UtcNow.ToString("yyyy/MM/dd"));
        Directory.CreateDirectory(uploadsDirectory);

        var fileName = $"{documentId}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsDirectory, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return filePath;
    }

    private async Task<string> ExtractTextFromDocumentAsync(string filePath, string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => await ExtractTextFromPdfAsync(filePath),
            "application/msword" => ExtractTextFromDocAsync(filePath),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ExtractTextFromDocxAsync(filePath),
            _ => throw new NotSupportedException($"Content type {contentType} is not supported")
        };
    }

    private async Task<string> ExtractTextFromPdfAsync(string filePath)
    {
        var text = new StringBuilder();

        try
        {
            using var document = PdfDocument.Open(filePath);
            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                text.AppendLine(pageText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting text from PDF using PdfPig, falling back to iTextSharp");
            
            // Fallback to iTextSharp
            using var reader = new PdfReader(filePath);
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                var pageText = PdfTextExtractor.GetTextFromPage(reader, i);
                text.AppendLine(pageText);
            }
        }

        return text.ToString();
    }

    private string ExtractTextFromDocAsync(string filePath)
    {
        // For .doc files, we would typically use a library like Aspose.Words or NPOI
        // For this implementation, we'll use a simplified approach
        throw new NotImplementedException("DOC file processing requires additional libraries like Aspose.Words or NPOI");
    }

    private string ExtractTextFromDocxAsync(string filePath)
    {
        var text = new StringBuilder();

        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document?.Body;

        if (body != null)
        {
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                text.AppendLine(paragraph.InnerText);
            }

            foreach (var table in body.Elements<Table>())
            {
                foreach (var row in table.Elements<TableRow>())
                {
                    var rowText = string.Join("\t", row.Elements<TableCell>().Select(cell => cell.InnerText));
                    text.AppendLine(rowText);
                }
            }
        }

        return text.ToString();
    }

    private List<TextChunk> ChunkText(string text, List<string> categories)
    {
        var chunks = new List<TextChunk>();
        var sentences = SplitIntoSentences(text);
        
        var currentChunk = new StringBuilder();
        var currentPosition = 0;
        var chunkStartPosition = 0;

        foreach (var sentence in sentences)
        {
            var sentenceWithSpace = sentence + " ";
            
            // Check if adding this sentence would exceed the chunk size
            if (currentChunk.Length + sentenceWithSpace.Length > _options.ChunkSize && currentChunk.Length > 0)
            {
                // Create chunk from current content
                var chunkContent = currentChunk.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    chunks.Add(new TextChunk
                    {
                        Content = chunkContent,
                        StartPosition = chunkStartPosition,
                        EndPosition = currentPosition - 1,
                        Category = DetermineCategory(chunkContent, categories),
                        Keywords = ExtractKeywords(chunkContent)
                    });
                }

                // Start new chunk with overlap
                var overlapText = GetOverlapText(currentChunk.ToString(), _options.OverlapSize);
                currentChunk.Clear();
                currentChunk.Append(overlapText);
                chunkStartPosition = currentPosition - overlapText.Length;
            }

            currentChunk.Append(sentenceWithSpace);
            currentPosition += sentenceWithSpace.Length;
        }

        // Add the last chunk if it has content
        if (currentChunk.Length > 0)
        {
            var chunkContent = currentChunk.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(chunkContent))
            {
                chunks.Add(new TextChunk
                {
                    Content = chunkContent,
                    StartPosition = chunkStartPosition,
                    EndPosition = currentPosition - 1,
                    Category = DetermineCategory(chunkContent, categories),
                    Keywords = ExtractKeywords(chunkContent)
                });
            }
        }

        return chunks;
    }

    private List<string> SplitIntoSentences(string text)
    {
        // Simple sentence splitting - in production, use a more sophisticated NLP library
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        return sentences;
    }

    private string GetOverlapText(string text, int overlapSize)
    {
        if (text.Length <= overlapSize) return text;
        
        var overlap = text.Substring(text.Length - overlapSize);
        
        // Try to start the overlap at a word boundary
        var lastSpaceIndex = overlap.LastIndexOf(' ');
        if (lastSpaceIndex > overlapSize / 2)
        {
            overlap = overlap.Substring(lastSpaceIndex + 1);
        }

        return overlap;
    }

    private string? DetermineCategory(string content, List<string> availableCategories)
    {
        if (!availableCategories.Any()) return null;

        // Simple keyword-based categorization
        var contentLower = content.ToLowerInvariant();
        
        var categoryScores = new Dictionary<string, int>();
        
        foreach (var category in availableCategories)
        {
            var categoryKeywords = GetCategoryKeywords(category);
            var score = categoryKeywords.Count(keyword => contentLower.Contains(keyword.ToLowerInvariant()));
            categoryScores[category] = score;
        }

        var bestCategory = categoryScores.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
        return bestCategory.Value > 0 ? bestCategory.Key : availableCategories.FirstOrDefault();
    }

    private List<string> GetCategoryKeywords(string category)
    {
        // In production, this would be more sophisticated, possibly using ML
        return category.ToLowerInvariant() switch
        {
            "technical" => new[] { "API", "code", "programming", "software", "system", "technical", "development" }.ToList(),
            "business" => new[] { "business", "strategy", "market", "customer", "revenue", "profit", "management" }.ToList(),
            "support" => new[] { "help", "support", "issue", "problem", "troubleshoot", "fix", "error" }.ToList(),
            "legal" => new[] { "legal", "contract", "agreement", "terms", "policy", "compliance", "regulation" }.ToList(),
            "finance" => new[] { "finance", "budget", "cost", "price", "payment", "invoice", "accounting" }.ToList(),
            _ => new List<string>()
        };
    }

    private List<string> ExtractKeywords(string content)
    {
        // Simple keyword extraction - in production, use NLP libraries like spaCy or Stanford NLP
        var words = Regex.Matches(content.ToLowerInvariant(), @"\b\w{3,}\b")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(word => !IsStopWord(word))
            .GroupBy(word => word)
            .OrderByDescending(group => group.Count())
            .Take(10)
            .Select(group => group.Key)
            .ToList();

        return words;
    }

    private bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>
        {
            "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "is", "are", "was", "were",
            "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should",
            "may", "might", "must", "can", "this", "that", "these", "those", "a", "an", "as", "if", "then", "than",
            "so", "very", "just", "now", "here", "there", "where", "when", "why", "how", "all", "any", "both",
            "each", "few", "more", "most", "other", "some", "such", "only", "own", "same", "so", "than", "too",
            "very", "can", "will", "just", "don", "should", "now"
        };

        return stopWords.Contains(word);
    }

    private async Task CreateSecurityAlertAsync(
        AlertType type, 
        AlertSeverity severity, 
        string title, 
        string description, 
        Dictionary<string, object> details)
    {
        var alert = new SecurityAlert
        {
            Type = type,
            Severity = severity,
            Title = title,
            Description = description,
            Details = details
        };

        _context.SecurityAlerts.Add(alert);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Security alert created: {Type} - {Title}", type, title);
    }

    public async Task<List<KnowledgeDocument>> GetDocumentsAsync(string apiKey, int skip = 0, int take = 50)
    {
        return await _context.KnowledgeDocuments
            .Where(d => d.ApiKey == apiKey)
            .OrderByDescending(d => d.UploadedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<bool> DeleteDocumentAsync(string documentId, string apiKey)
    {
        var document = await _context.KnowledgeDocuments
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ApiKey == apiKey);

        if (document == null) return false;

        try
        {
            // Delete vectors from vector database
            foreach (var chunk in document.Chunks.Where(c => !string.IsNullOrEmpty(c.VectorId)))
            {
                await _vectorDb.DeleteVectorAsync(chunk.VectorId);
            }

            // Delete file from storage
            if (File.Exists(document.FilePath))
            {
                File.Delete(document.FilePath);
            }

            // Delete from database
            _context.DocumentChunks.RemoveRange(document.Chunks);
            _context.KnowledgeDocuments.Remove(document);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return false;
        }
    }
}

public class TextChunk
{
    public string Content { get; set; } = string.Empty;
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public string? Category { get; set; }
    public List<string> Keywords { get; set; } = new();
}