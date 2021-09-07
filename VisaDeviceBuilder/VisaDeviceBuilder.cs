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
  public class VisaDeviceBuilder : IVisaDeviceBuilder<IVisaDevice>
  {
    /// <summary>
    ///   The base buildable VISA device instance that stores the builder configuration and is used for building of new
    ///   VISA device instances by cloning.
    /// </summary>
    private readonly IBuildableVisaDevice<IVisaDevice> _device = new VisaDevice();

    /// <summary>
    ///   Initializes a new VISA device builder instance.
    /// </summary>
    public VisaDeviceBuilder()
    {
    }

    /// <summary>
    ///   Initializes a new VISA device builder instance with building configuration copied from a compatible buildable
    ///   VISA device instance.
    /// </summary>
    /// <param name="baseDevice">
    ///   A base VISA device instance to copy configuration from. This instance must derive from the
    ///   <see cref="VisaDevice" /> class or must implement the <see cref="IBuildableVisaDevice{TVisaDevice}" />
    ///   interface where TVisaDevice = <see cref="IVisaDevice" />.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///   Cannot copy building configuration from the provided VISA device instance because it does not implement the
    ///   <see cref="IBuildableVisaDevice{TVisaDevice}" /> interface where TVisaDevice = <see cref="IVisaDevice" />.
    /// </exception>
    public VisaDeviceBuilder(IVisaDevice baseDevice)
    {
      if (baseDevice is not IBuildableVisaDevice<IVisaDevice> buildableVisaDevice)
        throw new InvalidOperationException(
          "Cannot copy building configuration from the provided VISA device instance of type " +
          $"\"{baseDevice.GetType().Name}\" because it does not implement the " +
          $"\"{typeof(IBuildableVisaDevice<IVisaDevice>).Name}\" interface.");

      _device = (IBuildableVisaDevice<IVisaDevice>) buildableVisaDevice.Clone();
    }

    /// <summary>
    ///   Instructs the builder to use the <see cref="GlobalResourceManager" /> class for VISA device session
    ///   management.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder UseGlobalVisaResourceManager()
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
    public VisaDeviceBuilder UseCustomVisaResourceManagerType(Type resourceManagerType)
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
    public VisaDeviceBuilder UseCustomVisaResourceManagerType<TResourceManager>()
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
    public VisaDeviceBuilder UseDefaultResourceName(string resourceName)
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
    public VisaDeviceBuilder UseConnectionTimeout(int timeout)
    {
      _device.ConnectionTimeout = timeout;
      return this;
    }

    /// <summary>
    ///   Instructs the builder that the VISA device being built supports the default hardware interfaces, defined for
    ///   the <see cref="VisaDevice" /> class (<see cref="VisaDevice.DefaultSupportedInterfaces" />).
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder UseDefaultSupportedHardwareInterfaces()
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
    public VisaDeviceBuilder UseSupportedHardwareInterfaces(params HardwareInterfaceType[] interfaces)
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
    ///   The delegate may accept a nullable VISA device instance (the one to be built) as a parameter, or just reject
    ///   it if it is not required for functioning.
    ///   The delegate must return a stored property's value of type <typeparamref name="TValue" />.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder AddReadOnlyAsyncProperty<TValue>(string name, Func<IVisaDevice?, TValue> getter)
    {
      _device.CustomAsyncProperties.Add(new AsyncProperty<TValue>(getter)
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
    ///   The delegate may accept a nullable VISA device instance (the one to be built) as the first parameter, or just
    ///   reject it if it is not required for functioning.
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
    public VisaDeviceBuilder AddWriteOnlyAsyncProperty<TValue>(string name, Action<IVisaDevice?, TValue> setter)
    {
      _device.CustomAsyncProperties.Add(new AsyncProperty<TValue>(setter)
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
    ///   The delegate may accept a nullable VISA device instance (the one to be built) as a parameter, or just reject
    ///   it if it is not required for functioning.
    ///   The delegate must return a stored property's value of type <typeparamref name="TValue" />.
    /// </param>
    /// <param name="setter">
    ///   A setter delegate to be called when the asynchronous property is written.
    ///   The delegate may accept a nullable VISA device instance (the one to be built) as the first parameter, or just
    ///   reject it if it is not required for functioning.
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
    public VisaDeviceBuilder AddReadWriteAsyncProperty<TValue>(string name, Func<IVisaDevice?, TValue> getter,
      Action<IVisaDevice?, TValue> setter, bool autoUpdateGetter = true)
    {
      _device.CustomAsyncProperties.Add(new AsyncProperty<TValue>(getter, setter)
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
    public VisaDeviceBuilder CopyAsyncProperty(IAsyncProperty asyncProperty)
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
    public VisaDeviceBuilder CopyAsyncProperties(params IAsyncProperty[] asyncProperties)
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
    public VisaDeviceBuilder RemoveAsyncProperty(string asyncPropertyName)
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
    public VisaDeviceBuilder ClearAsyncProperties()
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
    ///   The delegate may accept a nullable VISA device instance (the one to be built) as a parameter, or just reject
    ///   it if it is not required for functioning.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   For low-level control over the device communication process use the device's underlying
    ///   <see cref="IMessageDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder AddDeviceAction(string name, Action<IVisaDevice?> deviceAction)
    {
      _device.CustomDeviceActions.Add(new DeviceAction(deviceAction)
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
    public VisaDeviceBuilder CopyDeviceAction(IDeviceAction deviceAction)
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
    public VisaDeviceBuilder CopyDeviceActions(params IDeviceAction[] deviceActions)
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
    public VisaDeviceBuilder RemoveDeviceAction(string deviceActionName)
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
    public VisaDeviceBuilder ClearDeviceActions()
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
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder UseInitializeCallback(Action<IVisaDevice> callback)
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
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder UseDeInitializeCallback(Action<IVisaDevice> callback)
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
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder UseGetIdentifierCallback(Func<IVisaDevice, string> callback)
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
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder UseResetCallback(Action<IVisaDevice> callback)
    {
      _device.CustomResetCallback = callback;
      return this;
    }

    /// <inheritdoc />
    public IVisaDevice BuildDevice() => (IVisaDevice) _device.Clone();

    /// <inheritdoc />
    public IVisaDeviceController BuildDeviceController() => new VisaDeviceController(BuildDevice());
  }
}
