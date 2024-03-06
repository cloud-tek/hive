using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hive.Testing;

/// <summary>
/// Discovers the <see cref="SmokeTestAttribute"/> trait
/// </summary>
public class SmokeTestDiscoverer : ITraitDiscoverer
{
  /// <summary>
  /// The name of the discoverer's tests' type
  /// </summary>
  public const string TypeName = AssemblyInfo.Name + "." + nameof(SmokeTestDiscoverer);

  /// <summary>
  /// Gets the traits of the provided attribute
  /// </summary>
  /// <param name="traitAttribute"></param>
  /// <returns>An KeyValuePair containing "Category" as the key and the test's target category as the value</returns>
  public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
  {
    yield return new KeyValuePair<string, string>(Constants.Category, SmokeTestAttribute.Category);
  }
}