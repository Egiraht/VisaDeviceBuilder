using System;
using System.Linq;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   A builder class that can build message-based VISA devices with custom behavior.
  /// </summary>
  public class MessageDeviceBuilder : IVisaDeviceBuilder<IMessageDevice>
  {
    /// <summary>
    ///   The base buildable message-based VISA device instance that stores the builder configuration and is used for
    ///   building of new VISA device instances by cloning.
    /// </summary>
    private readonly IBuildableMessageDevice<IMessageDevice> _device = new MessageDevice();

    /// <summary>
    ///   Initializes a new message-based VISA device builder instance.
    /// </summary>
    public MessageDeviceBuilder()
    {
    }

    /// <summary>
    ///   Initializes a new VISA device builder instance with building configuration copied from a compatible buildable
    ///   VISA device instance.
    /// </summary>
    /// <param name="baseMessageDevice">
    ///   A base message-based VISA device instance to copy configuration from. This instance must derive from the
    ///   <see cref="MessageDevice" /> class or must implement the <see cref="IBuildableVisaDevice{TVisaDevice}" />
    ///   interface where TVisaDevice = <see cref="IMessageDevice" />.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///   Cannot copy building configuration from the provided VISA device instance because it does not implement the
    ///   <see cref="IBuildableVisaDevice{TVisaDevice}" /> interface where TVisaDevice = <see cref="IMessageDevice" />.
    /// </exception>
    public MessageDeviceBuilder(IMessageDevice baseMessageDevice)
    {
      if (baseMessageDevice is not IBuildableMessageDevice<IMessageDevice> buildableMessageDevice)
        throw new InvalidOperationException(
          "Cannot copy building configuration from the provided VISA device instance of type " +
          $"\"{baseMessageDevice.GetType().Name}\" because it does not implement the " +
          $"\"{typeof(IBuildableVisaDevice<IMessageDevice>).Name}\" interface.");

      _device = (IBuildableMessageDevice<IMessageDevice>) buildableMessageDevice.Clone();
    }

    /// <summary>
    ///   Instructs the builder to use the <see cref="GlobalResourceManager" /> class for VISA device session
    ///   management.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder UseGlobalVisaResourceManager()
    {
      _device.ResourceManager?.Dispose();
      _device.ResourceManager = null;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified VISA resource manager type for VISA device session management.
    /// </summary>
    /// <param name="resourceManagerType">
    ///   The type of a VISA resource manager to use.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder UseCustomVisaResourceManagerType(Type resourceManagerType)
    {
      if (resourceManagerType.GetInterface(nameof(IResourceManager)) == null)
        throw new InvalidOperationException(
          $"\"{resourceManagerType.Name}\" is not a valid VISA resource manager type.");

      _device.ResourceManager?.Dispose();
      _device.ResourceManager = (IResourceManager) Activator.CreateInstance(resourceManagerType)!;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to use the specified VISA resource manager type for VISA device session management.
    /// </summary>
    /// <typeparam name="TResourceManager">
    ///   The type of a VISA resource manager to use.
    /// </typeparam>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder UseCustomVisaResourceManagerType<TResourceManager>()
      where TResourceManager : IResourceManager =>
      UseCustomVisaResourceManagerType(typeof(TResourceManager));

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
      _device.ResourceName = resourceName;
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
      _device.ConnectionTimeout = timeout;
      return this;
    }

    /// <summary>
    ///   Instructs the builder that the VISA device being built supports the default hardware interfaces, defined for
    ///   the <see cref="MessageDevice" /> class (<see cref="MessageDevice.MessageBasedHardwareInterfaceTypes" />).
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder UseDefaultSupportedHardwareInterfaces()
    {
      _device.CustomSupportedInterfaces = null;
      return this;
    }

    /// <summary>
    ///   Instructs the builder that the VISA device being built supports the specified hardware interfaces.
    /// </summary>
    /// <param name="interfaces">
    ///   An array or parameter sequence of VISA hardware interfaces supported by the device.
    ///   If an empty array is provided, the default supported hardware interfaces (i.e. all available) will be used.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder UseSupportedHardwareInterfaces(params HardwareInterfaceType[] interfaces)
    {
      _device.CustomSupportedInterfaces = interfaces.Distinct().ToArray();
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
    ///   Adding multiple asynchronous properties with the same name is not forbidden but highly not recommended.
    /// </param>
    /// <param name="getter">
    ///   A getter delegate to be called when the asynchronous property is read.
    ///   The delegate may accept a nullable message-based VISA device instance (the one to be built) as a parameter,
    ///   or just reject it if it is not required for functioning.
    ///   The delegate must return a stored property's value of type <typeparamref name="TValue" />.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public MessageDeviceBuilder AddReadOnlyAsyncProperty<TValue>(string name, Func<IMessageDevice?, TValue> getter)
    {
      _device.CustomAsyncProperties
        .Add(new AsyncProperty<TValue>(visaDevice => getter.Invoke(visaDevice as IMessageDevice))
        {
          TargetDevice = _device,
          Name = name,
          AutoUpdateGetterAfterSetterCompletes = false
        });
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
    ///   Adding multiple asynchronous properties with the same name is not forbidden but highly not recommended.
    /// </param>
    /// <param name="setter">
    ///   A setter delegate to be called when the asynchronous property is written.
    ///   The delegate may accept a nullable message-based VISA device instance (the one to be built) as the first
    ///   parameter, or just reject it if it is not required for functioning.
    ///   As the second parameter the delegate is provided with a new property's value of type
    ///   <typeparamref name="TValue" />.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public MessageDeviceBuilder AddWriteOnlyAsyncProperty<TValue>(string name, Action<IMessageDevice?, TValue> setter)
    {
      _device.CustomAsyncProperties
        .Add(new AsyncProperty<TValue>((visaDevice, value) => setter.Invoke(visaDevice as IMessageDevice, value))
        {
          TargetDevice = _device,
          Name = name,
          AutoUpdateGetterAfterSetterCompletes = false
        });
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
    ///   Adding multiple asynchronous properties with the same name is not forbidden but highly not recommended.
    /// </param>
    /// <param name="getter">
    ///   A getter delegate to be called when the asynchronous property is read.
    ///   The delegate may accept a nullable message-based VISA device instance (the one to be built) as a parameter,
    ///   or just reject it if it is not required for functioning.
    ///   The delegate must return a stored property's value of type <typeparamref name="TValue" />.
    /// </param>
    /// <param name="setter">
    ///   A setter delegate to be called when the asynchronous property is written.
    ///   The delegate may accept a nullable message-based VISA device instance (the one to be built) as the first
    ///   parameter, or just reject it if it is not required for functioning.
    ///   As the second parameter the delegate is provided with a new property's value of type
    ///   <typeparamref name="TValue" />.
    /// </param>
    /// <param name="autoUpdateGetter">
    ///   Defines if the property's getter must be automatically updated every time a new value is successfully
    ///   processed by the property's setter.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public MessageDeviceBuilder AddReadWriteAsyncProperty<TValue>(string name, Func<IMessageDevice?, TValue> getter,
      Action<IMessageDevice?, TValue> setter, bool autoUpdateGetter = true)
    {
      _device.CustomAsyncProperties
        .Add(new AsyncProperty<TValue>(visaDevice => getter.Invoke(visaDevice as IMessageDevice),
          (visaDevice, value) => setter.Invoke(visaDevice as IMessageDevice, value))
        {
          TargetDevice = _device,
          Name = name,
          AutoUpdateGetterAfterSetterCompletes = autoUpdateGetter
        });
      return this;
    }

    /// <summary>
    ///   Copies the asynchronous property to the VISA device.
    /// </summary>
    /// <param name="asyncProperty">
    ///   The asynchronous property to copy.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder CopyAsyncProperty(IAsyncProperty asyncProperty)
    {
      var asyncPropertyClone = (IAsyncProperty) asyncProperty.Clone();
      asyncPropertyClone.TargetDevice = _device;
      _device.CustomAsyncProperties.Add(asyncPropertyClone);
      return this;
    }

    /// <summary>
    ///   Copies asynchronous properties to the VISA device.
    /// </summary>
    /// <param name="asyncProperties">
    ///   An array or a parameter sequence of asynchronous properties to copy.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder CopyAsyncProperties(params IAsyncProperty[] asyncProperties)
    {
      asyncProperties
        .Select(asyncProperty => (IAsyncProperty) asyncProperty.Clone())
        .ToList()
        .ForEach(asyncPropertyClone =>
        {
          asyncPropertyClone.TargetDevice = _device;
          _device.CustomAsyncProperties.Add(asyncPropertyClone);
        });
      return this;
    }

    /// <summary>
    ///   Removes a previously added asynchronous property from the VISA device by its name.
    /// </summary>
    /// <param name="asyncPropertyName">
    ///   The name of the asynchronous property to remove.
    ///   If there are multiple asynchronous properties with the same name, all of them will be removed.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder RemoveAsyncProperty(string asyncPropertyName)
    {
      _device.CustomAsyncProperties
        .Where(asyncProperty => asyncProperty.Name == asyncPropertyName)
        .ToList()
        .ForEach(asyncProperty => _device.CustomAsyncProperties.Remove(asyncProperty));
      return this;
    }

    /// <summary>
    ///   Clears all previously added asynchronous properties from the device.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder ClearAsyncProperties()
    {
      _device.CustomAsyncProperties.Clear();
      return this;
    }

    /// <summary>
    ///   Adds a device action to the VISA device.
    /// </summary>
    /// <param name="name">
    ///   The name of the device action.
    ///   Adding multiple device actions with the same name is not forbidden but highly not recommended.
    /// </param>
    /// <param name="deviceAction">
    ///   An action delegate to be invoked when the device action is executed.
    ///   The delegate may accept a nullable message-based VISA device instance (the one to be built) as a parameter,
    ///   or just reject it if it is not required for functioning.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public MessageDeviceBuilder AddDeviceAction(string name, Action<IMessageDevice?> deviceAction)
    {
      _device.CustomDeviceActions.Add(new DeviceAction(visaDevice => deviceAction.Invoke(visaDevice as IMessageDevice))
      {
        TargetDevice = _device,
        Name = name
      });
      return this;
    }

    /// <summary>
    ///   Copies a device action to the VISA device.
    /// </summary>
    /// <param name="deviceAction">
    ///   A device action to copy.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder CopyDeviceAction(IDeviceAction deviceAction)
    {
      var deviceActionClone = (IDeviceAction) deviceAction.Clone();
      deviceActionClone.TargetDevice = _device;
      _device.CustomDeviceActions.Add(deviceActionClone);
      return this;
    }

    /// <summary>
    ///   Copies device actions to the VISA device.
    /// </summary>
    /// <param name="deviceActions">
    ///   An array or a parameter sequence of device actions to copy.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder CopyDeviceActions(params IDeviceAction[] deviceActions)
    {
      deviceActions
        .Select(deviceAction => (IDeviceAction) deviceAction.Clone())
        .ToList()
        .ForEach(deviceActionClone =>
        {
          deviceActionClone.TargetDevice = _device;
          _device.CustomDeviceActions.Add(deviceActionClone);
        });
      return this;
    }

    /// <summary>
    ///   Removes a previously added device action from the VISA device by its name.
    /// </summary>
    /// <param name="deviceActionName">
    ///   The name of the device action to remove.
    ///   If there are multiple device actions with the same name, all of them will be removed.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public MessageDeviceBuilder RemoveDeviceAction(string deviceActionName)
    {
      _device.CustomDeviceActions
        .Where(deviceAction => deviceAction.Name == deviceActionName)
        .ToList()
        .ForEach(deviceAction => _device.CustomDeviceActions.Remove(deviceAction));
      return this;
    }

    /// <summary>
    ///   Clears all previously added device actions from the device.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   The standard <see cref="VisaDevice.Reset" /> device action is inherited and cannot be removed.
    /// </remarks>
    public MessageDeviceBuilder ClearDeviceActions()
    {
      _device.CustomDeviceActions.Clear();
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
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public MessageDeviceBuilder UseInitializeCallback(Action<IMessageDevice> callback)
    {
      _device.CustomInitializeCallback = callback;
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
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public MessageDeviceBuilder UseDeInitializeCallback(Action<IMessageDevice> callback)
    {
      _device.CustomDeInitializeCallback = callback;
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
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public MessageDeviceBuilder UseGetIdentifierCallback(Func<IMessageDevice, string> callback)
    {
      _device.CustomGetIdentifierCallback = callback;
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
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public MessageDeviceBuilder UseResetCallback(Action<IMessageDevice> callback)
    {
      _device.CustomResetCallback = callback;
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
      _device.CustomMessageProcessor = messageProcessor;
      return this;
    }

    /// <inheritdoc />
    public IMessageDevice BuildDevice() => (IMessageDevice) _device.Clone();

    /// <inheritdoc />
    public IVisaDeviceController BuildDeviceController() => new VisaDeviceController(BuildDevice());
  }
}
