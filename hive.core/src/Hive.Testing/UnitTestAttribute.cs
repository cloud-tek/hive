using Xunit.Sdk;

namespace Hive.Testing;

/// <summary>
/// Flags the test as a test with Category equal to "UnitTests"
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
[TraitDiscoverer(UnitTestDiscoverer.TypeName, AssemblyInfo.Name)]
public class UnitTestAttribute : Attribute, ITraitAttribute
{
  /// <summary>
  /// The test trait
  /// </summary>
  public const string Category = "UnitTests";
}