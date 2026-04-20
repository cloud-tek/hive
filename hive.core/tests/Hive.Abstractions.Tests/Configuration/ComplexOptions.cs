using System.ComponentModel.DataAnnotations;

namespace Hive.Abstractions.Tests.Configuration;

public class ComplexOptions
{
  public const string SectionKey = "ComplexOptions";

  [Required][MinLength(2)] public SimpleOptions[] Children { get; set; } = Array.Empty<SimpleOptions>();

  [Required] public string? Name { get; set; }
}