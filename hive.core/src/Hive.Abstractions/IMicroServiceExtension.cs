namespace Hive;

/// <summary>
/// Interface for creatable microservice extensions that can be registered via RegisterExtension.
/// Uses static abstract interface members (C# 11) to enforce compile-time safety for extension creation.
/// </summary>
/// <typeparam name="TExtension">The extension type itself (Curiously Recurring Template Pattern)</typeparam>
/// <remarks>
/// Extensions implementing this interface can be registered using the RegisterExtension method,
/// which provides compile-time validation that the extension can be properly instantiated.
///
/// Example:
/// <code>
/// public class MyExtension : MicroServiceExtension&lt;MyExtension&gt;
/// {
///     public MyExtension(IMicroServiceCore service) : base(service) { }
/// }
/// </code>
///
/// The base class MicroServiceExtension&lt;TExtension&gt; provides a default Create implementation.
/// </remarks>
public interface IMicroServiceExtension<TExtension> where TExtension : MicroServiceExtension<TExtension>
{
  /// <summary>
  /// Factory method to create an instance of the extension.
  /// This method is called by RegisterExtension to instantiate the extension.
  /// </summary>
  /// <param name="service">The microservice core instance</param>
  /// <returns>A new instance of the extension</returns>
  static abstract TExtension Create(IMicroServiceCore service);
}
