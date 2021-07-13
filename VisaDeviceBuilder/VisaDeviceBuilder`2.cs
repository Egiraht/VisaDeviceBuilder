using System;
using System.Linq;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   A builder class that can build VISA devices with custom behavior.
  /// </summary>
  /// <typeparam name="TBuildableVisaDevice">
  ///   The type of a buildable device this builder will actually work with.
  ///   It must be a class implementing the <see cref="IBuildableVisaDevice{TVisaDevice}" /> interface and derive from
  ///   the <typeparamref name="TOutputVisaDevice"/> type.
  /// </typeparam>
  /// <typeparam name="TOutputVisaDevice">
  ///   The target type of a VISA device this builder will finally build.
  ///   It must implement the <see cref="IVisaDevice" /> interface.
  /// </typeparam>
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
  public class VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> : IVisaDeviceBuilder<TOutputVisaDevice>
    where TBuildableVisaDevice : class, TOutputVisaDevice, IBuildableVisaDevice<TOutputVisaDevice>, new()
    where TOutputVisaDevice : class, IVisaDevice
  {
    /// <summary>
    ///   Gets the base buildable device instance that stores the builder configuration and is used for building of new
    ///   device instances by cloning and casting to the output device type.
    /// </summary>
    protected TBuildableVisaDevice Device { get; } = new();

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
    /// <param name="device">
    ///   A VISA device instance to copy configuration from. This instance must have been previously built by a
    ///   compatible VISA device builder class and must be of <typeparamref name="TBuildableVisaDevice" /> type or
    ///   derive from it.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///   Cannot copy building configuration from the provided VISA device instance because it is not assignable to the
    ///   <typeparamref name="TBuildableVisaDevice" /> type.
    /// </exception>
    public VisaDeviceBuilder(TOutputVisaDevice device)
    {
      if (device is not TBuildableVisaDevice buildableVisaDevice)
        throw new InvalidOperationException(
          "Cannot copy building configuration from the provided VISA device instance of type " +
          $"\"{device.GetType().Name}\" because it is not assignable to the \"{typeof(TBuildableVisaDevice).Name}\" type.");

      Device = (TBuildableVisaDevice) buildableVisaDevice.Clone();
    }

    /// <summary>
    ///   Instructs the builder to use the <see cref="GlobalResourceManager" /> class for VISA device session
    ///   management.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseGlobalVisaResourceManager()
    {
      Device.ResourceManager?.Dispose();
      Device.ResourceManager = null;
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseCustomVisaResourceManagerType(
      Type resourceManagerType)
    {
      if (resourceManagerType.GetInterface(nameof(IResourceManager)) == null)
        throw new InvalidOperationException(
          $"\"{resourceManagerType.Name}\" is not a valid VISA resource manager type.");

      Device.ResourceManager?.Dispose();
      Device.ResourceManager = (IResourceManager) Activator.CreateInstance(resourceManagerType)!;
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice>
      UseCustomVisaResourceManagerType<TResourceManager>() where TResourceManager : IResourceManager =>
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseDefaultResourceName(string resourceName)
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseConnectionTimeout(int timeout)
    {
      Device.ConnectionTimeout = timeout;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to treat the default hardware interfaces defined for the current buildable device type
    ///   (<typeparamref name="TBuildableVisaDevice" />) as supported by the created VISA device.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseDefaultSupportedHardwareInterfaces()
    {
      Device.CustomSupportedInterfaces = null;
      return this;
    }

    /// <summary>
    ///   Instructs the builder to treat the specified VISA hardware interfaces as supported by the created VISA device.
    /// </summary>
    /// <param name="interfaces">
    ///   An array or parameter sequence of VISA hardware interfaces supported by the device.
    ///   If an empty array is provided, the default supported hardware interfaces (i.e. all available) will be used.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseSupportedHardwareInterfaces(
      params HardwareInterfaceType[] interfaces)
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
    ///   Adding multiple asynchronous properties with the same name is not forbidden but highly not recommended.
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> AddReadOnlyAsyncProperty<TValue>(string name,
      Func<TOutputVisaDevice, TValue> getter)
    {
      Device.CustomAsyncProperties.Add(new OwnedAsyncProperty<TOutputVisaDevice, TValue>(getter)
      {
        Owner = Device,
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> AddWriteOnlyAsyncProperty<TValue>(string name,
      Action<TOutputVisaDevice, TValue> setter)
    {
      Device.CustomAsyncProperties.Add(new OwnedAsyncProperty<TOutputVisaDevice, TValue>(setter)
      {
        Owner = Device,
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
    ///   A getter delegate for the read-only asynchronous property that reads and returns a value from the device
    ///   instance provided as a delegate parameter.
    /// </param>
    /// <param name="setter">
    ///   A setter delegate for the write-only asynchronous property that writes a value to the device instance. Both
    ///   objects are provided as delegate parameters.
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
    ///   <see cref="IVisaDevice.Session" /> object.
    /// </remarks>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> AddReadWriteAsyncProperty<TValue>(string name,
      Func<TOutputVisaDevice, TValue> getter, Action<TOutputVisaDevice, TValue> setter, bool autoUpdateGetter = true)
    {
      Device.CustomAsyncProperties.Add(new OwnedAsyncProperty<TOutputVisaDevice, TValue>(getter, setter)
      {
        Owner = Device,
        Name = name,
        AutoUpdateGetterAfterSetterCompletes = autoUpdateGetter
      });
      return this;
    }

    /// <summary>
    ///   Copies a compatible owned asynchronous property to the VISA device.
    /// </summary>
    /// <param name="ownedAsyncProperty">
    ///   An owned asynchronous property instance of a compatible device type to copy.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   Non-owned asynchronous properties declared in classes as <see cref="AsyncProperty{TValue}" /> instances cannot
    ///   be copied because they reference their device directly in code. These instances can only be inherited in
    ///   deriving classes.
    /// </remarks>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> CopyAsyncProperty(
      IOwnedAsyncProperty<TOutputVisaDevice> ownedAsyncProperty)
    {
      Device.CustomAsyncProperties.Add((IOwnedAsyncProperty<TOutputVisaDevice>) ownedAsyncProperty.Clone());
      return this;
    }

    /// <summary>
    ///   Copies compatible owned asynchronous property to the VISA device.
    /// </summary>
    /// <param name="ownedAsyncProperties">
    ///   An array or a parameter sequence of owned asynchronous property instances of a compatible device type to copy.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   Non-owned asynchronous properties declared in classes as <see cref="AsyncProperty{TValue}" /> instances cannot
    ///   be copied because they reference their device directly in code. These instances can only be inherited in
    ///   deriving classes.
    /// </remarks>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> CopyAsyncProperties(
      params IOwnedAsyncProperty<TOutputVisaDevice>[] ownedAsyncProperties)
    {
      ownedAsyncProperties
        .Select(ownedAsyncProperty => (IOwnedAsyncProperty<TOutputVisaDevice>) ownedAsyncProperty.Clone())
        .ToList()
        .ForEach(ownedAsyncPropertyClone => Device.CustomAsyncProperties.Add(ownedAsyncPropertyClone));
      return this;
    }

    /// <summary>
    ///   Removes a previously added asynchronous property from the VISA device by its name.
    /// </summary>
    /// <param name="asyncPropertyName">
    ///   The name of the asynchronous property to remove.
    ///   If there are multiple asynchronous properties with the same name, only the first occurence will be removed.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> RemoveAsyncProperty(string asyncPropertyName)
    {
      var asyncPropertyToRemove =
        Device.CustomAsyncProperties.FirstOrDefault(asyncProperty => asyncProperty.Name == asyncPropertyName);
      if (asyncPropertyToRemove != null)
        Device.CustomAsyncProperties.Remove(asyncPropertyToRemove);
      return this;
    }

    /// <summary>
    ///   Clears all previously added asynchronous properties from the device.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> ClearAsyncProperties()
    {
      Device.CustomAsyncProperties.Clear();
      return this;
    }

    /// <summary>
    ///   Adds a device action to the VISA device.
    /// </summary>
    /// <param name="name">
    ///   The name of the device action.
    ///   Adding multiple device actions with the same name is not forbidden but highly not recommended.
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> AddDeviceAction(string name,
      Action<TOutputVisaDevice> action)
    {
      Device.CustomDeviceActions.Add(new OwnedDeviceAction<TOutputVisaDevice>(action)
      {
        Owner = Device,
        Name = name
      });
      return this;
    }

    /// <summary>
    ///   Copies a compatible owned device action to the VISA device.
    /// </summary>
    /// <param name="ownedDeviceAction">
    ///   An owned device action instance of a compatible device type to copy.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   Non-owned device actions declared in classes as <see cref="DeviceAction" /> instances cannot be copied because
    ///   they reference their device directly in code. These instances can only be inherited in deriving classes.
    /// </remarks>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> CopyDeviceAction(
      IOwnedDeviceAction<TOutputVisaDevice> ownedDeviceAction)
    {
      Device.CustomDeviceActions.Add((IOwnedDeviceAction<TOutputVisaDevice>) ownedDeviceAction.Clone());
      return this;
    }

    /// <summary>
    ///   Copies compatible owned device actions to the VISA device.
    /// </summary>
    /// <param name="ownedDeviceActions">
    ///   An array or a parameter sequence of owned device action instances of a compatible device type to copy.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   Non-owned device actions declared in classes as <see cref="DeviceAction" /> instances cannot be copied because
    ///   they reference their device directly in code. These instances can only be inherited in deriving classes.
    /// </remarks>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> CopyDeviceActions(
      params IOwnedDeviceAction<TOutputVisaDevice>[] ownedDeviceActions)
    {
      ownedDeviceActions
        .Select(ownedDeviceAction => (IOwnedDeviceAction<TOutputVisaDevice>) ownedDeviceAction.Clone())
        .ToList()
        .ForEach(ownedDeviceActionClone => Device.CustomDeviceActions.Add(ownedDeviceActionClone));
      return this;
    }

    /// <summary>
    ///   Removes a previously added device action from the VISA device by its name.
    /// </summary>
    /// <param name="deviceActionName">
    ///   The name of the device action to remove.
    ///   If there are multiple device actions with the same name, only the first occurence will be removed.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> RemoveDeviceAction(string deviceActionName)
    {
      var deviceActionToRemove =
        Device.CustomDeviceActions.FirstOrDefault(deviceAction => deviceAction.Name == deviceActionName);
      if (deviceActionToRemove != null)
        Device.CustomDeviceActions.Remove(deviceActionToRemove);
      return this;
    }

    /// <summary>
    ///   Clears all previously added device actions from the device.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    /// <remarks>
    ///   The standard <see cref="IVisaDevice.Reset" /> device action is inherited and cannot be removed.
    /// </remarks>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> ClearDeviceActions()
    {
      Device.CustomDeviceActions.Clear();
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseInitializeCallback(
      Action<TOutputVisaDevice> callback)
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseDeInitializeCallback(
      Action<TOutputVisaDevice> callback)
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseGetIdentifierCallback(
      Func<TOutputVisaDevice, string> callback)
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseResetCallback(
      Action<TOutputVisaDevice> callback)
    {
      Device.CustomResetCallback = callback;
      return this;
    }

    /// <inheritdoc />
    public TOutputVisaDevice BuildDevice() => (TOutputVisaDevice) Device.Clone();

    /// <inheritdoc />
    public IVisaDeviceController BuildDeviceController() => new VisaDeviceController(BuildDevice());
  }
}
