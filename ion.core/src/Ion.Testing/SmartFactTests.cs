using FluentAssertions;
using System.Runtime.InteropServices;

namespace Ion.Testing
{
    public class SmartFactTests
    {
        public class GivenTestMethodIsDecoratedByASmartFact
        {
            [SmartFact(On.Windows)]
            public void WhenExecuteOnWindows_ThenItDoesNotExecuteAnywhereElse()
            {
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows).Should().BeTrue();
            }

            [SmartFact(On.Linux)]
            public void WhenExecuteOnLinux_ThenItDoesNotExecuteAnywhereElse()
            {
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux).Should().BeTrue();
            }

            [SmartFact(On.MacOS)]
            public void WhenExecuteOnMacOs_ThenItDoesNotExecuteAnywhereElse()
            {
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX).Should().BeTrue();
            }
        }
    }
}