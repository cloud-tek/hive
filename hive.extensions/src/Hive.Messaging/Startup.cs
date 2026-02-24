namespace Hive.Messaging;

public static class Startup
{
  public static IMicroService WithMessaging(
    this IMicroService service,
    Action<HiveMessagingBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(service);
    ArgumentNullException.ThrowIfNull(configure);

    GuardDuplicateRegistration(service);

    var extension = new MessagingExtension(service, configure);
    service.Extensions.Add(extension);
    return service;
  }

  public static IMicroServiceCore WithMessaging(
    this IMicroServiceCore service,
    Action<HiveMessagingSendBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(service);
    ArgumentNullException.ThrowIfNull(configure);

    GuardDuplicateRegistration(service);

    var extension = new MessagingSendExtension(service, configure);
    service.Extensions.Add(extension);
    return service;
  }

  private static void GuardDuplicateRegistration(IMicroServiceCore service)
  {
    var hasMessaging = service.Extensions
      .Any(e => e is MessagingExtension or MessagingSendExtension);

    if (hasMessaging)
    {
      throw new InvalidOperationException(
        "WithMessaging() has already been called on this service. " +
        "Wolverine supports only one host per process. " +
        "Combine all messaging configuration into a single WithMessaging() call.");
    }
  }
}
