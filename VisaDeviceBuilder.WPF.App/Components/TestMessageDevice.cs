using System;
using System.Threading.Tasks;
using Ivi.Visa;

namespace VisaDeviceBuilder.WPF.App.Components
{
  /// <summary>
  ///   The test message-based VISA device.
  /// </summary>
  public class TestMessageDevice : MessageDevice
  {
    /// <summary>
    ///   Defines the communication delay in milliseconds imitated by this test device.
    /// </summary>
    public const int CommunicationDelay = 5;

    /// <summary>
    ///   The synchronization locking object.
    /// </summary>
    private readonly object _synchronizationLock = new object();

    /// <summary>
    ///   The actual value accessed by the <see cref="StringAsyncProperty" /> property.
    /// </summary>
    private string _stringAsyncPropertyValue = string.Empty;

    /// <summary>
    ///   The actual value accessed by the <see cref="IntegerAsyncProperty" /> property.
    /// </summary>
    private int _integerAsyncPropertyValue = 0;

    /// <summary>
    ///   The actual value accessed by the <see cref="FloatingPointAsyncProperty" /> property.
    /// </summary>
    private double _floatingPointAsyncPropertyValue = 0F;

    /// <summary>
    ///   The actual value accessed by the <see cref="GetOnlyAsyncProperty" /> and <see cref="SetOnlyAsyncProperty" />
    ///   properties.
    /// </summary>
    private string _singleAccessorAsyncPropertyValue = "";

    /// <summary>
    ///   The random number generator used by the <see cref="RandomAsyncProperty" /> property.
    /// </summary>
    private Random _random = new Random();

    /// <summary>
    ///   The backing field for the <see cref="StringAsyncProperty" /> property.
    /// </summary>
    private IAsyncProperty<string>? _stringAsyncProperty;

    /// <summary>
    ///   The backing field for the <see cref="IntegerAsyncProperty" /> property.
    /// </summary>
    private IAsyncProperty<int>? _integerAsyncProperty;

    /// <summary>
    ///   The backing field for the <see cref="FloatingPointAsyncProperty" /> property.
    /// </summary>
    private IAsyncProperty<double>? _floatingPointAsyncProperty;

    /// <summary>
    ///   The backing field for the <see cref="SetOnlyAsyncProperty" /> property.
    /// </summary>
    private IAsyncProperty<string>? _setOnlyAsyncProperty;

    /// <summary>
    ///   The backing field for the <see cref="GetOnlyAsyncProperty" /> property.
    /// </summary>
    private IAsyncProperty<string>? _getOnlyAsyncProperty;

    /// <summary>
    ///   The backing field for the <see cref="RandomAsyncProperty" /> property.
    /// </summary>
    private IAsyncProperty<double>? _randomAsyncProperty;

    /// <summary>
    ///   Gets the test asynchronous property of a string type.
    /// </summary>
    public IAsyncProperty StringAsyncProperty => _stringAsyncProperty ??= new AsyncProperty<string>(() =>
    {
      lock (_synchronizationLock)
      {
        Task.Delay(CommunicationDelay).Wait();
        return _stringAsyncPropertyValue;
      }
    }, newValue =>
    {
      lock (_synchronizationLock)
      {
        Task.Delay(CommunicationDelay).Wait();
        _stringAsyncPropertyValue = newValue;
      }
    });

    /// <summary>
    ///   Gets the get-only test asynchronous property of an integer type.
    /// </summary>
    public IAsyncProperty IntegerAsyncProperty => _integerAsyncProperty ??= new AsyncProperty<int>(() =>
    {
      lock (_synchronizationLock)
      {
        Task.Delay(CommunicationDelay).Wait();
        return _integerAsyncPropertyValue;
      }
    }, newValue =>
    {
      lock (_synchronizationLock)
      {
        Task.Delay(CommunicationDelay).Wait();
        _integerAsyncPropertyValue = newValue;
      }
    });

    /// <summary>
    ///   Gets the get-only test asynchronous property of a double-precision floating point type.
    /// </summary>
    public IAsyncProperty FloatingPointAsyncProperty => _floatingPointAsyncProperty ??= new AsyncProperty<double>(() =>
    {
      lock (_synchronizationLock)
      {
        Task.Delay(CommunicationDelay).Wait();
        return _floatingPointAsyncPropertyValue;
      }
    }, newValue =>
    {
      lock (_synchronizationLock)
      {
        Task.Delay(CommunicationDelay).Wait();
        _floatingPointAsyncPropertyValue = newValue;
      }
    });

    /// <summary>
    ///   Gets the set-only test asynchronous property of a string type.
    /// </summary>
    public IAsyncProperty SetOnlyAsyncProperty => _setOnlyAsyncProperty ??= new AsyncProperty<string>(newValue =>
    {
      lock (_synchronizationLock)
      {
        Task.Delay(CommunicationDelay).Wait();
        _singleAccessorAsyncPropertyValue = newValue;
      }
    });

    /// <summary>
    ///   Gets the set-only test asynchronous property of a string type.
    /// </summary>
    public IAsyncProperty GetOnlyAsyncProperty => _getOnlyAsyncProperty ??= new AsyncProperty<string>(() =>
    {
      lock (_synchronizationLock)
      {
        Task.Delay(CommunicationDelay).Wait();
        return _singleAccessorAsyncPropertyValue;
      }
    });

    /// <summary>
    ///   Gets the set-only test asynchronous property of a string type.
    /// </summary>
    public IAsyncProperty RandomAsyncProperty => _randomAsyncProperty ??= new AsyncProperty<double>(() =>
    {
      lock (_synchronizationLock)
      {
        Task.Delay(CommunicationDelay).Wait();
        return _random.NextDouble();
      }
    });

    /// <inheritdoc />
    public TestMessageDevice(string resourceName, IResourceManager? resourceManager = null) :
      base(resourceName, resourceManager)
    {
    }

    /// <summary>
    ///   Defines the device's test asynchronous action.
    /// </summary>
    [AsyncAction]
    public Task TestAsyncAction() => Task.Delay(CommunicationDelay);
  }
}
