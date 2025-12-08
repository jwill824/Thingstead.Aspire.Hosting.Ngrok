using System;
using System.Collections.Generic;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Thingstead.Aspire.Hosting.Ngrok.Tests
{
    public class NgrokLoggingExtensionsTests
    {
        [Fact]
        public void Info_adds_prefix_and_source()
        {
            var logger = new TestLogger(level => true);
            logger.Info("hello {Name}", "world");

            Assert.Single(logger.Entries);
            var e = logger.Entries[0];
            Assert.Equal(LogLevel.Information, e.Level);
            Assert.Contains("ngrok: hello world", e.Message);
            Assert.Contains("(src=", e.Message);
            Assert.Null(e.Exception);
        }

        [Fact]
        public void Warn_with_exception_records_exception_and_prefix()
        {
            var logger = new TestLogger(level => true);
            var ex = new InvalidOperationException("boom");
            logger.Warn(ex, "problem {P}", 42);

            Assert.Single(logger.Entries);
            var e = logger.Entries[0];
            Assert.Equal(LogLevel.Warning, e.Level);
            Assert.Same(ex, e.Exception);
            Assert.Contains("ngrok: problem 42", e.Message);
        }

        [Fact]
        public void Error_records_exception()
        {
            var logger = new TestLogger(level => true);
            var ex = new ArgumentNullException("x");
            logger.Error(ex, "oops");

            Assert.Single(logger.Entries);
            var e = logger.Entries[0];
            Assert.Equal(LogLevel.Error, e.Level);
            Assert.Same(ex, e.Exception);
            Assert.Contains("ngrok: oops", e.Message);
        }

        [Fact]
        public void Debug_not_logged_when_disabled()
        {
            // only enable Information+ levels
            var logger = new TestLogger(level => level >= LogLevel.Information);
            logger.Debug("should not appear");
            Assert.Empty(logger.Entries);
        }
    }

    internal sealed class TestLogger(Func<LogLevel, bool> isEnabled) : ILogger
    {
        public List<LogEntry> Entries { get; } = new();
        private readonly Func<LogLevel, bool> _isEnabled = isEnabled ?? (_ => true);

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => _isEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var msg = formatter(state, exception);
            Entries.Add(new LogEntry { Level = logLevel, Message = msg, Exception = exception });
        }
    }

    internal sealed class LogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception Exception { get; set; }
    }

    internal sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
