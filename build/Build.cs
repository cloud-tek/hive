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

        static new readonly Repository Repository = new ()
        {
            Artifacts = (new Dictionary<string, Artifact[]>()
            {
                { 
                    "ion.core", new[]
                    {
                        new Artifact() { Type = ArtifactType.Package, Project = "Ion.Abstractions" },
                        new Artifact() { Type = ArtifactType.Package, Project = "Ion.Analyzers" },
                        new Artifact() { Type = ArtifactType.Package, Project = "Ion.Testing" }
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