using System;
using System.Linq;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   A builder class that can build message-based VISA devices with custom behavior.
  /// </summary>
  /// <remarks>
  ///   After a VISA device is built, the current builder instance cannot be reused. Create a new builder if necessary.
  /// </remarks>
  public class MessageDeviceBuilder : IMessageDeviceBuilder
  {
    /// <summary>
    ///   The flag indicating if a device has been already built.
    /// </summary>
    private bool _isBuilt = false;

    /// <summary>
    ///   Gets the message-based VISA device object being built by this builder instance.
    /// </summary>
    private IBuildableMessageDevice Device
    {
      get
      {
        ThrowWhenBuilderIsReused();
        return _device;
      }
    }
    private IBuildableMessageDevice _device = new BuildableMessageDevice();

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
    public MessageDeviceBuilder UseGlobalVisaResourceManager()
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
    public MessageDeviceBuilder UseCustomVisaResourceManager(IResourceManager resourceManager)
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
    public MessageDeviceBuilder UseDefaultResourceName(string resourceName)
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
    public MessageDeviceBuilder UseConnectionTimeout(int timeout)
    {
      Device.ConnectionTimeout = timeout;
      return this;
    }

    /// <summary>
    ///   Defines VISA hardware interfaces that the created VISA device can support.
    /// </summary>
    /// <param name="interfaces">
    ///   An array or parameter sequence of VISA hardware interfaces supported by the device.
    ///   If an empty array is provided, the default supported message-based hardware interfaces will be used.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder DefineSupportedHardwareInterfaces(params HardwareInterfaceType[] interfaces)
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
    ///   A getter delegate for the read-only asynchronous property that reads and returns a value from the
    ///   message-based device instance provided as a delegate parameter.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For high-level message communication with the device use the <see cref="IMessageDevice.SendMessage" /> method.
    ///   Ensure that the necessary message processing delegate is provided using the builder's
    ///   <see cref="UseMessageProcessor" /> method.
    /// </remarks>
    /// <seealso cref="AsyncProperty{TValue}" />
    public MessageDeviceBuilder AddReadOnlyAsyncProperty<TValue>(string name, Func<IMessageDevice, TValue> getter)
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
    ///   A setter delegate for the write-only asynchronous property that writes a value to the message-based device
    ///   instance. Both objects are provided as delegate parameters.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For high-level message communication with the device use the <see cref="IMessageDevice.SendMessage" /> method.
    ///   Ensure that the necessary message processing delegate is provided using the builder's
    ///   <see cref="UseMessageProcessor" /> method.
    /// </remarks>
    /// <seealso cref="AsyncProperty{TValue}" />
    public MessageDeviceBuilder AddWriteOnlyAsyncProperty<TValue>(string name, Action<IMessageDevice, TValue> setter)
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
    ///   A getter delegate for the read-only asynchronous property that reads and returns a value from the
    ///   message-based device instance provided as a delegate parameter.
    /// </param>
    /// <param name="setter">
    ///   A setter delegate for the write-only asynchronous property that writes a value to the message-based device
    ///   instance. Both objects are provided as delegate parameters.
    /// </param>
    /// <param name="autoUpdateGetterAfterSetterCompletes">
    ///   Defines if the <paramref name="getter" /> delegate must be automatically called to update the property's value
    ///   after the <paramref name="setter" /> delegate is called and successfully completes its execution.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For high-level message communication with the device use the <see cref="IMessageDevice.SendMessage" /> method.
    ///   Ensure that the necessary message processing delegate is provided using the builder's
    ///   <see cref="UseMessageProcessor" /> method.
    /// </remarks>
    /// <seealso cref="AsyncProperty{TValue}" />
    public MessageDeviceBuilder AddReadWriteAsyncProperty<TValue>(string name, Func<IMessageDevice, TValue> getter,
      Action<IMessageDevice, TValue> setter, bool autoUpdateGetterAfterSetterCompletes = true)
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
    ///   For high-level message communication with the device use the <see cref="IMessageDevice.SendMessage" /> method.
    ///   Ensure that the necessary message processing delegate is provided using the builder's
    ///   <see cref="UseMessageProcessor" /> method.
    /// </remarks>
    /// <seealso cref="DeviceAction" />
    public MessageDeviceBuilder AddDeviceAction(string name, Action<IMessageDevice> action)
    {
      Device.CustomDeviceActions.Add(new DeviceAction(() => action.Invoke(Device)) {Name = name});
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified delegate as a device initialization stage callback.
    /// </summary>
    /// <param name="callback">
    ///   A delegate to be used as a device initialization stage callback.
    ///   As a parameter a message-based VISA device object is provided.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For high-level message communication with the device use the <see cref="IMessageDevice.SendMessage" /> method.
    ///   Ensure that the necessary message processing delegate is provided using the builder's
    ///   <see cref="UseMessageProcessor" /> method.
    /// </remarks>
    public MessageDeviceBuilder UseInitializeCallback(Action<IMessageDevice> callback)
    {
      Device.CustomInitializeCallback = callback;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified delegate as a device de-initialization stage callback.
    /// </summary>
    /// <param name="callback">
    ///   A delegate to be used as a device de-initialization stage callback.
    ///   As a parameter a message-based VISA device object is provided.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For high-level message communication with the device use the <see cref="IMessageDevice.SendMessage" /> method.
    ///   Ensure that the necessary message processing delegate is provided using the builder's
    ///   <see cref="UseMessageProcessor" /> method.
    /// </remarks>
    public MessageDeviceBuilder UseDeInitializeCallback(Action<IMessageDevice> callback)
    {
      Device.CustomDeInitializeCallback = callback;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified delegate for getting the device identifier string.
    /// </summary>
    /// <param name="callback">
    ///   A delegate to be used for getting the device identifier string.
    ///   As a parameter a message-based VISA device object is provided.
    ///   The delegate must return the device identifier string.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For high-level message communication with the device use the <see cref="IMessageDevice.SendMessage" /> method.
    ///   Ensure that the necessary message processing delegate is provided using the builder's
    ///   <see cref="UseMessageProcessor" /> method.
    /// </remarks>
    public MessageDeviceBuilder UseGetIdentifierCallback(Func<IMessageDevice, string> callback)
    {
      Device.CustomGetIdentifierCallback = callback;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified delegate to reset the device.
    /// </summary>
    /// <param name="callback">
    ///   A delegate to be used to reset the device.
    ///   As a parameter a message-based VISA device object is provided.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For high-level message communication with the device use the <see cref="IMessageDevice.SendMessage" /> method.
    ///   Ensure that the necessary message processing delegate is provided using the builder's
    ///   <see cref="UseMessageProcessor" /> method.
    /// </remarks>
    public MessageDeviceBuilder UseResetCallback(Action<IMessageDevice> callback)
    {
      Device.CustomResetCallback = callback;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified message processing delegate to handle request and response
    ///   messages.
    /// </summary>
    /// <param name="messageProcessor">
    ///   A delegate that will handle the common process of message-based communication with the device, and also
    ///   perform additional message checking and formatting if necessary.
    ///   The delegate is provided with a message-based VISA device object and a raw request message string as
    ///   parameters.
    ///   The function must return a processed response message string.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying message-based
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public MessageDeviceBuilder UseMessageProcessor(Func<IMessageDevice, string, string> messageProcessor)
    {
      Device.CustomMessageProcessor = messageProcessor;
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
    public MessageDeviceBuilder RegisterDisposable(IDisposable disposable)
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
    public MessageDeviceBuilder RegisterDisposables(params IDisposable[] disposables)
    {
      Device.CustomDisposables.AddRange(disposables);
      return this;
    }

    /// <inheritdoc />
    public IMessageDevice BuildVisaDevice()
    {
      _isBuilt = true;
      return _device;
    }

    /// <inheritdoc />
    IVisaDevice IVisaDeviceBuilder.BuildVisaDevice() => BuildVisaDevice();

    /// <inheritdoc />
    public IVisaDeviceController BuildVisaDeviceController() => new VisaDeviceController(BuildVisaDevice());
  }
}
