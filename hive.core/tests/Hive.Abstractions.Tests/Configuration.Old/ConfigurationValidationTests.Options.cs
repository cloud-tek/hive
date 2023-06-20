using System;
using System.ComponentModel.DataAnnotations;

namespace Hive.Tests.Configuration;

public partial class ConfigurationValidationTests
{
  public partial class Options
  {
    public const string SectionKey = "TestOptions";

    [Required, MinLength(1)] public ChildOptions[] Children { get; set; } = Array.Empty<ChildOptions>();

    [Required] public string? Name { get; set; }
  }
}
