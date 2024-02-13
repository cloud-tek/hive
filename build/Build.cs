using System;
using CloudTek.Build;
using CloudTek.Build.Primitives;
using CloudTek.Build.Versioning;
using Nuke.Common.Execution;
using Nuke.Common.Tools.GitVersion;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CloudTek.Build.Packaging;
using Nuke.Common.Utilities.Collections;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace _build
{
  [ExcludeFromCodeCoverage]
  public class Build : SmartBuild<PackageManager.NuGet, VersioningStrategy.GitVersion>
  {
    public static int Main() => Execute<Build>(x => x.Compile);

    /// <summary>
    /// GitVersion information for SmartBuild
    /// </summary>
    [GitVersion(Framework = "net8.0", NoFetch = true)]
    public GitVersion GitVersion { get; set; } = default!;

    public override Regex PackageChecksRegex { get; init; } = new Regex("^CloudTek", RegexOptions.Compiled);

    public Build() : base(Repository)
    { }

    static IDictionary<string, ArtifactType> HiveCore = new Dictionary<string, ArtifactType>()
        {
          { "Hive.Abstractions", ArtifactType.Package }
        };

    static IDictionary<string, ArtifactType> HiveLogging = new Dictionary<string, ArtifactType>()
        {
          { "Hive.Logging", ArtifactType.Package },
          { "Hive.Logging.LogzIo", ArtifactType.Package },
          { "Hive.Logging.AppInsights", ArtifactType.Package },
          { "Hive.Logging.Xunit", ArtifactType.Package }
        };

    static IDictionary<string, ArtifactType> HiveMicroServices = new Dictionary<string, ArtifactType>()
        {
          { "Hive.MicroServices", ArtifactType.Package },
          { "Hive.MicroServices.Api", ArtifactType.Package },
          { "Hive.MicroServices.GraphQL", ArtifactType.Package },
          { "Hive.MicroServices.Grpc", ArtifactType.Package },
          { "Hive.MicroServices.Job", ArtifactType.Package },
          { "Hive.MicroServices.Demo", ArtifactType.Demo },
          { "Hive.MicroServices.Demo.Api", ArtifactType.Demo },
          { "Hive.MicroServices.Demo.ApiControllers", ArtifactType.Demo },
          { "Hive.MicroServices.Demo.GraphQL", ArtifactType.Demo },
          { "Hive.MicroServices.Demo.Grpc", ArtifactType.Demo },
          { "Hive.MicroServices.Demo.GrpcCodeFirst", ArtifactType.Demo },
          { "Hive.MicroServices.Demo.Job", ArtifactType.Demo },
        };

    new static readonly Repository Repository = new()
    {
      Artifacts = RootDirectory.GetArtifacts(
        ("hive.core", HiveCore),
        ("hive.logging", HiveLogging),
        ("hive.microservices", HiveMicroServices))
    };
  }
}
