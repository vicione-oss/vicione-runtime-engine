using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class TestLogger : ILogger
{
    private readonly List<(LogLevel Level, string Message)> _receivedMessages = [];

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => Substitute.For<IDisposable>();
    public string Category { get; }
    public bool IsEnabled(LogLevel logLevel) => true;

    internal TestLogger() : this(string.Empty) { }
    internal TestLogger(string category) => Category = category;

    public void ClearLoggedMessages()
        => _receivedMessages.Clear();

    public IEnumerable<string> GetLoggedMessages()
        => _receivedMessages.Select(x => x.Message);

    public IEnumerable<string> GetLoggedMessages(LogLevel logLevel)
        => _receivedMessages.Where(x => x.Level == logLevel).Select(x => x.Message);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _receivedMessages.Add((logLevel, state?.ToString() ?? string.Empty));
}
