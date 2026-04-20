using CloudTek.Testing;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Xunit;

#pragma warning disable CA1848 // Use LoggerMessage delegates for performance

namespace Hive.OpenTelemetry.Tests.E2E;

/// <summary>
/// End-to-end tests for log emission via OpenTelemetry
/// </summary>
[Collection("E2E Tests")]
public class LogEmissionTests : E2ETestBase
{
  private const string ServiceName = "log-emission-tests";

  [Fact]
  [IntegrationTest]
  public async Task GivenOpenTelemetryLogging_WhenLogEmitted_ThenLogRecordIsCaptured()
  {
    // Arrange
    var service = CreateLoggingTestService<LogEmissionTests>(
      ServiceName,
      app => app.MapGet("/log-test", (ILogger<LogEmissionTests> logger) =>
      {
        logger.LogInformation("Test log message from endpoint");
        return "OK";
      }));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      await client.GetAsync("/log-test");
      await Task.Delay(200);
    });

    // Assert - log record should be captured
    ExportedLogs.Should().NotBeEmpty("log records should be captured");
    ExportedLogs.Should().Contain(log =>
      log.FormattedMessage != null &&
      log.FormattedMessage.Contains("Test log message"),
      "should contain the test log message");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenOpenTelemetryLogging_WhenDifferentLogLevelsUsed_ThenAllLevelsAreCaptured()
  {
    // Arrange
    var service = CreateLoggingTestService<LogEmissionTests>(
      ServiceName,
      app => app.MapGet("/levels-test", (ILogger<LogEmissionTests> logger) =>
      {
        logger.LogTrace("Trace level message");
        logger.LogDebug("Debug level message");
        logger.LogInformation("Information level message");
        logger.LogWarning("Warning level message");
        logger.LogError("Error level message");
        return "OK";
      }));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      await client.GetAsync("/levels-test");
      await Task.Delay(200);
    });

    // Assert - at least Information, Warning, and Error should be captured (default level filtering)
    ExportedLogs.Should().Contain(log =>
      log.LogLevel == LogLevel.Information,
      "Information level logs should be captured");
    ExportedLogs.Should().Contain(log =>
      log.LogLevel == LogLevel.Warning,
      "Warning level logs should be captured");
    ExportedLogs.Should().Contain(log =>
      log.LogLevel == LogLevel.Error,
      "Error level logs should be captured");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenOpenTelemetryLogging_WhenStructuredLoggingUsed_ThenStateIsCaptured()
  {
    // Arrange
    var service = CreateLoggingTestService<LogEmissionTests>(
      ServiceName,
      app => app.MapGet("/structured-test", (ILogger<LogEmissionTests> logger) =>
      {
        logger.LogInformation("Processing request for user {UserId} with action {Action}",
          "user-123", "test-action");
        return "OK";
      }));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      await client.GetAsync("/structured-test");
      await Task.Delay(200);
    });

    // Assert - log record should capture structured data
    var structuredLog = ExportedLogs.FirstOrDefault(log =>
      log.FormattedMessage != null &&
      log.FormattedMessage.Contains("Processing request"));

    structuredLog.Should().NotBeNull("structured log should be captured");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenOpenTelemetryLogging_WhenExceptionLogged_ThenExceptionIsCaptured()
  {
    // Arrange
    var service = CreateLoggingTestService<LogEmissionTests>(
      ServiceName,
      app => app.MapGet("/exception-test", (ILogger<LogEmissionTests> logger) =>
      {
        try
        {
          throw new InvalidOperationException("Test exception for logging");
        }
        catch (Exception ex)
        {
          logger.LogError(ex, "An error occurred during test");
        }
        return "OK";
      }));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      await client.GetAsync("/exception-test");
      await Task.Delay(200);
    });

    // Assert - error log with exception should be captured
    var errorLog = ExportedLogs.FirstOrDefault(log =>
      log.LogLevel == LogLevel.Error);

    errorLog.Should().NotBeNull("error log should be captured");
    errorLog!.Exception.Should().NotBeNull("exception should be captured in log record");
    errorLog.Exception!.Message.Should().Contain("Test exception",
      "exception message should be preserved");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenOpenTelemetryLogging_WhenLoggerFromDI_ThenCategoryNameIsCorrect()
  {
    // Arrange
    var service = CreateLoggingTestService<LogEmissionTests>(
      ServiceName,
      app => app.MapGet("/category-test", (ILogger<LogEmissionTests> logger) =>
      {
        logger.LogInformation("Category test message");
        return "OK";
      }));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      await client.GetAsync("/category-test");
      await Task.Delay(200);
    });

    // Assert - log should have correct category
    var log = ExportedLogs.FirstOrDefault(l =>
      l.FormattedMessage != null &&
      l.FormattedMessage.Contains("Category test"));

    log.Should().NotBeNull("log with category should be captured");
    log!.CategoryName.Should().Contain(nameof(LogEmissionTests),
      "category name should reflect the logger type");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenOpenTelemetryLogging_WhenMultipleLogsEmitted_ThenAllAreCaptured()
  {
    // Arrange
    var service = CreateLoggingTestService<LogEmissionTests>(
      ServiceName,
      app => app.MapGet("/multiple-test", (ILogger<LogEmissionTests> logger) =>
      {
        for (var i = 1; i <= 5; i++)
        {
          logger.LogInformation("Log message number {Number}", i);
        }
        return "OK";
      }));

    // Act & Assert
    await RunServiceAndExecuteAsync(service, async () =>
    {
      using var client = CreateHttpClient();
      await client.GetAsync("/multiple-test");
      await Task.Delay(200);
    });

    // Assert - multiple logs should be captured
    var testLogs = ExportedLogs.Where(log =>
      log.FormattedMessage != null &&
      log.FormattedMessage.Contains("Log message number")).ToList();

    testLogs.Should().HaveCountGreaterOrEqualTo(5,
      "all 5 log messages should be captured");
  }
}