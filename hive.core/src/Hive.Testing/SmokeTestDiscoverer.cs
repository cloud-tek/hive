using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hive.Testing
{
    public class SmokeTestDiscoverer : ITraitDiscoverer
    {
        public const string TypeName = AssemblyInfo.Name + "." + nameof(SmokeTestDiscoverer);

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>("Category", SmokeTestAttribute.Category);
        }
    }
}
