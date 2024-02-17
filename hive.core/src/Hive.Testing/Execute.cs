namespace Hive.Testing;

/// <summary>
/// Represents the execution context in which the test should be executed
/// </summary>
[Flags]
public enum Execute
{
  /// <summary>
  /// Always execute the test
  /// </summary>
  Always = 0,

  /// <summary>
  /// Execute the test only in GitHub Actions
  /// </summary>
  InGithubActions = 1,

  /// <summary>
  /// Execute the test only in Azure DevOps
  /// </summary>
  InAzureDevOps = 2,

  /// <summary>
  /// Execute the test only in a container
  /// </summary>
  InContainer = 3,

  /// <summary>
  /// Execute the test only in debug mode
  /// </summary>
  InDebug = 4
}