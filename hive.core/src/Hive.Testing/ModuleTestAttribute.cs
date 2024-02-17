using Xunit.Sdk;

namespace Hive.Testing;

/// <summary>
/// Flags the test as a test with Category equal to "ModuleTests"
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
[TraitDiscoverer(ModuleTestDiscoverer.TypeName, AssemblyInfo.Name)]
public class ModuleTestAttribute : Attribute, ITraitAttribute
{
  /// <summary>
  /// The test trait
  /// </summary>
  public const string Category = "ModuleTests";
}