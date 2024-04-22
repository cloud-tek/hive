using System.ComponentModel.DataAnnotations;
// todo: replace hive.testing's attributes with cloudtek.testing after net8 TFM change
namespace Hive.Abstractions.Tests.Configuration;

public class ComplexOptions
{
  public const string SectionKey = "ComplexOptions";

  [Required][MinLength(2)] public SimpleOptions[] Children { get; set; } = Array.Empty<SimpleOptions>();

  [Required] public string? Name { get; set; }
}