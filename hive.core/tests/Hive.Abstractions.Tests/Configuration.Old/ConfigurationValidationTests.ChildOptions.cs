using System.ComponentModel.DataAnnotations;

namespace Hive.Tests.Configuration;

public partial class ConfigurationValidationTests
{
  // ReSharper disable once ClassNeverInstantiated.Global
  public partial class ChildOptions
  {
    [Required] public string? Name { get; set; }
  }
}
