namespace Hive;

/// <summary>
/// Constants used throughout the Hive.Abstractions namespace
/// </summary>
public static partial class Constants
{
  /// <summary>
  /// Constants related to environment variables
  /// </summary>
  public static class EnvironmentVariables
  {
    /// <summary>
    /// The HOSTNAME environment variable
    /// </summary>
    public const string Hostname = "HOSTNAME";

    /// <summary>
    /// The Environment environment variable
    /// </summary>
    public const string Environment = "Environment";

    /// <summary>
    /// Constants related to .NET environment variables
    /// </summary>
    public static class DotNet
    {
      /// <summary>
      /// The ASPNETCORE_ENVIRONMENT environment variable
      /// </summary>
      public const string Environment = "ASPNETCORE_ENVIRONMENT";

      /// <summary>
      /// The DOTNET_RUNNING_IN_CONTAINER environment variable
      /// </summary>
      public const string DotNetRunningInContainer = "DOTNET_RUNNING_IN_CONTAINER";
    }

    /// <summary>
    /// Constants related to Kubernetes environment variables
    /// </summary>
    public static class Kubernetes
    {
      /// <summary>
      /// The KUBERNETES environment variables' prefix
      /// </summary>
      public const string KubernetesVariablePrefix = "KUBERNETES";
    }
  }
}