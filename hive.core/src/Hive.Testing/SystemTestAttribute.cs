using Xunit.Sdk;

namespace Hive.Testing;

/// <summary>
/// Flags the test as a test with Category equal to "SmokeTests"
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
[TraitDiscoverer(SmokeTestDiscoverer.TypeName, AssemblyInfo.Name)]
public class SmokeTestAttribute : Attribute, ITraitAttribute
{
  /// <summary>
  /// The test trait
  /// </summary>
  public const string Category = "SmokeTests";
}