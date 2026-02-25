namespace Hive.Messaging;

/// <summary>
/// Extension methods for registering the Hive.Messaging extension on a microservice.
/// </summary>
public static class Startup
{
  /// <summary>
  /// Adds full messaging support (sending and handling) to the microservice.
  /// </summary>
  /// <param name="service">The microservice to configure.</param>
  /// <param name="configure">Action to configure the messaging builder.</param>
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

  /// <summary>
  /// Adds send-only messaging support to the microservice (no queue listeners).
  /// </summary>
  /// <param name="service">The microservice core to configure.</param>
  /// <param name="configure">Action to configure the send-only messaging builder.</param>
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
