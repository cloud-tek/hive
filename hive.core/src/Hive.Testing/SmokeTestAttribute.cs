using Xunit.Sdk;

namespace Hive.Testing;

/// <summary>
/// Flags the test as a test with Category equal to "SystemTests"
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
[TraitDiscoverer(SystemTestDiscoverer.TypeName, AssemblyInfo.Name)]
public class SystemTestAttribute : Attribute, ITraitAttribute
{
  /// <summary>
  /// The test trait
  /// </summary>
  public const string Category = "SystemTests";
}