using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Ion.Testing
{
    public class UnitTestDiscoverer : ITraitDiscoverer
    {
        public const string TypeName = AssemblyInfo.Name + "." + nameof(UnitTestDiscoverer);

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>("Category", UnitTestAttribute.Category);
        }
    }
}