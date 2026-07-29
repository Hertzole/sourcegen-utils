using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private const string LOG_NAME = NAMESPACE + ".Log";

    private static TypeSource CreateLog()
    {
        string[] deps = [LOG_NAME + ".Write(string)"];

        return new TypeSource
        {
            Signature = "internal static partial class Log",
            Attributes =
            [
                "global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"MicrosoftCodeAnalysisCorrectness\", \"RS1035:Do not use APIs banned for analyzers\", Justification = \"This is only used in debug builds.\")"
            ],
            Trivia = new TriviaSource
            {
                Summary = "Provides debug-only logging that writes to a log file."
            },
            Fields = new Dictionary<string, FieldSource>
            {
                ["isInitialized"] = new FieldSource
                {
                    Signature = "private static bool isInitialized = false;",
                    RequiredDependencies = [LOG_NAME + ".Write(string)"],
                    ConditionalPreprocessorSymbol = "DEBUG"
                },
                ["path"] = new FieldSource
                {
                    Signature = "private static readonly string path = " +
                                "global::System.IO.Path.GetFullPath(global::System.IO.Path.Combine(global::System.IO.Directory.GetCurrentDirectory(), " +
                                "global::System.Reflection.Assembly.GetCallingAssembly().GetName().Name + \".log\"));",
                    RequiredDependencies = [LOG_NAME + ".Write(string)"],
                    ConditionalPreprocessorSymbol = "DEBUG"
                }
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "Info",
                    Signature = "public static partial void Info(object message)",
                    Implementation = (writer, in _) => { Write(writer, "INFO"); },
                    Dependencies = deps,
                    Attributes = ["global::System.Diagnostics.Conditional(\"DEBUG\")"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Logs an info message with an 'INFO' prefix.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["message"] = "The message to log."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "Warning",
                    Signature = "public static partial void Warning(object message)",
                    Implementation = (writer, in _) => { Write(writer, "WARNING"); },
                    Dependencies = deps,
                    Attributes = ["global::System.Diagnostics.Conditional(\"DEBUG\")"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Logs a warning message with a 'WARNING' prefix.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["message"] = "The message to log."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "Error",
                    Signature = "public static partial void Error(object message)",
                    Implementation = (writer, in _) => { Write(writer, "ERROR"); },
                    Dependencies = deps,
                    Attributes = ["global::System.Diagnostics.Conditional(\"DEBUG\")"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Logs an error message with an 'ERROR' prefix.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["message"] = "The message to log."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "ClearLogs",
                    Signature = "public static partial void ClearLogs()",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendPreprocessorSymbol("#if DEBUG");
                        writer.AppendLine("global::System.IO.File.WriteAllText(path, string.Empty);");
                        writer.AppendPreprocessorSymbol("#endif");
                    },
                    Attributes = ["global::System.Diagnostics.Conditional(\"DEBUG\")"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Clears the log file."
                    }
                },
                new MethodSource
                {
                    Name = "Write",
                    Signature = "private static void Write(string message)",
                    ConditionalPreprocessorSymbol = "DEBUG",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (!isInitialized)");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("isInitialized = true;");
                            writer.AppendLine("global::System.IO.File.WriteAllText(path, string.Empty);");
                        }

                        writer.AppendLine(
                            "using (global::System.IO.FileStream stream = global::System.IO.File.Open(path, global::System.IO.FileMode.Append, global::System.IO.FileAccess.Write, global::System.IO.FileShare.Read))");

                        using (writer.WithBlock())
                        {
                            writer.AppendLine(
                                "byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes($\"[{System.DateTimeOffset.Now:HH:mm:ss.fff}] {message}{System.Environment.NewLine}\");");

                            writer.AppendLine("stream.Write(bytes, 0, bytes.Length);");
                        }
                    },
                    SkipPartial = true
                }
            ]
        };

        static void Write(CodeWriter writer, string prefix)
        {
            writer.AppendConditionalSymbol("DEBUG");
            writer.Append("Write($\"[");
            writer.Append(prefix);
            writer.AppendLine("] {message}\");");
            writer.AppendPreprocessorSymbol("#endif");
        }
    }
}