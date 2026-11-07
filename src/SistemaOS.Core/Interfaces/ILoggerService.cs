using System;

namespace SistemaOS.Core.Interfaces;

/// <summary>
/// Interface para serviço de logging estruturado
/// </summary>
public interface ILoggerService
{
    void LogInformation(string messageTemplate, params object[] propertyValues);
    void LogWarning(string messageTemplate, params object[] propertyValues);
    void LogError(Exception exception, string messageTemplate, params object[] propertyValues);
    void LogError(string messageTemplate, params object[] propertyValues);
    void LogDebug(string messageTemplate, params object[] propertyValues);
    void LogCritical(string messageTemplate, params object[] propertyValues);

    IDisposable BeginScope(string scopeName);
    IDisposable BeginScope<TState>(TState state);
}