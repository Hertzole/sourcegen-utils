using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;

namespace Hertzole.SourceGenUtils;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers",
    Justification = "This is only used in debug builds.")]
[ExcludeFromCodeCoverage]
internal static class Log
{
#if DEBUG
    private static bool isInitialized;

    private static readonly string path =
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), Assembly.GetCallingAssembly().GetName().Name + ".log"));
#endif

    [Conditional("DEBUG")]
    public static void Info(object message)
    {
#if DEBUG
        Write($"[INFO] {message}");
#endif
    }

    [Conditional("DEBUG")]
    public static void Info<T>(object message)
    {
#if DEBUG
        Write($"[INFO] <{typeof(T).Name}> {message}");
#endif
    }

    [Conditional("DEBUG")]
    public static void Warning(object message)
    {
#if DEBUG
        Write($"[WARNING] {message}");
#endif
    }

    [Conditional("DEBUG")]
    public static void Warning<T>(object message)
    {
#if DEBUG
        Write($"[WARNING] <{typeof(T).Name}> {message}");
#endif
    }

    [Conditional("DEBUG")]
    public static void Error(object message)
    {
#if DEBUG
        Write($"[ERROR] {message}");
#endif
    }

    [Conditional("DEBUG")]
    public static void Error<T>(object message)
    {
#if DEBUG
        Write($"[ERROR] <{typeof(T).Name}> {message}");
#endif
    }

#if DEBUG
    private static void Write(string value)
    {
        if (!isInitialized)
        {
            isInitialized = true;
            File.WriteAllText(path, string.Empty);
        }

        using (FileStream stream = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            byte[] bytes = Encoding.UTF8.GetBytes($"[{DateTimeOffset.Now:HH:mm:ss.fff}] {value}{Environment.NewLine}");
            stream.Write(bytes, 0, bytes.Length);
        }
    }
#endif
}