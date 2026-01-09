using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

internal sealed class TestLoggerFactory : ILoggerFactory
{
    private readonly Dictionary<string, TestLogger> _testLoggers = new(StringComparer.Ordinal);

    public void AddProvider(ILoggerProvider provider) { }

    public ILogger CreateLogger(string categoryName)
    {
        if (_testLoggers.TryGetValue(categoryName, out var existingLogger))
            return existingLogger;

        var testLogger = new TestLogger(categoryName);
        _testLoggers[categoryName] = testLogger;
        return testLogger;
    }

    public TestLogger GetLogger(string categoryName)
        => _testLoggers[categoryName];

    public IEnumerable<TestLogger> GetLoggers()
        => _testLoggers.Values;

    public void Dispose()
    {
        foreach (var logger in _testLoggers.Values)
            logger.ClearLoggedMessages();
        _testLoggers.Clear();
    }
}
