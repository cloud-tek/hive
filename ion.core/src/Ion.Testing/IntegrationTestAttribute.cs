using System;
using Xunit.Sdk;

namespace Ion.Testing
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    [TraitDiscoverer(IntegrationTestDiscoverer.TypeName, AssemblyInfo.Name)]
    public class IntegrationTestAttribute : Attribute, ITraitAttribute
    {
        public const string Category = "IntegrationTests";
    }
}