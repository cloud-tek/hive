using System.Text;

namespace Hive.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string @str)
    {
        return string.IsNullOrEmpty(@str);
    }

    public static bool IsNotNullOrEmpty(this string @str)
    {
        return !string.IsNullOrEmpty(@str);
    }

    public static bool DoesNotContain(this string @str, params string[] values)
    {
        if (values == null || values.Length == 0) throw new InvalidOperationException(nameof(values));
        return !values.Any(@str.Contains);
    }

    public static bool IsDevelopment(this string @str)
    {
        return Environments.Development.Any(env => @str.Equals(env, StringComparison.InvariantCultureIgnoreCase));
    }

    public static bool IsUat(this string @str)
    {
        return Environments.Test.Any(env => @str.Equals(env, StringComparison.InvariantCultureIgnoreCase));
    }

    public static bool IsProduction(this string @str)
    {
        return Environments.Production.Any(env => @str.Equals(env, StringComparison.InvariantCultureIgnoreCase));
    }

    public static string ToMultilineString(this string[] strings)
    {
      {
        var sb = new StringBuilder();

        foreach (var str in strings)
        {
          sb.AppendLine(str);
        }

        return sb.ToString();
      }
    }

    private static class Environments
    {
        public static string[] Development = new string[]
        {
            "dev",
            "development"
        };

        public static string[] Test = new string[]
        {
            "uat",
            "test"
        };

        public static string[] Production = new string[]
        {
            "xyz",
            "prod",
            "production"
        };
    }
}
