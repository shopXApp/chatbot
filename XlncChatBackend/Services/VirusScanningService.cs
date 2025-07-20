using Microsoft.Extensions.Options;
using System.Net.Sockets;
using System.Text;
using XlncChatBackend.Configuration;
using XlncChatBackend.Services.Interfaces;

namespace XlncChatBackend.Services;

public class VirusScanningService : IVirusScanningService
{
    private readonly VirusScanningOptions _options;
    private readonly ILogger<VirusScanningService> _logger;
    private readonly ISecurityService _securityService;

    public VirusScanningService(
        IOptions<VirusScanningOptions> options,
        ILogger<VirusScanningService> logger,
        ISecurityService securityService)
    {
        _options = options.Value;
        _logger = logger;
        _securityService = securityService;
    }

    public async Task<VirusScanResult> ScanFileAsync(string filePath)
    {
        if (!_options.EnableScanning)
        {
            _logger.LogDebug("Virus scanning is disabled, marking file as clean");
            return new VirusScanResult { IsClean = true };
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        try
        {
            _logger.LogInformation("Starting virus scan for file: {FilePath}", filePath);

            // Check if ClamAV service is available
            if (!await IsServiceAvailableAsync())
            {
                _logger.LogWarning("ClamAV service is not available, skipping scan");
                return new VirusScanResult 
                { 
                    IsClean = true, 
                    Details = "ClamAV service unavailable - scan skipped" 
                };
            }

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return await ScanStreamAsync(fileStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning file: {FilePath}", filePath);
            
            // Alert on scan failures
            await _securityService.CreateAlertAsync(
                Models.AlertType.SystemError,
                Models.AlertSeverity.Medium,
                "Virus scan failed",
                $"Failed to scan file {Path.GetFileName(filePath)}: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["filePath"] = filePath,
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                });

            // In case of scan failure, treat as potentially infected for safety
            return new VirusScanResult 
            { 
                IsClean = false, 
                ThreatName = "SCAN_FAILED",
                Details = $"Scan failed: {ex.Message}"
            };
        }
    }

    public async Task<VirusScanResult> ScanStreamAsync(Stream stream)
    {
        if (!_options.EnableScanning)
        {
            return new VirusScanResult { IsClean = true };
        }

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_options.ClamAVHost, _options.ClamAVPort);
            client.ReceiveTimeout = _options.TimeoutSeconds * 1000;
            client.SendTimeout = _options.TimeoutSeconds * 1000;

            using var networkStream = client.GetStream();

            // Send INSTREAM command
            var instreamCommand = Encoding.ASCII.GetBytes("zINSTREAM\0");
            await networkStream.WriteAsync(instreamCommand);

            // Send file data in chunks
            const int chunkSize = 2048;
            var buffer = new byte[chunkSize];
            int bytesRead;

            stream.Position = 0;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, chunkSize)) > 0)
            {
                // Send chunk size (4 bytes, network byte order)
                var chunkSizeBytes = BitConverter.GetBytes((uint)bytesRead);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(chunkSizeBytes);
                }
                await networkStream.WriteAsync(chunkSizeBytes);

                // Send chunk data
                await networkStream.WriteAsync(buffer, 0, bytesRead);
            }

            // Send zero-length chunk to indicate end
            var endChunk = new byte[4] { 0, 0, 0, 0 };
            await networkStream.WriteAsync(endChunk);

            // Read response
            var responseBuffer = new byte[512];
            var responseLength = await networkStream.ReadAsync(responseBuffer);
            var response = Encoding.ASCII.GetString(responseBuffer, 0, responseLength).Trim();

            _logger.LogDebug("ClamAV response: {Response}", response);

            var result = ParseClamAVResponse(response);

            if (!result.IsClean)
            {
                _logger.LogWarning("Virus detected: {ThreatName}", result.ThreatName);
                
                // Create security alert for virus detection
                await _securityService.CreateAlertAsync(
                    Models.AlertType.VirusDetected,
                    Models.AlertSeverity.High,
                    "Virus detected in uploaded file",
                    $"ClamAV detected threat: {result.ThreatName}",
                    new Dictionary<string, object>
                    {
                        ["threatName"] = result.ThreatName ?? "Unknown",
                        ["scanTime"] = result.ScanTime,
                        ["fileSize"] = stream.Length
                    });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during stream virus scan");
            throw;
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(_options.ClamAVHost, _options.ClamAVPort);
            
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) == connectTask)
            {
                using var networkStream = client.GetStream();
                
                // Send PING command
                var pingCommand = Encoding.ASCII.GetBytes("zPING\0");
                await networkStream.WriteAsync(pingCommand);

                // Read response
                var buffer = new byte[256];
                var responseLength = await networkStream.ReadAsync(buffer);
                var response = Encoding.ASCII.GetString(buffer, 0, responseLength).Trim();

                return response.Contains("PONG");
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ClamAV service availability check failed");
            return false;
        }
    }

    private VirusScanResult ParseClamAVResponse(string response)
    {
        var result = new VirusScanResult();

        if (string.IsNullOrWhiteSpace(response))
        {
            result.IsClean = false;
            result.ThreatName = "EMPTY_RESPONSE";
            result.Details = "Empty response from ClamAV";
            return result;
        }

        // Standard ClamAV responses:
        // "stream: OK" - Clean file
        // "stream: Win.Trojan.Agent-1234567 FOUND" - Virus found
        // "stream: ERROR" - Scan error

        if (response.EndsWith("OK"))
        {
            result.IsClean = true;
            result.Details = "File is clean";
        }
        else if (response.Contains("FOUND"))
        {
            result.IsClean = false;
            
            // Extract threat name
            var parts = response.Split(':');
            if (parts.Length > 1)
            {
                var threatPart = parts[1].Trim();
                var foundIndex = threatPart.LastIndexOf("FOUND");
                if (foundIndex > 0)
                {
                    result.ThreatName = threatPart.Substring(0, foundIndex).Trim();
                }
            }
            
            if (string.IsNullOrEmpty(result.ThreatName))
            {
                result.ThreatName = "UNKNOWN_THREAT";
            }

            result.Details = $"Threat detected: {result.ThreatName}";
        }
        else if (response.Contains("ERROR"))
        {
            result.IsClean = false;
            result.ThreatName = "SCAN_ERROR";
            result.Details = "ClamAV scan error: " + response;
        }
        else
        {
            // Unexpected response - treat as potentially dangerous
            result.IsClean = false;
            result.ThreatName = "UNEXPECTED_RESPONSE";
            result.Details = "Unexpected ClamAV response: " + response;
        }

        return result;
    }

    public async Task<bool> QuarantineFileAsync(string filePath, string threatName)
    {
        if (!_options.QuarantineInfectedFiles)
        {
            return false;
        }

        try
        {
            var quarantineDir = Path.Combine(_options.QuarantineDirectory, DateTime.UtcNow.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(quarantineDir);

            var fileName = Path.GetFileName(filePath);
            var quarantineFileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{threatName}_{fileName}";
            var quarantinePath = Path.Combine(quarantineDir, quarantineFileName);

            // Move file to quarantine
            File.Move(filePath, quarantinePath);

            // Create metadata file
            var metadataPath = quarantinePath + ".metadata";
            var metadata = new
            {
                OriginalPath = filePath,
                ThreatName = threatName,
                QuarantineTime = DateTime.UtcNow,
                FileSize = new FileInfo(quarantinePath).Length
            };

            await File.WriteAllTextAsync(metadataPath, System.Text.Json.JsonSerializer.Serialize(metadata));

            _logger.LogWarning("File quarantined: {OriginalPath} -> {QuarantinePath}", filePath, quarantinePath);

            // Alert about quarantine action
            await _securityService.CreateAlertAsync(
                Models.AlertType.VirusDetected,
                Models.AlertSeverity.High,
                "Infected file quarantined",
                $"File with threat '{threatName}' has been moved to quarantine",
                new Dictionary<string, object>
                {
                    ["originalPath"] = filePath,
                    ["quarantinePath"] = quarantinePath,
                    ["threatName"] = threatName
                });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to quarantine file: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<List<QuarantinedFile>> GetQuarantinedFilesAsync()
    {
        var files = new List<QuarantinedFile>();

        if (!Directory.Exists(_options.QuarantineDirectory))
        {
            return files;
        }

        try
        {
            var metadataFiles = Directory.GetFiles(_options.QuarantineDirectory, "*.metadata", SearchOption.AllDirectories);

            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataFile);
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                    
                    files.Add(new QuarantinedFile
                    {
                        QuarantinePath = metadataFile.Replace(".metadata", ""),
                        OriginalPath = metadata.GetProperty("OriginalPath").GetString() ?? "",
                        ThreatName = metadata.GetProperty("ThreatName").GetString() ?? "",
                        QuarantineTime = metadata.GetProperty("QuarantineTime").GetDateTime(),
                        FileSize = metadata.GetProperty("FileSize").GetInt64()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse quarantine metadata: {MetadataFile}", metadataFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quarantined files");
        }

        return files.OrderByDescending(f => f.QuarantineTime).ToList();
    }

    public async Task<bool> RestoreFileAsync(string quarantinePath, string restorePath)
    {
        try
        {
            if (!File.Exists(quarantinePath))
            {
                return false;
            }

            var restoreDir = Path.GetDirectoryName(restorePath);
            if (!string.IsNullOrEmpty(restoreDir))
            {
                Directory.CreateDirectory(restoreDir);
            }

            File.Move(quarantinePath, restorePath);

            // Remove metadata file
            var metadataPath = quarantinePath + ".metadata";
            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }

            _logger.LogInformation("File restored from quarantine: {QuarantinePath} -> {RestorePath}", 
                quarantinePath, restorePath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore file from quarantine: {QuarantinePath}", quarantinePath);
            return false;
        }
    }

    public async Task<bool> DeleteQuarantinedFileAsync(string quarantinePath)
    {
        try
        {
            if (File.Exists(quarantinePath))
            {
                File.Delete(quarantinePath);
            }

            var metadataPath = quarantinePath + ".metadata";
            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }

            _logger.LogInformation("Quarantined file deleted: {QuarantinePath}", quarantinePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete quarantined file: {QuarantinePath}", quarantinePath);
            return false;
        }
    }
}

public class QuarantinedFile
{
    public string QuarantinePath { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public string ThreatName { get; set; } = string.Empty;
    public DateTime QuarantineTime { get; set; }
    public long FileSize { get; set; }
}