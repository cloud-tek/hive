using System.ComponentModel.DataAnnotations;

namespace Hive.Tests.Configuration;

public class SimpleOptions
{
  public const string SectionKey = "SimpleOptions";

  [Required]
  [MinLength(3)]
  public string Name { get; set; } = default!;

  public string Address { get; set; } = default!;
}
