using System;
using System.Collections.Generic;
using System.Linq;
using CloudTek.Build.Primitives;
using Nuke.Common.IO;

namespace _build;

internal static class ArtifactsExtensions
{
  internal static Artifact[] GetArtifacts(this AbsolutePath rootDirectory, params (string key, IDictionary<string, ArtifactType> module)[] modules)
  {
    _ = modules ?? throw new ArgumentNullException(nameof(modules));

    var result = new List<Artifact>();

    foreach (var m in modules)
    {
      result.AddRange(m.module.ToArray(rootDirectory, m.key));
    }

    return result.ToArray();
  }
  private static Artifact[] ToArray(this IDictionary<string, ArtifactType> artifacts, AbsolutePath rootDirectory, string module)
  {
    return artifacts
      .Select(x => new Artifact
        { Type = x.Value, Path = rootDirectory / module / "src" / x.Key / "*.*sproj" })
      .ToArray();
  }
}
