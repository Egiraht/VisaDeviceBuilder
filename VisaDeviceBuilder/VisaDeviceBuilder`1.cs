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
    ///   Gets or sets a custom VISA resource manager type.
    ///   Setting to <c>null</c> indicates that the <see cref="GlobalResourceManager" /> class should be used.
    /// </summary>
    private Type? CustomResourceManagerType { get; set; }

    /// <summary>
    ///   Initializes a new VISA device builder instance.
    /// </summary>
    public VisaDeviceBuilder()
    {
    }

    /// <summary>
    ///   Initializes a new VISA device builder instance with building configuration copied from a compatible
    ///   VISA device builder instance.
    /// </summary>
    /// <param name="baseDeviceBuilder">
    ///   A compatible <see cref="VisaDeviceBuilder{TBuildableVisaDevice, TOutputVisaDevice}" /> instance to copy
    ///   configuration from.
    /// </param>
    public VisaDeviceBuilder(VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> baseDeviceBuilder) =>
      Device = (TBuildableVisaDevice) baseDeviceBuilder.BuildDevice().Clone();

    /// <summary>
    ///   Instructs the builder to use the <see cref="GlobalResourceManager" /> class for VISA device session
    ///   management.
    /// </summary>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseGlobalVisaResourceManager()
    {
      CustomResourceManagerType = null;
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

      CustomResourceManagerType = resourceManagerType;
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
      UseCustomVisaResourceManagerType<TResourceManager>() =>
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
    ///   Defines VISA hardware interfaces that the created VISA device can support.
    /// </summary>
    /// <param name="interfaces">
    ///   An array or parameter sequence of VISA hardware interfaces supported by the device.
    ///   If an empty array is provided, the default supported hardware interfaces (i.e. all available) will be used.
    /// </param>
    /// <returns>
    ///   This builder instance.
    /// </returns>
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> DefineSupportedHardwareInterfaces(
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
        Owner = null,
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
        Owner = null,
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
        Owner = null,
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
      var clone = (IOwnedAsyncProperty<TOutputVisaDevice>) ownedAsyncProperty.Clone();
      clone.Owner = null;
      Device.CustomAsyncProperties.Add(clone);
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
      Device.CustomAsyncProperties.AddRange(ownedAsyncProperties.Select(asyncProperty =>
      {
        var clone = (IOwnedAsyncProperty<TOutputVisaDevice>) asyncProperty.Clone();
        clone.Owner = null;
        return clone;
      }));
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> AddDeviceAction(string name,
      Action<TOutputVisaDevice> action)
    {
      Device.CustomDeviceActions.Add(new OwnedDeviceAction<TOutputVisaDevice>(action)
      {
        Owner = null,
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
      var clone = (IOwnedDeviceAction<TOutputVisaDevice>) ownedDeviceAction.Clone();
      clone.Owner = null;
      Device.CustomDeviceActions.Add(clone);
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
      Device.CustomDeviceActions.AddRange(ownedDeviceActions.Select(deviceAction =>
      {
        var clone = (IOwnedDeviceAction<TOutputVisaDevice>) deviceAction.Clone();
        clone.Owner = null;
        return clone;
      }));
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> RegisterDisposable(IDisposable disposable)
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> RegisterDisposables(
      params IDisposable[] disposables)
    {
      Device.CustomDisposables.AddRange(disposables);
      return this;
    }

    /// <inheritdoc />
    public TOutputVisaDevice BuildDevice() => (TOutputVisaDevice) Device.Clone();

    /// <inheritdoc />
    public IVisaDeviceController BuildDeviceController() => new VisaDeviceController(BuildDevice());
  }
}
