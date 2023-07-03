using CloudTek.Build;
using CloudTek.Build.Primitives;
using CloudTek.Build.Versioning;
using Nuke.Common.Execution;
using Nuke.Common.Tools.GitVersion;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common.Utilities.Collections;

// ReSharper disable once CheckNamespace
namespace _build
{
    [CheckBuildProjectConfigurations]
    public class Build : SmartGitVersionBuild
    {
        public static int Main () => Execute<Build>(x => x.Compile);

        public Build() : base(Repository)
        { }

        new static readonly Repository Repository = new ()
        {
            Artifacts = (new Dictionary<string, Artifact[]>()
            {
                {
                    "hive.core", new[]
                    {
                        new Artifact() { Type = ArtifactType.Package, Project = "Hive.Abstractions" },
                        new Artifact() { Type = ArtifactType.Package, Project = "Hive.Analyzers" },
                        new Artifact() { Type = ArtifactType.Package, Project = "Hive.Testing" }
                    }
                },
                {
                    "hive.logging", new[]
                    {
                        new Artifact() { Type = ArtifactType.Package, Project = "Hive.Logging" },
                        new Artifact() { Type = ArtifactType.Package, Project = "Hive.Logging.LogzIo" },
                        new Artifact() { Type = ArtifactType.Package, Project = "Hive.Logging.AppInsights" },
                        new Artifact() { Type = ArtifactType.Package, Project = "Hive.Logging.Xunit" }
                    }
                },
                {
                    "hive.microservices", new[]
                    {
                      new Artifact() { Type = ArtifactType.Package, Project = "Hive.MicroServices" },
                      new Artifact() { Type = ArtifactType.Package, Project = "Hive.MicroServices.Api" },
                      new Artifact() { Type = ArtifactType.Package, Project = "Hive.MicroServices.GraphQL" },
                      new Artifact() { Type = ArtifactType.Package, Project = "Hive.MicroServices.Grpc" },
                      new Artifact() { Type = ArtifactType.Package, Project = "Hive.MicroServices.Job" },

                      // Demos
                      new Artifact() { Type = ArtifactType.Demo, Project = "Hive.MicroServices.Demo.Api" },
                      new Artifact() { Type = ArtifactType.Demo, Project = "Hive.MicroServices.Demo.ApiControllers" },
                      new Artifact() { Type = ArtifactType.Demo, Project = "Hive.MicroServices.Demo" },
                      new Artifact() { Type = ArtifactType.Demo, Project = "Hive.MicroServices.Demo.Grpc" },
                      new Artifact() { Type = ArtifactType.Demo, Project = "Hive.MicroServices.Demo.GrpcCodeFirst" },
                      new Artifact() { Type = ArtifactType.Demo, Project = "Hive.MicroServices.Demo.GraphQL" },
                      new Artifact() { Type = ArtifactType.Demo, Project = "Hive.MicroServices.Demo.Job" }
                    }
                }
            }).SelectMany(module =>
            {
                module.Value.ForEach(artifact => artifact.Module = module.Key);
                return module.Value;
            }).ToArray()
        };
    }
}
