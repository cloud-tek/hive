using Xunit;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

namespace Hive.OpenTelemetry.Tests.E2E;

/// <summary>
/// Collection definition for E2E tests to run serially.
/// These tests use environment variables for port configuration and
/// start actual HTTP servers, so they cannot run in parallel.
/// </summary>
[CollectionDefinition("E2E Tests", DisableParallelization = true)]
public class E2ETestCollection : ICollectionFixture<E2ETestFixture>
{
}

/// <summary>
/// Shared fixture for E2E tests
/// </summary>
public class E2ETestFixture
{
}