using System;
using Xunit.Sdk;

namespace Hive.Testing
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    [TraitDiscoverer(SystemTestDiscoverer.TypeName, AssemblyInfo.Name)]
    public class SystemTestAttribute : Attribute, ITraitAttribute
    {
        public const string Category = "SystemTests";
    }
}
