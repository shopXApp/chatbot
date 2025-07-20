using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using XlncChatBackend.Configuration;
using XlncChatBackend.Services.Interfaces;

namespace XlncChatBackend.Services;

public class VectorDatabaseService : IVectorDatabaseService
{
    private readonly QdrantClient _client;
    private readonly VectorDatabaseOptions _options;
    private readonly ILogger<VectorDatabaseService> _logger;
    private const string CollectionName = "knowledge_base";

    public VectorDatabaseService(
        IOptions<VectorDatabaseOptions> options,
        ILogger<VectorDatabaseService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        _client = new QdrantClient(
            host: _options.Host,
            port: _options.Port,
            https: _options.UseHttps,
            apiKey: _options.ApiKey
        );
        
        // Initialize collection if it doesn't exist
        _ = Task.Run(InitializeCollectionAsync);
    }

    public async Task<string> StoreVectorAsync(
        float[] embedding, 
        string content, 
        Dictionary<string, object> metadata)
    {
        try
        {
            var pointId = Guid.NewGuid().ToString();
            
            // Prepare payload with metadata and content
            var payload = new Dictionary<string, Value>
            {
                ["content"] = content,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            // Add metadata to payload
            foreach (var kvp in metadata)
            {
                payload[kvp.Key] = ConvertToValue(kvp.Value);
            }

            var point = new PointStruct
            {
                Id = pointId,
                Vectors = embedding,
                Payload = { payload }
            };

            var upsertPoints = new UpsertPoints
            {
                CollectionName = CollectionName,
                Points = { point }
            };

            var response = await _client.UpsertAsync(upsertPoints);
            
            if (response.Status == UpdateStatus.Completed)
            {
                _logger.LogDebug("Successfully stored vector {PointId}", pointId);
                return pointId;
            }
            else
            {
                throw new InvalidOperationException($"Failed to store vector: {response.Status}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing vector in database");
            throw;
        }
    }

    public async Task<List<VectorSearchResult>> SearchSimilarAsync(
        float[] queryEmbedding, 
        int limit = 10, 
        double threshold = 0.7,
        Dictionary<string, object>? filters = null)
    {
        try
        {
            var searchPoints = new SearchPoints
            {
                CollectionName = CollectionName,
                Vector = queryEmbedding,
                Limit = (uint)limit,
                ScoreThreshold = (float)threshold,
                WithPayload = true,
                WithVectors = false
            };

            // Apply filters if provided
            if (filters != null && filters.Any())
            {
                searchPoints.Filter = CreateFilter(filters);
            }

            var response = await _client.SearchAsync(searchPoints);
            
            var results = new List<VectorSearchResult>();
            
            foreach (var scoredPoint in response)
            {
                var metadata = new Dictionary<string, object>();
                
                foreach (var kvp in scoredPoint.Payload)
                {
                    metadata[kvp.Key] = ConvertFromValue(kvp.Value);
                }

                results.Add(new VectorSearchResult
                {
                    Id = scoredPoint.Id.ToString(),
                    Content = metadata.GetValueOrDefault("content")?.ToString() ?? string.Empty,
                    Score = scoredPoint.Score,
                    Metadata = metadata
                });
            }

            _logger.LogDebug("Found {ResultCount} similar vectors", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vectors");
            throw;
        }
    }

    public async Task<bool> DeleteVectorAsync(string vectorId)
    {
        try
        {
            var deletePoints = new DeletePoints
            {
                CollectionName = CollectionName,
                Points = new PointsSelector
                {
                    Points = new PointsIdsList
                    {
                        Ids = { vectorId }
                    }
                }
            };

            var response = await _client.DeleteAsync(deletePoints);
            
            var success = response.Status == UpdateStatus.Completed;
            
            if (success)
            {
                _logger.LogDebug("Successfully deleted vector {VectorId}", vectorId);
            }
            else
            {
                _logger.LogWarning("Failed to delete vector {VectorId}: {Status}", vectorId, response.Status);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vector {VectorId}", vectorId);
            return false;
        }
    }

    public async Task<bool> DeleteVectorsByFilterAsync(Dictionary<string, object> filters)
    {
        try
        {
            var deletePoints = new DeletePoints
            {
                CollectionName = CollectionName,
                Points = new PointsSelector
                {
                    Filter = CreateFilter(filters)
                }
            };

            var response = await _client.DeleteAsync(deletePoints);
            
            var success = response.Status == UpdateStatus.Completed;
            
            if (success)
            {
                _logger.LogDebug("Successfully deleted vectors with filters");
            }
            else
            {
                _logger.LogWarning("Failed to delete vectors with filters: {Status}", response.Status);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vectors with filters");
            return false;
        }
    }

    public async Task<VectorSearchResult?> GetVectorAsync(string vectorId)
    {
        try
        {
            var getPoints = new GetPoints
            {
                CollectionName = CollectionName,
                Ids = { vectorId },
                WithPayload = true,
                WithVectors = false
            };

            var response = await _client.GetAsync(getPoints);
            
            if (response.Any())
            {
                var point = response.First();
                var metadata = new Dictionary<string, object>();
                
                foreach (var kvp in point.Payload)
                {
                    metadata[kvp.Key] = ConvertFromValue(kvp.Value);
                }

                return new VectorSearchResult
                {
                    Id = point.Id.ToString(),
                    Content = metadata.GetValueOrDefault("content")?.ToString() ?? string.Empty,
                    Score = 1.0, // Perfect match since we're getting by ID
                    Metadata = metadata
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vector {VectorId}", vectorId);
            return null;
        }
    }

    public async Task<long> GetCollectionSizeAsync()
    {
        try
        {
            var collectionInfo = await _client.GetCollectionInfoAsync(CollectionName);
            return (long)collectionInfo.VectorsCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection size");
            return 0;
        }
    }

    public async Task<bool> UpdateVectorMetadataAsync(string vectorId, Dictionary<string, object> metadata)
    {
        try
        {
            // Get existing point
            var existingPoint = await GetVectorAsync(vectorId);
            if (existingPoint == null)
            {
                return false;
            }

            // Merge metadata
            var updatedPayload = new Dictionary<string, Value>();
            
            // Keep existing metadata
            foreach (var kvp in existingPoint.Metadata)
            {
                updatedPayload[kvp.Key] = ConvertToValue(kvp.Value);
            }
            
            // Update with new metadata
            foreach (var kvp in metadata)
            {
                updatedPayload[kvp.Key] = ConvertToValue(kvp.Value);
            }

            var setPayload = new SetPayload
            {
                CollectionName = CollectionName,
                Payload = { updatedPayload },
                Points = new PointsSelector
                {
                    Points = new PointsIdsList
                    {
                        Ids = { vectorId }
                    }
                }
            };

            var response = await _client.SetPayloadAsync(setPayload);
            
            var success = response.Status == UpdateStatus.Completed;
            
            if (success)
            {
                _logger.LogDebug("Successfully updated metadata for vector {VectorId}", vectorId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vector metadata {VectorId}", vectorId);
            return false;
        }
    }

    public async Task<List<string>> GetVectorsByDocumentIdAsync(string documentId)
    {
        try
        {
            var searchPoints = new ScrollPoints
            {
                CollectionName = CollectionName,
                Filter = CreateFilter(new Dictionary<string, object> { ["documentId"] = documentId }),
                WithPayload = false,
                WithVectors = false,
                Limit = 1000 // Adjust based on expected document size
            };

            var response = await _client.ScrollAsync(searchPoints);
            
            return response.Select(point => point.Id.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vectors for document {DocumentId}", documentId);
            return new List<string>();
        }
    }

    private async Task InitializeCollectionAsync()
    {
        try
        {
            // Check if collection exists
            var collections = await _client.ListCollectionsAsync();
            var collectionExists = collections.Any(c => c.Name == CollectionName);

            if (!collectionExists)
            {
                _logger.LogInformation("Creating Qdrant collection: {CollectionName}", CollectionName);
                
                var createCollection = new CreateCollection
                {
                    CollectionName = CollectionName,
                    VectorsConfig = new VectorsConfig
                    {
                        Size = (uint)_options.VectorSize,
                        Distance = Distance.Cosine
                    },
                    OptimizersConfig = new OptimizersConfigDiff
                    {
                        DefaultSegmentNumber = 2
                    },
                    ReplicationFactor = 1
                };

                await _client.CreateCollectionAsync(createCollection);
                
                // Create indexes for better performance
                await CreateIndexesAsync();
                
                _logger.LogInformation("Successfully created collection: {CollectionName}", CollectionName);
            }
            else
            {
                _logger.LogDebug("Collection {CollectionName} already exists", CollectionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Qdrant collection");
            throw;
        }
    }

    private async Task CreateIndexesAsync()
    {
        try
        {
            // Create index for documentId field
            var createIndex = new CreateFieldIndexCollection
            {
                CollectionName = CollectionName,
                FieldName = "documentId",
                FieldType = FieldType.Keyword
            };

            await _client.CreateFieldIndexAsync(createIndex);

            // Create index for category field
            var createCategoryIndex = new CreateFieldIndexCollection
            {
                CollectionName = CollectionName,
                FieldName = "category",
                FieldType = FieldType.Keyword
            };

            await _client.CreateFieldIndexAsync(createCategoryIndex);

            _logger.LogDebug("Successfully created field indexes");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creating field indexes (this is not critical)");
        }
    }

    private Filter CreateFilter(Dictionary<string, object> filters)
    {
        var conditions = new List<Condition>();

        foreach (var kvp in filters)
        {
            var condition = new Condition
            {
                Field = new FieldCondition
                {
                    Key = kvp.Key,
                    Match = new Match
                    {
                        Value = ConvertToValue(kvp.Value)
                    }
                }
            };
            conditions.Add(condition);
        }

        return new Filter
        {
            Must = { conditions }
        };
    }

    private Value ConvertToValue(object obj)
    {
        return obj switch
        {
            string s => new Value { StringValue = s },
            int i => new Value { IntegerValue = i },
            long l => new Value { IntegerValue = l },
            double d => new Value { DoubleValue = d },
            float f => new Value { DoubleValue = f },
            bool b => new Value { BoolValue = b },
            DateTime dt => new Value { StringValue = dt.ToString("O") },
            List<string> list => new Value 
            { 
                ListValue = new ListValue 
                { 
                    Values = { list.Select(s => new Value { StringValue = s }) } 
                } 
            },
            _ => new Value { StringValue = obj.ToString() ?? string.Empty }
        };
    }

    private object ConvertFromValue(Value value)
    {
        return value.KindCase switch
        {
            Value.KindOneofCase.StringValue => value.StringValue,
            Value.KindOneofCase.IntegerValue => value.IntegerValue,
            Value.KindOneofCase.DoubleValue => value.DoubleValue,
            Value.KindOneofCase.BoolValue => value.BoolValue,
            Value.KindOneofCase.ListValue => value.ListValue.Values.Select(ConvertFromValue).ToList(),
            _ => value.ToString()
        };
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

public class VectorSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}