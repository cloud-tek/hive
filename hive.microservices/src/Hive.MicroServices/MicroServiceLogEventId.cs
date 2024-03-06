namespace Hive.MicroServices;

/// <summary>
/// The microservice's log event id(s)
/// </summary>
public enum MicroServiceLogEventId
{
  /// <summary>
  /// No event id.
  /// </summary>
  None = 0,

  /* Service Lifecycle Events */

  /// <summary>
  /// The service has started successfully
  /// </summary>
  ServiceStarted = 100,

  /// <summary>
  /// The service is stopping
  /// </summary>
  ServiceStopping = 101,

  /// <summary>
  /// The service has been drained successfully
  /// </summary>
  ServiceDrainedHTTP = 102,

  /// <summary>
  /// The service has been drained successfully
  /// </summary>
  ServiceDrainedFailedHTTP = 103,

  /// <summary>
  /// The service has remaining messages to be drained
  /// </summary>
  ServiceDrainRemainingHTTP = 104,

  /// <summary>
  /// The service has failed to start due to a critical failure
  /// </summary>
  ServiceStartupCriticalFailure = 110,

  /// <summary>
  /// A hosted startup service has completed
  /// </summary>
  HostedStartupServiceCompleted = 120,

  /// <summary>
  /// A hosted startup service has started
  /// </summary>
  JobHostedServiceStarted = 200,

  /// <summary>
  /// A hosted job is stopping
  /// </summary>
  JobHostedServiceStopping = 201,

  /// <summary>
  /// A hosted job failed to complete at least 1 of the jobs
  /// </summary>
  JobHostedServiceErrorFailed = 202,

  /// <summary>
  /// A hosted job failed to start
  /// </summary>
  JobHostedServiceCriticalFailure = 210,

  /* Service Extension Events */

  /// <summary>
  /// An extension has successfully applied a piece of configuration
  /// </summary>
  ServiceExtensionConfigurationApplied = 1000,

  /// <summary>
  /// An extension has failed to start due to a critical failure
  /// </summary>
  ServiceExtensionCriticalFailure = 1001,

  /// <summary>
  /// Unhandled exception.
  /// </summary>
  UnhandledException = 10000
}