using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;

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
    ///   The synchronization locking object.
    /// </summary>
    private readonly object _synchronizationLock = new();

    /// <summary>
    ///   Gets or sets the actual value accessed by the <see cref="TestAsyncProperty" /> property.
    /// </summary>
    public int TestValue { get; set; }

    /// <summary>
    ///   Gets the test asynchronous property of integer type that is defined using the <see cref="IAsyncProperty" />
    ///   type class declaration and must be enlisted into the <see cref="IVisaDevice.AsyncProperties" /> enumeration.
    /// </summary>
    public IAsyncProperty<int> TestAsyncProperty => _testAsyncProperty ??= new AsyncProperty<int>(() =>
    {
      lock (_synchronizationLock)
      {
        if (ThrowOnAsyncPropertyGetter)
          throw new TestException();

        Task.Delay(CommunicationDelay).Wait();
        return TestValue;
      }
    }, newValue =>
    {
      lock (_synchronizationLock)
      {
        if (ThrowOnAsyncPropertySetter)
          throw new TestException();

        Task.Delay(CommunicationDelay).Wait();
        TestValue = newValue;
      }
    });
    private IAsyncProperty<int>? _testAsyncProperty;

    /// <summary>
    ///   Gets the device action that is defined using the <see cref="IDeviceAction" /> type class declaration and
    ///   must be enlisted into the <see cref="IVisaDevice.DeviceActions" /> enumeration.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public IDeviceAction DeclaredTestDeviceAction => _testDeclaredDeviceAction ??= new DeviceAction(() => { });
    private IDeviceAction? _testDeclaredDeviceAction;

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
    ///   getter updating.
    /// </summary>
    public bool ThrowOnAsyncPropertyGetter { get; set; } = false;

    /// <summary>
    ///   Gets or sets the flag defining if a test exception should be thrown during <see cref="TestAsyncProperty" />
    ///   setter processing.
    /// </summary>
    public bool ThrowOnAsyncPropertySetter { get; set; } = false;

    /// <inheritdoc />
    protected override void Initialize()
    {
      if (ThrowOnInitialization)
        throw new TestException();
    }

    /// <inheritdoc />
    protected override void DeInitialize()
    {
      if (ThrowOnDeInitialization)
        throw new TestException();
    }

    /// <summary>
    ///   Defines the device action that is defined using the <see cref="DeviceActionAttribute" /> and must be enlisted
    ///   into the <see cref="IVisaDevice.DeviceActions" /> enumeration.
    /// </summary>
    [DeviceAction, ExcludeFromCodeCoverage]
    public void DecoratedTestDeviceAction()
    {
    }
  }
}
