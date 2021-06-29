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
    where TBuildableVisaDevice : TOutputVisaDevice, IBuildableVisaDevice<TOutputVisaDevice>, new()
    where TOutputVisaDevice : IVisaDevice
  {
    private TBuildableVisaDevice _device = new();

    /// <summary>
    ///   Gets or sets a custom VISA resource manager type.
    ///   Setting to <c>null</c> indicates that the <see cref="GlobalResourceManager" /> class should be used.
    /// </summary>
    private Type? CustomResourceManagerType { get; set; }

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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseConnectionTimeout(int timeout)
    {
      _device.ConnectionTimeout = timeout;
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
      _device.CustomAsyncProperties.Add(new OwnedAsyncProperty<TOutputVisaDevice, TValue>(getter) {Name = name});
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
      _device.CustomAsyncProperties.Add(new OwnedAsyncProperty<TOutputVisaDevice, TValue>(setter) {Name = name});
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
      _device.CustomAsyncProperties.Add(new OwnedAsyncProperty<TOutputVisaDevice, TValue>(getter, setter)
      {
        Name = name,
        AutoUpdateGetterAfterSetterCompletes = autoUpdateGetter
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> AddDeviceAction(string name,
      Action<TOutputVisaDevice> action)
    {
      _device.CustomDeviceActions.Add(new OwnedDeviceAction<TOutputVisaDevice>(action) {Name = name});
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseDeInitializeCallback(
      Action<TOutputVisaDevice> callback)
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseGetIdentifierCallback(
      Func<TOutputVisaDevice, string> callback)
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
    public VisaDeviceBuilder<TBuildableVisaDevice, TOutputVisaDevice> UseResetCallback(
      Action<TOutputVisaDevice> callback)
    {
      _device.CustomResetCallback = callback;
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
      _device.CustomDisposables.Add(disposable);
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
      _device.CustomDisposables.AddRange(disposables);
      return this;
    }

    /// <inheritdoc />
    public TOutputVisaDevice BuildDevice()
    {
      // Creating a new VISA device instance by cloning it from the _device instance.
      var device = new TBuildableVisaDevice
      {
        ResourceManager = CustomResourceManagerType != null
          ? (IResourceManager) Activator.CreateInstance(CustomResourceManagerType)!
          : null,
        ResourceName = _device.ResourceName,
        ConnectionTimeout = _device.ConnectionTimeout,
        CustomSupportedInterfaces = (HardwareInterfaceType[]?) _device.CustomSupportedInterfaces?.Clone(),
        CustomAsyncProperties = _device.CustomAsyncProperties
          .Select(asyncProperty => (IOwnedAsyncProperty<TOutputVisaDevice>) asyncProperty.Clone())
          .ToList(),
        CustomDeviceActions = _device.CustomDeviceActions
          .Select(deviceAction => (IOwnedDeviceAction<TOutputVisaDevice>) deviceAction.Clone())
          .ToList(),
        CustomInitializeCallback = (Action<TOutputVisaDevice>?) _device.CustomInitializeCallback?.Clone(),
        CustomDeInitializeCallback = (Action<TOutputVisaDevice>?) _device.CustomDeInitializeCallback?.Clone(),
        CustomGetIdentifierCallback = (Func<TOutputVisaDevice, string>?) _device.CustomGetIdentifierCallback?.Clone(),
        CustomResetCallback = (Action<TOutputVisaDevice>?) _device.CustomResetCallback?.Clone()
      };

      // Setting the Owner properties to the new device instance.
      foreach (var asyncProperty in device.CustomAsyncProperties)
        asyncProperty.Owner = device;
      foreach (var deviceAction in device.CustomDeviceActions)
        deviceAction.Owner = device;

      // Adding disposables.
      if (device.ResourceManager != null)
        device.CustomDisposables.Add(device.ResourceManager);

      return device;
    }

    /// <inheritdoc />
    public IVisaDeviceController BuildDeviceController() => new VisaDeviceController(BuildDevice());
  }
}
