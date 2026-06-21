namespace Hive
{
  public static partial class Constants
  {
    /// <summary>
    /// Constants related to errors
    /// </summary>
    public static class Errors
    {
      /// <summary>
      /// The error message when no request processing pipeline has been configured
      /// </summary>
      public const string PipelineNotSet = "No pipeline has been configured. Aborting";

      /// <summary>
      /// The error message format when ASPNETCORE_ENVIRONMENT and DOTNET_ENVIRONMENT are both set to conflicting values.
      /// {0} = ASPNETCORE_ENVIRONMENT value, {1} = DOTNET_ENVIRONMENT value.
      /// </summary>
      public const string EnvironmentVariableConflict =
        "Environment variable conflict: ASPNETCORE_ENVIRONMENT='{0}' and DOTNET_ENVIRONMENT='{1}' are set to different values. " +
        "Hive honors ASPNETCORE_ENVIRONMENT for configuration loading. Resolve the conflict before starting the service.";
    }
  }
}