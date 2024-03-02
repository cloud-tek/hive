using System.Reflection;
using System.Text.Json.Serialization;

namespace Hive.Middleware;

/// <summary>
/// The liveness response.
/// </summary>
public class LivenessResponse : MiddlewareResponse
{
  /// <summary>
  /// Creates a new <see cref="LivenessResponse"/> instance
  /// </summary>
  /// <param name="service"></param>
  public LivenessResponse(IMicroService service)
  : base(service)
  {
    var asm = Assembly.GetEntryAssembly();
    var versions = new Dictionary<string, string>();

    versions[asm!.GetName().Name!] = asm!.GetName().Version!.ToString();

    foreach (var assembly in asm.GetReferencedAssemblies())
    {
      if (assembly.Name != null && assembly.Version != null)
      {
        versions[assembly.Name] = assembly.Version.ToString();
      }
    }

    Versions = versions.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
  }

  /// <summary>
  /// The versions of the assemblies returned by the liveness response
  /// </summary>
  [JsonPropertyName("versions")]
  public Dictionary<string, string> Versions { get; set; } = new Dictionary<string, string>();
}