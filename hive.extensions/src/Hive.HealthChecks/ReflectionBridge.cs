namespace Hive.HealthChecks;

/// <summary>
/// Bridges runtime <see cref="Type"/> objects to static abstract member invocation
/// on <see cref="IHiveHealthCheck"/>. Called once per discovered type during ConfigureServices.
/// </summary>
internal static class ReflectionBridge
{
  private const string GetNameMethod = "GetName";
  private const string ConfigureMethod = "Configure";

  public static string GetCheckName(Type healthCheckType)
  {
    try
    {
      return (string)typeof(Invoker<>).MakeGenericType(healthCheckType)
        .GetMethod(GetNameMethod)!.Invoke(null, null)!;
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException(
        $"Type '{healthCheckType.FullName}' must implement IHiveHealthCheck with " +
        $"static abstract members 'CheckName' and 'ConfigureDefaults'.", ex);
    }
  }

  public static void InvokeConfigureDefaults(Type healthCheckType, HiveHealthCheckOptions options)
  {
    try
    {
      typeof(Invoker<>).MakeGenericType(healthCheckType)
        .GetMethod(ConfigureMethod)!.Invoke(null, [options]);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException(
        $"Type '{healthCheckType.FullName}' must implement IHiveHealthCheck with " +
        $"static abstract members 'CheckName' and 'ConfigureDefaults'.", ex);
    }
  }

  private static class Invoker<T> where T : IHiveHealthCheck
  {
    public static string GetName() => T.CheckName;
    public static void Configure(HiveHealthCheckOptions o) => T.ConfigureDefaults(o);
  }
}