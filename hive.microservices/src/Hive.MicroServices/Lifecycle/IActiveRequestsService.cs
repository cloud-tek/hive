namespace Hive.MicroServices.Lifecycle;

/// <summary>
/// An interface used to decorate an IHostedService which is used to keep track of active requests
/// </summary>
public interface IActiveRequestsService
{
  /// <summary>
  /// The current count of active requests
  /// </summary>
  long Counter { get; }

  /// <summary>
  /// Indicates if there are any active requests
  /// </summary>
  bool HasActiveRequests { get; }

  /// <summary>
  /// Decrements the active request count
  /// </summary>
  void Decrement();

  /// <summary>
  /// Increments the active request count
  /// </summary>
  void Increment();
}