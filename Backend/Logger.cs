using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace HyPrism.Backend;

public enum AppLogLevel
{
    Debug = 0,
    Info = 1,
    Success = 2,
    Warning = 3,
    Error = 4,
    None = 5  // Completely silent
}

public static class Logger
{
    private static readonly object _lock = new();
    private static readonly Queue<string> _logBuffer = new();
    private const int MaxLogEntries = 100;
    
    // File logging
    private static readonly string _logsFolder;
    private static readonly string _latestLogPath;
    private static StreamWriter? _latestLogWriter;
    private static StreamWriter? _dateLogWriter;
    private static string? _currentDateLogPath;
    private static bool _fileLoggingInitialized = false;
    
    // Default: only show Success, Warning, and Error messages
    public static AppLogLevel MinimumLevel { get; set; } = AppLogLevel.Success;
    
    static Logger()
    {
        // Get logs folder path based on platform
        string basePath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                "Library", "Application Support", "HyPrism");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HyPrism");
        }
        else
        {
            // Linux
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hyprism");
        }
        
        _logsFolder = Path.Combine(basePath, "logs");
        _latestLogPath = Path.Combine(_logsFolder, "hyprism-latest.log");
        
        InitializeFileLogging();
    }
    
    private static void InitializeFileLogging()
    {
        try
        {
            // Create logs folder if needed
            if (!Directory.Exists(_logsFolder))
            {
                Directory.CreateDirectory(_logsFolder);
            }
            
            // Clear and open latest log (fresh start on each launch)
            _latestLogWriter = new StreamWriter(_latestLogPath, append: false) { AutoFlush = true };
            
            // Write header to latest log
            _latestLogWriter.WriteLine($"=== HyPrism Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            _latestLogWriter.WriteLine();
            
            // Open date-based log (append mode)
            OpenDateLog();
            
            _fileLoggingInitialized = true;
        }
        catch
        {
            // If file logging fails, continue with console only
            _fileLoggingInitialized = false;
        }
    }
    
    private static void OpenDateLog()
    {
        var today = DateTime.Now;
        var dateLogFileName = $"hyprism-{today.Month}-{today.Day}-{today.Year}.log";
        var dateLogPath = Path.Combine(_logsFolder, dateLogFileName);
        
        // Only open new file if date changed
        if (_currentDateLogPath != dateLogPath)
        {
            _dateLogWriter?.Dispose();
            _dateLogWriter = new StreamWriter(dateLogPath, append: true) { AutoFlush = true };
            _currentDateLogPath = dateLogPath;
            
            // Write session separator
            _dateLogWriter.WriteLine();
            _dateLogWriter.WriteLine($"=== Session Started at {DateTime.Now:HH:mm:ss} ===");
        }
    }
    
    private static void WriteToFiles(string logEntry)
    {
        if (!_fileLoggingInitialized) return;
        
        try
        {
            // Check if we need to roll to a new date log
            var today = DateTime.Now;
            var expectedPath = Path.Combine(_logsFolder, $"hyprism-{today.Month}-{today.Day}-{today.Year}.log");
            if (_currentDateLogPath != expectedPath)
            {
                OpenDateLog();
            }
            
            // Write to both log files
            _latestLogWriter?.WriteLine(logEntry);
            _dateLogWriter?.WriteLine(logEntry);
        }
        catch
        {
            // Silently ignore file write errors
        }
    }
    
    public static void Info(string category, string message)
    {
        if (MinimumLevel <= AppLogLevel.Info)
            WriteLog("INFO", category, message, ConsoleColor.White);
        else
            BufferOnly("INFO", category, message);
    }
    
    public static void Success(string category, string message)
    {
        if (MinimumLevel <= AppLogLevel.Success)
            WriteLog("OK", category, message, ConsoleColor.Green);
        else
            BufferOnly("OK", category, message);
    }
    
    public static void Warning(string category, string message)
    {
        if (MinimumLevel <= AppLogLevel.Warning)
            WriteLog("WARN", category, message, ConsoleColor.Yellow);
        else
            BufferOnly("WARN", category, message);
    }
    
    public static void Error(string category, string message)
    {
        if (MinimumLevel <= AppLogLevel.Error)
            WriteLog("ERR", category, message, ConsoleColor.Red);
        else
            BufferOnly("ERR", category, message);
    }
    
    public static void Debug(string category, string message)
    {
#if DEBUG
        if (MinimumLevel <= AppLogLevel.Debug)
            WriteLog("DBG", category, message, ConsoleColor.Gray);
        else
            BufferOnly("DBG", category, message);
#endif
    }
    
    // Add to buffer without printing (for log retrieval later)
    private static void BufferOnly(string level, string category, string message)
    {
        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"{timestamp} | {level} | {category} | {message}";
            _logBuffer.Enqueue(logEntry);
            while (_logBuffer.Count > MaxLogEntries)
            {
                _logBuffer.Dequeue();
            }
            
            // Still write to log files even if not printing to console
            WriteToFiles(logEntry);
        }
    }
    
    public static List<string> GetRecentLogs(int count = 10)
    {
        lock (_lock)
        {
            var entries = _logBuffer.ToArray();
            var start = Math.Max(0, entries.Length - count);
            var result = new List<string>();
            for (int i = start; i < entries.Length; i++)
            {
                result.Add(entries[i]);
            }
            return result;
        }
    }
    
    public static void Progress(string category, int percent, string message)
    {
        lock (_lock)
        {
            Console.Write($"\r[{category}] {message.PadRight(40)} [{ProgressBar(percent, 20)}] {percent,3}%");
            if (percent >= 100)
            {
                Console.WriteLine();
            }
        }
    }
    
    private static void WriteLog(string level, string category, string message, ConsoleColor color)
    {
        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var icon = level switch
            {
                "OK" => "✔",
                "WARN" => "⚠",
                "ERR" => "✖",
                _ => "•"
            };

            var logEntry = $"{timestamp} | {level} | {category} | {message}";
            
            // Add to buffer
            _logBuffer.Enqueue(logEntry);
            while (_logBuffer.Count > MaxLogEntries)
            {
                _logBuffer.Dequeue();
            }
            
            // Write to log files
            WriteToFiles(logEntry);
            
            var originalColor = Console.ForegroundColor;
            
            Console.Write($"{timestamp}  ");
            Console.ForegroundColor = color;
            Console.Write($"{icon} {level}");
            Console.ForegroundColor = originalColor;
            Console.WriteLine($"  {category}: {message}");
        }
    }
    
    private static string ProgressBar(int percent, int width)
    {
        int filled = (int)((percent / 100.0) * width);
        int empty = width - filled;
        return new string('=', filled) + new string('-', empty);
    }
}
