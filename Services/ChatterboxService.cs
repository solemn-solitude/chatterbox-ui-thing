using ChatterboxInference.Client;
using ChatterboxInference.Client.Models;
using Microsoft.Extensions.Options;

namespace chatterbox_ui.Services;

public class ChatterboxService : IDisposable
{
    private readonly HTTPClient _client;
    private bool _disposed = false;

    public ChatterboxService(IOptions<ChatterboxConfig> config)
    {
        var settings = config.Value;

        AppLogger.Instance.Log(
            "ChatterboxService",
            $"Initializing with ServerUrl: {settings.ServerUrl}"
        );
        AppLogger.Instance.Log(
            "ChatterboxService",
            $"API Key present: {!string.IsNullOrEmpty(settings.ApiKey)}"
        );

        _client = new HTTPClient(settings.ServerUrl, settings.ApiKey);
    }

    public async Task<List<VoiceInfo>> ListVoicesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                AppLogger.Instance.Log("ChatterboxService", "Calling ListVoices");

                var result = _client.ListVoicesTyped();

                AppLogger.Instance.Log(
                    "ChatterboxService",
                    $"Successfully retrieved {result.Total} voices"
                );

                // Convert from Client.Models.VoiceInfo to Services.VoiceInfo
                return result
                    .Voices.Select(v => new VoiceInfo
                    {
                        VoiceId = v.VoiceId,
                        CreatedAt = v.UploadedAt,
                        SampleRate = v.SampleRate,
                        FilePath = v.Filename,
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                AppLogger.Instance.LogError(
                    "ChatterboxService",
                    "Exception in ListVoicesAsync",
                    ex
                );
                throw; // Re-throw so the UI can display the error
            }
        });
    }

    public async Task<byte[]> SynthesizeAsync(
        string text,
        string voiceMode = "default",
        string? voiceName = null,
        string? voiceId = null
    )
    {
        return await Task.Run(() =>
        {
            AppLogger.Instance.Log("ChatterboxService", $"Starting synthesis: mode={voiceMode}, voiceName={voiceName}, voiceId={voiceId}");
            
            var audioData = new List<byte>();
            foreach (
                var chunk in _client.Synthesize(
                    text: text,
                    voiceMode: voiceMode,
                    voiceName: voiceName,
                    voiceId: voiceId
                    // audioFormat defaults to "wav" in HTTPClient
                )
            )
            {
                audioData.AddRange(chunk);
                AppLogger.Instance.Log("ChatterboxService", $"Received chunk: {chunk.Length} bytes, total so far: {audioData.Count} bytes");
            }
            
            AppLogger.Instance.Log("ChatterboxService", $"Synthesis complete: {audioData.Count} total bytes");
            return audioData.ToArray();
        });
    }

    public async Task<UploadResult> UploadVoiceAsync(
        string voiceId,
        string audioFilePath,
        int sampleRate
    )
    {
        return await Task.Run(() =>
        {
            try
            {
                var result = _client.UploadVoice(voiceId, audioFilePath, sampleRate);
                return new UploadResult
                {
                    Success =
                        result.TryGetValue("success", out var success) && success is bool b && b,
                    Message = result.TryGetValue("message", out var msg)
                        ? msg?.ToString() ?? ""
                        : "",
                };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = ex.Message };
            }
        });
    }

    public async Task<UploadResult> UploadVoiceFromBase64Async(
        string voiceId,
        string base64Audio,
        int sampleRate
    )
    {
        return await Task.Run(() =>
        {
            try
            {
                // Save base64 to temp WAV file
                var tempPath = Path.Combine(Path.GetTempPath(), $"{voiceId}_{Guid.NewGuid()}.wav");
                var audioBytes = Convert.FromBase64String(base64Audio);
                File.WriteAllBytes(tempPath, audioBytes);

                var result = _client.UploadVoice(voiceId, tempPath, sampleRate);

                // Clean up temp file
                try
                {
                    File.Delete(tempPath);
                }
                catch { }

                return new UploadResult
                {
                    Success =
                        result.TryGetValue("success", out var success) && success is bool b && b,
                    Message = result.TryGetValue("message", out var msg)
                        ? msg?.ToString() ?? ""
                        : "",
                };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = ex.Message };
            }
        });
    }

    public async Task<DeleteResult> DeleteVoiceAsync(string voiceId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var result = _client.DeleteVoice(voiceId);
                return new DeleteResult
                {
                    Success =
                        result.TryGetValue("success", out var success) && success is bool b && b,
                    Message = result.TryGetValue("message", out var msg)
                        ? msg?.ToString() ?? ""
                        : "",
                };
            }
            catch (Exception ex)
            {
                return new DeleteResult { Success = false, Message = ex.Message };
            }
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _client?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

public class VoiceInfo
{
    public string VoiceId { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public int SampleRate { get; set; }
    public string FilePath { get; set; } = string.Empty;
}

public class UploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class DeleteResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
