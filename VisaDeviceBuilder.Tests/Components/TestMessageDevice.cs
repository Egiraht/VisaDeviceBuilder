using System;
using System.Threading.Tasks;
using Ivi.Visa;

namespace VisaDeviceBuilder.Tests.Components
{
  /// <summary>
  ///   The test message-based VISA device used for testing purposes.
  /// </summary>
  public class TestMessageDevice : MessageDevice
  {
    /// <summary>
    ///   The actual value accessed by the <see cref="TestAsyncProperty" /> property.
    /// </summary>
    private int _value;

    /// <summary>
    ///   The backing field for the <see cref="TestAsyncProperty" /> property.
    /// </summary>
    private IAsyncProperty<int>? _testAsyncProperty;

    /// <summary>
    ///   Gets the test asynchronous property of integer type.
    /// </summary>
    public IAsyncProperty<int> TestAsyncProperty =>
      _testAsyncProperty ??= new AsyncProperty<int>(() => _value, newValue => _value = newValue);

    /// <summary>
    ///   Gets or sets the flag defining if a test exception should be thrown on the device initialization.
    /// </summary>
    public bool ThrowOnInitialization { get; set; } = false;

    /// <summary>
    ///   Gets or sets the flag defining if a test exception should be thrown on the device de-initialization.
    /// </summary>
    public bool ThrowOnDeInitialization { get; set; } = false;

    /// <inheritdoc />
    public TestMessageDevice(string resourceName, int connectionTimeout = DefaultConnectionTimeout,
      IResourceManager? resourceManager = null) : base(resourceName, connectionTimeout, resourceManager)
    {
    }

    /// <inheritdoc />
    public override Task InitializeAsync() => ThrowOnInitialization
      ? throw new Exception("Test exception")
      : Task.CompletedTask;

    /// <inheritdoc />
    public override Task DeInitializeAsync() => ThrowOnDeInitialization
      ? throw new Exception("Test exception")
      : Task.CompletedTask;
  }
}
