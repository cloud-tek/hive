namespace Hive;

/// <summary>
/// Detects conflicting values between ASPNETCORE_ENVIRONMENT and DOTNET_ENVIRONMENT.
/// </summary>
internal static class EnvironmentVariableConflictDetector
{
  private static readonly System.Text.CompositeFormat ConflictFormat =
    System.Text.CompositeFormat.Parse(Constants.Errors.EnvironmentVariableConflict);

  /// <summary>
  /// Returns a conflict message when both environment variables are set to case-insensitively different values,
  /// or <c>null</c> when there is no conflict.
  /// </summary>
  /// <param name="aspNetCoreEnvironment">Value of ASPNETCORE_ENVIRONMENT, or null/empty if not set.</param>
  /// <param name="dotNetEnvironment">Value of DOTNET_ENVIRONMENT, or null/empty if not set.</param>
  /// <returns>A self-sufficient conflict message, or <c>null</c> when no conflict is detected.</returns>
  internal static string? Detect(string? aspNetCoreEnvironment, string? dotNetEnvironment)
  {
    if (string.IsNullOrEmpty(aspNetCoreEnvironment) || string.IsNullOrEmpty(dotNetEnvironment))
    {
      return null;
    }

    if (string.Equals(aspNetCoreEnvironment, dotNetEnvironment, StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return string.Format(
      System.Globalization.CultureInfo.InvariantCulture,
      ConflictFormat,
      aspNetCoreEnvironment,
      dotNetEnvironment);
  }
}