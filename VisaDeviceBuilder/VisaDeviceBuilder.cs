using System;
using System.Linq;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   A builder class that can build VISA devices with custom behavior.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This builder class is intended for building VISA devices that require using custom non-message-based VISA
  ///     session implementations. For the most commonly used case of message-based device communication (e.g. SCPI
  ///     language-based communication), consider using the <see cref="MessageDeviceBuilder" /> class instead of this
  ///     one.
  ///   </para>
  ///   <para>
  ///     After a VISA device is built, the current builder instance cannot be reused. Create a new builder if
  ///     necessary.
  ///   </para>
  /// </remarks>
  public class VisaDeviceBuilder : IVisaDeviceBuilder
  {
    /// <summary>
    ///   The flag indicating if a device has been already built.
    /// </summary>
    private bool _isBuilt = false;

    /// <summary>
    ///   Gets the VISA device object being built by this builder instance.
    /// </summary>
    private IBuildableVisaDevice Device
    {
      get
      {
        ThrowWhenBuilderIsReused();
        return _device;
      }
    }
    private readonly IBuildableVisaDevice _device = new BuildableVisaDevice();

    /// <summary>
    ///   Throws an <see cref="InvalidOperationException" /> if a device has been already built using this builder
    ///   instance. This is important because further <see cref="Device" /> instance modification will also influence
    ///   the built instance.
    /// </summary>
    private void ThrowWhenBuilderIsReused()
    {
      if (_isBuilt)
        throw new InvalidOperationException("This builder instance cannot be reused after a device has been built.");
    }

    /// <summary>
    ///   Instructs the builder to use the <see cref="GlobalResourceManager" /> class as a default VISA resource manager
    ///   for VISA device session management.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder UseGlobalVisaResourceManager()
    {
      Device.ResourceManager = null;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified VISA resource manager object for VISA device session management.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder UseCustomVisaResourceManager(IResourceManager resourceManager)
    {
      Device.ResourceManager = resourceManager;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified VISA resource name when a VISA device instance will be built.
    /// </summary>
    /// <param name="resourceName">
    ///   The VISA resource name that can be resolved by the used VISA resource manager.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder UseDefaultResourceName(string resourceName)
    {
      Device.ResourceName = resourceName;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified VISA session connection timeout.
    /// </summary>
    /// <param name="timeout">
    ///   The VISA session connection timeout expressed in milliseconds.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder UseConnectionTimeout(int timeout)
    {
      Device.ConnectionTimeout = timeout;
      return this;
    }

    /// <summary>
    ///   Defines VISA hardware interfaces that the created VISA device can support.
    /// </summary>
    /// <param name="interfaces">
    ///   An array or parameter sequence of VISA hardware interfaces supported by the device.
    ///   If an empty array is provided, the default supported hardware interfaces (i.e. all available) will be used.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder DefineSupportedHardwareInterfaces(params HardwareInterfaceType[] interfaces)
    {
      Device.CustomSupportedInterfaces = interfaces.Distinct().ToArray();
      return this;
    }

    /// <summary>
    ///   Adds a read-only asynchronous property to the VISA device.
    /// </summary>
    /// <typeparam name="TValue">
    ///   The type of the asynchronous property.
    /// </typeparam>
    /// <param name="name">
    ///   The name of the asynchronous property.
    /// </param>
    /// <param name="getter">
    ///   A getter delegate for the read-only asynchronous property that reads and returns a value from the device
    ///   instance provided as a delegate parameter.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder AddReadOnlyAsyncProperty<TValue>(string name, Func<IVisaDevice, TValue> getter)
    {
      Device.CustomAsyncProperties.Add(new AsyncProperty<TValue>(() => getter.Invoke(Device)) {Name = name});
      return this;
    }

    /// <summary>
    ///   Adds a write-only asynchronous property to the VISA device.
    /// </summary>
    /// <typeparam name="TValue">
    ///   The type of the asynchronous property.
    /// </typeparam>
    /// <param name="name">
    ///   The name of the asynchronous property.
    /// </param>
    /// <param name="setter">
    ///   A setter delegate for the write-only asynchronous property that writes a value to the device instance. Both
    ///   objects are provided as delegate parameters.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder AddWriteOnlyAsyncProperty<TValue>(string name, Action<IVisaDevice, TValue> setter)
    {
      Device.CustomAsyncProperties.Add(new AsyncProperty<TValue>(value => setter.Invoke(Device, value)) {Name = name});
      return this;
    }

    /// <summary>
    ///   Adds a read-write asynchronous property to the VISA device.
    /// </summary>
    /// <typeparam name="TValue">
    ///   The type of the asynchronous property.
    /// </typeparam>
    /// <param name="name">
    ///   The name of the asynchronous property.
    /// </param>
    /// <param name="getter">
    ///   A getter delegate for the read-only asynchronous property that reads and returns a value from the device
    ///   instance provided as a delegate parameter.
    /// </param>
    /// <param name="setter">
    ///   A setter delegate for the write-only asynchronous property that writes a value to the device instance. Both
    ///   objects are provided as delegate parameters.
    /// </param>
    /// <param name="autoUpdateGetterAfterSetterCompletes">
    ///   Defines if the <paramref name="getter" /> delegate must be automatically called to update the property's value
    ///   after the <paramref name="setter" /> delegate is called and successfully completes its execution.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder AddReadWriteAsyncProperty<TValue>(string name, Func<IVisaDevice, TValue> getter,
      Action<IVisaDevice, TValue> setter, bool autoUpdateGetterAfterSetterCompletes = true)
    {
      Device.CustomAsyncProperties.Add(
        new AsyncProperty<TValue>(() => getter.Invoke(Device), value => setter.Invoke(Device, value))
        {
          Name = name,
          AutoUpdateGetterAfterSetterCompletes = autoUpdateGetterAfterSetterCompletes
        });
      return this;
    }

    /// <summary>
    ///   Adds a device action to the VISA device.
    /// </summary>
    /// <param name="name">
    ///   The name of the device action.
    /// </param>
    /// <param name="action">
    ///   A device action delegate that performs an asynchronous operation for the device instance provided as a
    ///   delegate parameter.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder AddDeviceAction(string name, Action<IVisaDevice> action)
    {
      Device.CustomDeviceActions.Add(new DeviceAction(() => action.Invoke(Device)) {Name = name});
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified delegate as a device initialization stage callback.
    /// </summary>
    /// <param name="callback">
    ///   A delegate to be used as a device initialization stage callback.
    ///   As a parameter a VISA device object is provided.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder UseInitializeCallback(Action<IVisaDevice> callback)
    {
      Device.CustomInitializeCallback = callback;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified delegate as a device de-initialization stage callback.
    /// </summary>
    /// <param name="callback">
    ///   A delegate to be used as a device de-initialization stage callback.
    ///   As a parameter a VISA device object is provided.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder UseDeInitializeCallback(Action<IVisaDevice> callback)
    {
      Device.CustomDeInitializeCallback = callback;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified delegate for getting the device identifier string.
    /// </summary>
    /// <param name="callback">
    ///   A delegate to be used for getting the device identifier string.
    ///   As a parameter a VISA device object is provided.
    ///   The delegate must return the device identifier string.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder UseGetIdentifierCallback(Func<IVisaDevice, string> callback)
    {
      Device.CustomGetIdentifierCallback = callback;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified delegate to reset the device.
    /// </summary>
    /// <param name="callback">
    ///   A delegate to be used to reset the device.
    ///   As a parameter a VISA device object is provided.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder UseResetCallback(Action<IVisaDevice> callback)
    {
      Device.CustomResetCallback = callback;
      return this;
    }

    /// <summary>
    ///   Registers an <see cref="IDisposable" /> object for disposal when the created VISA device instance will be
    ///   disposed of.
    /// </summary>
    /// <param name="disposable">
    ///   An <see cref="IDisposable" /> object.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder RegisterDisposable(IDisposable disposable)
    {
      Device.CustomDisposables.Add(disposable);
      return this;
    }

    /// <summary>
    ///   Registers <see cref="IDisposable" /> objects for disposal when the created VISA device instance will be
    ///   disposed of.
    /// </summary>
    /// <param name="disposables">
    ///   An array or parameter sequence of <see cref="IDisposable" /> objects.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder RegisterDisposables(params IDisposable[] disposables)
    {
      Device.CustomDisposables.AddRange(disposables);
      return this;
    }

    /// <inheritdoc />
    public IVisaDevice BuildVisaDevice()
    {
      _isBuilt = true;
      return _device;
    }

    /// <inheritdoc />
    public IVisaDeviceController BuildVisaDeviceController() => new VisaDeviceController(BuildVisaDevice());
  }
}
