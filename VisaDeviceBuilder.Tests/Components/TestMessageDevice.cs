using System;
using System.Diagnostics.CodeAnalysis;
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
    ///   Defines the communication delay in milliseconds imitated by this test device.
    /// </summary>
    public const int CommunicationDelay = 1;

    /// <summary>
    ///   Defines the test exception text.
    /// </summary>
    public const string TestExceptionText = "Test exception";

    /// <summary>
    ///   The synchronization locking object.
    /// </summary>
    private readonly object _synchronizationLock = new();

    /// <summary>
    ///   The actual value accessed by the <see cref="TestAsyncProperty" /> property.
    /// </summary>
    private int _value;

    /// <summary>
    ///   The backing field for the <see cref="TestAsyncProperty" /> property.
    /// </summary>
    private IAsyncProperty<int>? _testAsyncProperty;

    /// <summary>
    ///   Gets the test asynchronous property of integer type that must be enlisted into the
    ///   <see cref="IVisaDevice.AsyncProperties" /> dictionary.
    /// </summary>
    public IAsyncProperty<int> TestAsyncProperty => _testAsyncProperty ??= new AsyncProperty<int>(() =>
    {
      lock (_synchronizationLock)
      {
        if (ThrowOnAsyncPropertyGetter)
          throw new Exception(TestExceptionText);

        Task.Delay(CommunicationDelay).Wait();
        return _value;
      }
    }, newValue =>
    {
      lock (_synchronizationLock)
      {
        Task.Delay(CommunicationDelay).Wait();
        _value = newValue;
      }
    });

    /// <summary>
    ///   Gets or sets the flag defining if a test exception should be thrown on the device initialization.
    /// </summary>
    public bool ThrowOnInitialization { get; set; } = false;

    /// <summary>
    ///   Gets or sets the flag defining if a test exception should be thrown on the device de-initialization.
    /// </summary>
    public bool ThrowOnDeInitialization { get; set; } = false;

    /// <summary>
    ///   Gets or sets the flag defining if a test exception should be thrown during <see cref="TestAsyncProperty" />
    ///   getter processing.
    /// </summary>
    public bool ThrowOnAsyncPropertyGetter { get; set; } = false;

    /// <inheritdoc />
    public TestMessageDevice(string resourceName, IResourceManager? resourceManager = null) :
      base(resourceName, resourceManager)
    {
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
      if (ThrowOnInitialization)
        throw new Exception(TestExceptionText);
    }

    /// <inheritdoc />
    protected override void DeInitialize()
    {
      if (ThrowOnDeInitialization)
        throw new Exception(TestExceptionText);
    }

    /// <summary>
    ///   Defines the valid device action that must be enlisted into the <see cref="IVisaDevice.DeviceActions" />
    ///   dictionary.
    /// </summary>
    [DeviceAction, ExcludeFromCodeCoverage]
    public void TestDeviceAction()
    {
    }

    /// <summary>
    ///   Defines the invalid device action that must not be enlisted into the <see cref="IVisaDevice.DeviceActions" />
    ///   dictionary because it does not match the <see cref="Action" /> delegate signature.
    /// </summary>
    [DeviceAction, ExcludeFromCodeCoverage]
    public string InvalidDeviceAction() => string.Empty;
  }
}
