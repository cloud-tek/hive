using System.Runtime.InteropServices;
using System.Text;
using Hive.Extensions;

namespace Hive.Testing
{
  internal static class TestExecutionResolver
  {
    internal static IDictionary<Execute, string> SkipReasonForExecute = new Dictionary<Execute, string>()
        {
            { Execute.InGithubActions, "Test to be executed only in Github Actions." },
            { Execute.InAzureDevOps, "Test to be executed only in Azure DevOps." },
            { Execute.InContainer, "Test to be executed only in a .NET Core Container." },
            { Execute.InDebug, "Test to be executed only in DEBUG configuration." }
        };

    internal static IDictionary<On, string> SkipReasonForOn = new Dictionary<On, string>()
        {
            { On.Windows, "Test to be executed on Windows." },
            { On.Linux, "Test to be executed on Linux." },
            { On.MacOS, "Test to be executed on MacOS." },
        };

    private static readonly string[] AzureDevOpsEnvVariables = new[] { "AGENT_ID", "BUILD_BUILDID" };
    private static readonly string[] GitHubActionsEnvVariables = new[] { "GITHUB_ACTIONS" };

    public static string? Resolve(Execute execute, On on, IEnumerable<string> environment = null!)
    {
      var errors = new string[]
      {
        Resolve(execute)!,
        Resolve(on)!,
        (environment != null ? Resolve(environment) : null)!
      };

      var count = errors.Count(e => e != null);

      if (count > 1)
      {
        var result = new StringBuilder();

        errors.Where(e => e != null).ForEach(e =>
        {
          result.Append(e);

          if (e != errors.Last(e => e != null))
          {
            result.Append(" & ");
          }
        });

        return result.ToString();
      }
      else if (count == 1)
      {
        return errors.Single(e => e != null);
      }

      return null;
    }

    internal static string? Resolve(Execute execute)
    {
      if ((Execute.InGithubActions & execute) != 0)
      {
        var err = ValidateEnvVariablesExists(GitHubActionsEnvVariables, () => SkipReasonForExecute[Execute.InGithubActions]);
        if (err != null)
          return err;
      }

      if ((Execute.InAzureDevOps & execute) != 0)
      {
        var err = ValidateEnvVariablesExists(AzureDevOpsEnvVariables, () => SkipReasonForExecute[Execute.InAzureDevOps]);
        if (err != null)
          return err;
      }

      if ((Execute.InContainer & execute) != 0)
      {
        var err = ValidateEnvVariableValue(
          "DOTNET_RUNNING_IN_CONTAINER",
          "true",
          () => SkipReasonForExecute[Execute.InContainer]);

        if (err != null)
          return err;
      }

      if ((Execute.InDebug & execute) != 0)
      {
#if DEBUG
#else
        return SkipReasonForExecute[Execute.InDebug];
#endif
      }

      return null;
    }

    internal static string? Resolve(On on)
    {
      if ((On.Windows & on) == On.Windows && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        return SkipReasonForOn[On.Windows];
      }

      if ((On.Linux & on) == On.Linux && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        return SkipReasonForOn[On.Linux];
      }

      if ((On.MacOS & on) == On.MacOS && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        return SkipReasonForOn[On.MacOS];
      }

      return null;
    }

    internal static string? Resolve(IEnumerable<string> environment)
    {
      return ValidateEnvVariablesExists(environment, () => "A required environment variable is missing");
    }

    private static string? ValidateEnvVariablesExists(IEnumerable<string> names, Func<string> errorSelector)
    {
      foreach (var name in names)
      {
        var val = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(val))
        {
          return errorSelector();
        }
      }

      return null;
    }

    private static string? ValidateEnvVariableValue(string name, string value, Func<string> errorSelector)
    {
      var val = Environment.GetEnvironmentVariable(name);
      if (string.IsNullOrEmpty(val) || val.Equals(value, StringComparison.OrdinalIgnoreCase))
      {
        return SkipReasonForExecute[Execute.InContainer];
      }

      return null;
    }
  }
}