using System;
using Xunit.Sdk;

namespace Ion.Testing
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    [TraitDiscoverer(ModuleTestDiscoverer.TypeName, AssemblyInfo.Name)]
    public class ModuleTestAttribute : Attribute, ITraitAttribute
    {
        public const string Category = "ModuleTests";
    }
}