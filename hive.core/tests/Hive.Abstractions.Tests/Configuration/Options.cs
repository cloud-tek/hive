using System;
using System.ComponentModel.DataAnnotations;

namespace Hive.Tests.Configuration;

public class Options
{
  public const string SectionKey = "TestOptions";

  [Required, MinLength(1)] public ConfigurationValidationTests.ChildOptions[] Children { get; set; } = Array.Empty<ConfigurationValidationTests.ChildOptions>();

  [Required] public string? Name { get; set; }
}
