using System.Text;

namespace Hive.Extensions;

/// <summary>
/// String extensions
/// </summary>
public static class StringExtensions
{
  /// <summary>
  /// Checks if the string is null or empty
  /// </summary>
  /// <param name="value"></param>
  /// <returns>boolean</returns>
  public static bool IsNullOrEmpty(this string value)
  {
    return string.IsNullOrEmpty(value);
  }

  /// <summary>
  /// Checks if the string is not null or empty
  /// </summary>
  /// <param name="value"></param>
  /// <returns>boolean</returns>
  public static bool IsNotNullOrEmpty(this string value)
  {
    return !string.IsNullOrEmpty(value);
  }

  /// <summary>
  /// Checks if does not contain any of the provided values
  /// </summary>
  /// <param name="value"></param>
  /// <param name="values"></param>
  /// <returns>boolean</returns>
  /// <exception cref="InvalidOperationException">Thrown when there are no provided values</exception>
  public static bool DoesNotContain(this string value, params string[] values)
  {
    if (values == null || values.Length == 0)
      throw new InvalidOperationException(nameof(values));

    return !values.Any(value.Contains);
  }

  /// <summary>
  /// Checks if the provided string belongs to a set of values representing development environments
  /// </summary>
  /// <param name="value"></param>
  /// <returns>boolean</returns>
  public static bool IsDevelopment(this string value)
  {
    return Environments.Development.Any(env => value.Equals(env, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Checks if the provided string belongs to a set of values representing uat environments
  /// </summary>
  /// <param name="value"></param>
  /// <returns>boolean</returns>
  public static bool IsUat(this string value)
  {
    return Environments.Test.Any(env => value.Equals(env, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Checks if the provided string belongs to a set of values representing production environments
  /// </summary>
  /// <param name="value"></param>
  /// <returns>boolean</returns>
  public static bool IsProduction(this string value)
  {
    return Environments.Production.Any(env => value.Equals(env, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Converts the provided string values to a multiline string
  /// </summary>
  /// <param name="values"></param>
  /// <returns>A multiline representation of the provided string values</returns>
  public static string ToMultilineString(this string[] values)
  {
    {
      var sb = new StringBuilder();

      foreach (var value in values)
      {
        sb.AppendLine(value);
      }

      return sb.ToString();
    }
  }

  private static class Environments
  {
    public static readonly string[] Development =
    {
            "dev",
            "development"
    };

    public static readonly string[] Test =
    {
            "uat",
            "test"
    };

    public static readonly string[] Production =
    {
            "xyz",
            "prod",
            "production"
    };
  }
}