using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;

namespace Hive.Diagnostics;

/// <summary>
/// Diagnostic listener for middleware
/// </summary>
public class MiddlewareDiagnosticListener
{
  /// <summary>
  /// A middleware is starting
  /// </summary>
  /// <param name="httpContext"></param>
  /// <param name="name"></param>
  [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting")]
  public virtual void OnMiddlewareStarting(HttpContext httpContext, string name)
  {
    Console.WriteLine($"MiddlewareStarting: {name}; {httpContext.Request.Path}");
  }

  /// <summary>
  /// A middleware has thrown an exception
  /// </summary>
  /// <param name="exception"></param>
  /// <param name="name"></param>
  [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException")]
  public virtual void OnMiddlewareException(Exception exception, string name)
  {
    Console.WriteLine($"MiddlewareException: {name}; {exception.Message}");
  }

  /// <summary>
  /// A middleware has finished executing
  /// </summary>
  /// <param name="httpContext"></param>
  /// <param name="name"></param>
  [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareFinished")]
  public virtual void OnMiddlewareFinished(HttpContext httpContext, string name)
  {
    Console.WriteLine($"MiddlewareFinished: {name}; {httpContext.Response.StatusCode}");
  }
}