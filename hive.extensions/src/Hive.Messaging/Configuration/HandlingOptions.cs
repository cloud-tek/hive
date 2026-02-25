namespace Hive.Messaging.Configuration;

/// <summary>
/// Options controlling message handler behavior such as prefetch and listener counts.
/// </summary>
public class HandlingOptions
{
  /// <summary>
  /// Number of messages to prefetch from the broker. Null uses the transport default.
  /// </summary>
  public int? PrefetchCount { get; set; }

  /// <summary>
  /// Number of concurrent listeners for a queue. Null uses the transport default.
  /// </summary>
  public int? ListenerCount { get; set; }
}
