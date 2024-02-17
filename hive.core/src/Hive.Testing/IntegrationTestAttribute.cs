using Xunit.Sdk;

namespace Hive.Testing;

/// <summary>
/// Flags the test as a test with Category equal to "IntegrationTests"
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
[TraitDiscoverer(IntegrationTestDiscoverer.TypeName, AssemblyInfo.Name)]
public class IntegrationTestAttribute : Attribute, ITraitAttribute
{
  /// <summary>
  /// The test trait
  /// </summary>
  public const string Category = "IntegrationTests";
}