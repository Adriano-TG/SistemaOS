using System;
using Serilog;
using Serilog.Context;
using SistemaOS.Core.Interfaces;

namespace SistemaOS.Infrastructure.Services;

/// <summary>
/// Implementação do ILoggerService usando Serilog
/// </summary>
public class SerilogLoggerService : ILoggerService, IDisposable
{
    private readonly ILogger _logger;
    private bool _disposed;

    public SerilogLoggerService()
    {
        _logger = Log.Logger;
    }

    public SerilogLoggerService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogInformation(string messageTemplate, params object[] propertyValues)
    {
        _logger.Information(messageTemplate, propertyValues);
    }

    public void LogWarning(string messageTemplate, params object[] propertyValues)
    {
        _logger.Warning(messageTemplate, propertyValues);
    }

    public void LogError(Exception exception, string messageTemplate, params object[] propertyValues)
    {
        _logger.Error(exception, messageTemplate, propertyValues);
    }

    public void LogError(string messageTemplate, params object[] propertyValues)
    {
        _logger.Error(messageTemplate, propertyValues);
    }

    public void LogDebug(string messageTemplate, params object[] propertyValues)
    {
        _logger.Debug(messageTemplate, propertyValues);
    }

    public void LogCritical(string messageTemplate, params object[] propertyValues)
    {
        _logger.Fatal(messageTemplate, propertyValues);
    }

    public IDisposable BeginScope(string scopeName)
    {
        return LogContext.PushProperty("Scope", scopeName);
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return LogContext.PushProperty("Scope", state?.ToString() ?? "null");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            // Não fechamos o Log global aqui, apenas se criamos o logger
        }
    }
}

/// <summary>
/// Configuração estática do Serilog para inicialização na App
/// </summary>
public static class SerilogConfig
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SistemaOS", "Logs", "sistemaos-.log");

    public static void Configure()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "SistemaOS")
            .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                LogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        Log.Information("=== Serilog configurado ===");
    }

    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}