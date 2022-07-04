using System;
using Xunit.Sdk;

namespace Hive.Testing
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    [TraitDiscoverer(UnitTestDiscoverer.TypeName, AssemblyInfo.Name)]
    public class UnitTestAttribute : Attribute, ITraitAttribute
    {
        public const string Category = "UnitTests";
    }
}
