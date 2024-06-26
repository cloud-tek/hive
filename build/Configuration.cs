using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Nuke.Common.Tooling;

[TypeConverter(typeof(TypeConverter<Configuration>))]
[ExcludeFromCodeCoverage]
public class Configuration : Enumeration
{
  public static Configuration Debug = new Configuration { Value = nameof(Debug) };
  public static Configuration Release = new Configuration { Value = nameof(Release) };

  public static implicit operator string(Configuration configuration)
  {
    return configuration.Value;
  }
}