using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting
{
    internal static class NgrokLoggingExtensions
    {
        private static string FormatSource(string filePath, int line, string member)
        {
            var file = string.IsNullOrEmpty(filePath) ? "<unknown>" : Path.GetFileName(filePath);
            return $"(src={file}:{line}#{member})";
        }

        private static void LogIfEnabled(this ILogger? logger, LogLevel level, string message, object?[]? args, Exception? ex,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (logger is null) return;
            if (!logger.IsEnabled(level)) return;
            var src = FormatSource(file, line, member);
            if (args == null || args.Length == 0)
            {
                if (ex is null)
                    logger.Log(level, "{Message} {Source}", message, src);
                else
                    logger.Log(level, ex, "{Message} {Source}", message, src);
            }
            else
            {
                // append source as last argument and include a placeholder at the end of the message
                var newArgs = new object?[args.Length + 1];
                for (int i = 0; i < args.Length; i++) newArgs[i] = args[i];
                newArgs[^1] = src;
                var msgWithSrc = message + " {Source}";
                if (ex is null)
                    logger.Log(level, msgWithSrc, newArgs);
                else
                    logger.Log(level, ex, msgWithSrc, newArgs);
            }
        }

        public static void Info(this ILogger? logger, string message, params object?[] args) =>
            logger.LogIfEnabled(LogLevel.Information, "ngrok: " + message, args, null);

        public static void Warn(this ILogger? logger, string message, params object?[] args) =>
            logger.LogIfEnabled(LogLevel.Warning, "ngrok: " + message, args, null);

        public static void Error(this ILogger? logger, Exception ex, string message, params object?[] args) =>
            logger.LogIfEnabled(LogLevel.Error, "ngrok: " + message, args, ex);

        public static void Warn(this ILogger? logger, Exception ex, string message, params object?[] args) =>
            logger.LogIfEnabled(LogLevel.Warning, "ngrok: " + message, args, ex);

        public static void Debug(this ILogger? logger, string message, params object?[] args) =>
            logger.LogIfEnabled(LogLevel.Debug, "ngrok: " + message, args, null);
    }
}
