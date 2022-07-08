using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hive.Testing
{
    public class IntegrationTestDiscoverer : ITraitDiscoverer
    {
        public const string TypeName = AssemblyInfo.Name + "." + nameof(IntegrationTestDiscoverer);

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>("Category", IntegrationTestAttribute.Category);
        }
    }
}
