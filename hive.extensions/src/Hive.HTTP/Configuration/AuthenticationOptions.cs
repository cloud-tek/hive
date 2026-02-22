using System.ComponentModel.DataAnnotations;

namespace Hive.HTTP.Configuration;

public class AuthenticationOptions
{
  [Required]
  public string Type { get; set; } = default!;

  public string? HeaderName { get; set; }

  public string? Value { get; set; }
}