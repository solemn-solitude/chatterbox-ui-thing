using System.Collections.Concurrent;

namespace chatterbox_ui.Services;

/// <summary>
/// Simple file-based logger for debugging
/// </summary>
public class AppLogger : IDisposable
{
    private static readonly Lazy<AppLogger> _instance = new Lazy<AppLogger>(() => new AppLogger());
    private readonly string _logFilePath;
    private readonly ConcurrentQueue<string> _logQueue = new();
    private readonly Timer _flushTimer;
    private readonly object _fileLock = new object();
    private bool _disposed = false;

    public static AppLogger Instance => _instance.Value;

    private AppLogger()
    {
        // Log to the main chatterbox-ui directory
        var logDir = Directory.GetCurrentDirectory();
        _logFilePath = Path.Combine(logDir, "chatterbox-ui.log");

        // Create or append to log file
        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(_logFilePath, $"\n\n========== New Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========\n\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize logger: {ex.Message}");
        }

        // Flush logs every 2 seconds
        _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public void Log(string category, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] [{category}] {message}";
        
        _logQueue.Enqueue(logEntry);
        Console.WriteLine(logEntry); // Also log to console
    }

    public void LogError(string category, string message, Exception? ex = null)
    {
        var errorMessage = ex != null 
            ? $"{message}\nException: {ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}"
            : message;
        
        Log($"ERROR-{category}", errorMessage);
    }

    public void LogRequest(string method, string url, string? body = null)
    {
        var message = $"{method} {url}";
        if (!string.IsNullOrEmpty(body))
        {
            message += $"\nBody: {body}";
        }
        Log("HTTP-REQUEST", message);
    }

    public void LogResponse(string url, int statusCode, string? body = null)
    {
        var message = $"{url} returned {statusCode}";
        if (!string.IsNullOrEmpty(body) && body.Length <= 500)
        {
            message += $"\nResponse: {body}";
        }
        else if (!string.IsNullOrEmpty(body))
        {
            message += $"\nResponse: {body.Substring(0, 500)}... (truncated, total length: {body.Length})";
        }
        Log("HTTP-RESPONSE", message);
    }

    private void FlushLogs(object? state)
    {
        if (_logQueue.IsEmpty || _disposed)
            return;

        try
        {
            lock (_fileLock)
            {
                var entries = new List<string>();
                while (_logQueue.TryDequeue(out var entry))
                {
                    entries.Add(entry);
                }

                if (entries.Count > 0)
                {
                    File.AppendAllLines(_logFilePath, entries);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to flush logs: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _flushTimer?.Dispose();
        
        // Final flush
        FlushLogs(null);
        
        GC.SuppressFinalize(this);
    }
}
