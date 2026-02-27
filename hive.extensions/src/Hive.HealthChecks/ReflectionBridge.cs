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
    => (string)typeof(Invoker<>).MakeGenericType(healthCheckType)
      .GetMethod(GetNameMethod)!.Invoke(null, null)!;

  public static void InvokeConfigureDefaults(Type healthCheckType, HiveHealthCheckOptions options)
    => typeof(Invoker<>).MakeGenericType(healthCheckType)
      .GetMethod(ConfigureMethod)!.Invoke(null, [options]);

  private static class Invoker<T> where T : IHiveHealthCheck
  {
    public static string GetName() => T.CheckName;
    public static void Configure(HiveHealthCheckOptions o) => T.ConfigureDefaults(o);
  }
}
