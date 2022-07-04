using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Hive.Testing;

public static class ConfigurationExtensions
{
    private const string IonLoggingLogzIoEnvVar = "Ion__Logging__LogzIo__Token";

    public static IConfigurationBuilder UseDefaultLoggingConfiguration(this IConfigurationBuilder builder)
    {
        builder.AddInMemoryCollection(new Dictionary<string, string>()
        {
            { "Hive:Logging:Level", "Information" }
        });

        return builder;
    }

    public static IConfigurationBuilder UseTestLogzIoConfiguration(this IConfigurationBuilder builder)
    {
        builder.AddInMemoryCollection(new Dictionary<string, string>()
        {
            { "Hive:Logging:LogzIo:Region", "eu" },
            { "Hive:Logging:LogzIo:Token", Environment.GetEnvironmentVariable(IonLoggingLogzIoEnvVar) ?? throw new ArgumentNullException($"Missing environment variable {IonLoggingLogzIoEnvVar}") }
        });

        return builder;
    }
}
