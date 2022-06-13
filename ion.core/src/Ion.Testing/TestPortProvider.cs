using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Ion.Testing
{
    public static class TestPortProvider
    {
        public static IDisposable GetAvailableServicePortScope(ushort startingPort, out ushort port)
        {
            port = GetAvailablePort(startingPort);

            return EnvironmentVariableScope.Create("ASPNETCORE_URLS",
                $"http://localhost:{port}");
        }

        public static ushort GetAvailablePort(ushort startingPort)
        {
            var portArray = new List<ushort>();

            var properties = IPGlobalProperties.GetIPGlobalProperties();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                portArray.AddRange(GetPorts(
                    () => properties.GetActiveTcpConnections().Select(x => x.LocalEndPoint), startingPort));
                portArray.AddRange(GetPorts(() => properties.GetActiveTcpListeners(), startingPort));
                portArray.AddRange(GetPorts(() => properties.GetActiveUdpListeners(), startingPort));
            }
            else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                portArray.AddRange(GetPorts(
                    () => properties.GetActiveTcpConnections().Select(x => x.LocalEndPoint), startingPort, true));
                portArray.AddRange(GetPorts(
                    () => properties.GetActiveTcpConnections()
                        .Where(x => x.State == TcpState.Listen)
                        .Select(x => x.LocalEndPoint), startingPort, true));
                portArray.AddRange(GetPorts(() => properties.GetActiveUdpListeners(), startingPort, true));
            }

            portArray = portArray.Distinct().ToList();
            portArray.Sort();

            for (ushort i = startingPort; i < ushort.MaxValue; i++)
            {
                if (!portArray.Contains(i))
                {
                    return Convert.ToUInt16(i);
                }
            }

            return 0;
        }

        private static IEnumerable<ushort> GetPorts(Func<IEnumerable<IPEndPoint>> endpointSelector, ushort startinPort, bool invertEndianness = false)
        {
            return endpointSelector().Where(x => x.Port >= startinPort).Select(x =>
                invertEndianness ? InvertEndianness(Convert.ToUInt16(x.Port)) : Convert.ToUInt16(x.Port));
        }

        public static ushort InvertEndianness(ushort value)
        {
            return (ushort)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }
    }
}