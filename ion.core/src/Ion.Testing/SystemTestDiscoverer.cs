using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Ion.Testing
{
    public class SystemTestDiscoverer : ITraitDiscoverer
    {
        public const string TypeName = AssemblyInfo.Name + "." + nameof(SystemTestDiscoverer);

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>("Category", SystemTestAttribute.Category);
        }
    }
}