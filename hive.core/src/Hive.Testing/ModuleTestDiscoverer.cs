using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hive.Testing
{
    public class ModuleTestDiscoverer : ITraitDiscoverer
    {
        public const string TypeName = AssemblyInfo.Name + "." + nameof(ModuleTestDiscoverer);

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>("Category", ModuleTestAttribute.Category);
        }
    }
}
