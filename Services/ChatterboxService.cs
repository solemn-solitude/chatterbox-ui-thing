using ChatterboxInference.Client;
using Microsoft.Extensions.Options;

namespace chatterbox_ui.Services;

public class ChatterboxService : IDisposable
{
    private readonly HTTPClient _client;
    private bool _disposed = false;

    public ChatterboxService(IOptions<ChatterboxConfig> config)
    {
        var settings = config.Value;
        _client = new HTTPClient(settings.ServerUrl, settings.ApiKey);
    }

    public async Task<List<VoiceInfo>> ListVoicesAsync()
    {
        return await Task.Run(() =>
        {
            var result = _client.ListVoices();
            if (result.TryGetValue("voices", out var voicesObj) && voicesObj is List<object> voicesList)
            {
                return voicesList.Select(v =>
                {
                    if (v is Dictionary<string, object> voiceDict)
                    {
                        return new VoiceInfo
                        {
                            VoiceId = voiceDict.TryGetValue("voice_id", out var id) ? id?.ToString() ?? "" : "",
                            CreatedAt = voiceDict.TryGetValue("created_at", out var created) ? created?.ToString() ?? "" : "",
                            SampleRate = voiceDict.TryGetValue("sample_rate", out var sr) && int.TryParse(sr?.ToString(), out var sampleRate) ? sampleRate : 0,
                            FilePath = voiceDict.TryGetValue("file_path", out var fp) ? fp?.ToString() ?? "" : ""
                        };
                    }
                    return new VoiceInfo();
                }).ToList();
            }
            return new List<VoiceInfo>();
        });
    }

    public async Task<byte[]> SynthesizeAsync(string text, string voiceMode = "default", string? voiceName = null, string? voiceId = null)
    {
        return await Task.Run(() =>
        {
            var audioData = new List<byte>();
            foreach (var chunk in _client.Synthesize(
                text: text,
                voiceMode: voiceMode,
                voiceName: voiceName,
                voiceId: voiceId,
                audioFormat: "pcm"))
            {
                audioData.AddRange(chunk);
            }
            return audioData.ToArray();
        });
    }

    public async Task<UploadResult> UploadVoiceAsync(string voiceId, string audioFilePath, int sampleRate)
    {
        return await Task.Run(() =>
        {
            try
            {
                var result = _client.UploadVoice(voiceId, audioFilePath, sampleRate);
                return new UploadResult
                {
                    Success = result.TryGetValue("success", out var success) && success is bool b && b,
                    Message = result.TryGetValue("message", out var msg) ? msg?.ToString() ?? "" : ""
                };
            }
            catch (Exception ex)
            {
                return new UploadResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        });
    }

    public async Task<UploadResult> UploadVoiceFromBase64Async(string voiceId, string base64Audio, int sampleRate)
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
                try { File.Delete(tempPath); } catch { }

                return new UploadResult
                {
                    Success = result.TryGetValue("success", out var success) && success is bool b && b,
                    Message = result.TryGetValue("message", out var msg) ? msg?.ToString() ?? "" : ""
                };
            }
            catch (Exception ex)
            {
                return new UploadResult
                {
                    Success = false,
                    Message = ex.Message
                };
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
                    Success = result.TryGetValue("success", out var success) && success is bool b && b,
                    Message = result.TryGetValue("message", out var msg) ? msg?.ToString() ?? "" : ""
                };
            }
            catch (Exception ex)
            {
                return new DeleteResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
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
