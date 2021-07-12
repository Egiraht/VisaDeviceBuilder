using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   A class representing a message-based VISA device that can be created using a VISA device builder class.
  /// </summary>
  public class BuildableVisaDevice : VisaDevice, IBuildableVisaDevice<IVisaDevice>
  {
    /// <inheritdoc />
    public HardwareInterfaceType[]? CustomSupportedInterfaces { get; set; }

    /// <inheritdoc />
    public ObservableCollection<IOwnedAsyncProperty<IVisaDevice>> CustomAsyncProperties { get; } = new();

    /// <inheritdoc />
    public ObservableCollection<IOwnedDeviceAction<IVisaDevice>> CustomDeviceActions { get; } = new();

    /// <inheritdoc />
    public Action<IVisaDevice>? CustomInitializeCallback { get; set; }

    /// <inheritdoc />
    public Action<IVisaDevice>? CustomDeInitializeCallback { get; set; }

    /// <inheritdoc />
    public Func<IVisaDevice, string>? CustomGetIdentifierCallback { get; set; }

    /// <inheritdoc />
    public Action<IVisaDevice>? CustomResetCallback { get; set; }

    /// <inheritdoc />
    public List<IDisposable> CustomDisposables { get; } = new();

    /// <inheritdoc />
    public override IEnumerable<IAsyncProperty> AsyncProperties => base.AsyncProperties.Concat(CustomAsyncProperties);

    /// <inheritdoc />
    public override IEnumerable<IDeviceAction> DeviceActions => base.DeviceActions.Concat(CustomDeviceActions);

    /// <inheritdoc />
    public override HardwareInterfaceType[] SupportedInterfaces =>
      CustomSupportedInterfaces ?? base.SupportedInterfaces;

    /// <summary>
    ///   Initializes a new buildable VISA device instance.
    /// </summary>
    public BuildableVisaDevice()
    {
      // Automatically take ownership of owned asynchronous properties and device actions when adding them into the
      // corresponding observable collections.
      CustomAsyncProperties.CollectionChanged += (_, args) => args.NewItems?
        .Cast<IOwnedAsyncProperty<IVisaDevice>>()
        .ToList()
        .ForEach(ownedAsyncProperty => ownedAsyncProperty.Owner = this);
      CustomDeviceActions.CollectionChanged += (_, args) => args.NewItems?
        .Cast<IOwnedDeviceAction<IVisaDevice>>()
        .ToList()
        .ForEach(ownedDeviceAction => ownedDeviceAction.Owner = this);
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
      lock (SessionLock)
      {
        base.Initialize();
        CustomInitializeCallback?.Invoke(this);
      }
    }

    /// <inheritdoc />
    protected override void DeInitialize()
    {
      lock (SessionLock)
      {
        CustomDeInitializeCallback?.Invoke(this);
        base.DeInitialize();
      }
    }

    /// <inheritdoc />
    public override string GetIdentifier()
    {
      ThrowWhenNoVisaSessionIsOpened();

      lock (SessionLock)
        return CustomGetIdentifierCallback?.Invoke(this) ?? base.GetIdentifier();
    }

    /// <inheritdoc />
    public override void Reset()
    {
      ThrowWhenNoVisaSessionIsOpened();

      lock (SessionLock)
      {
        if (CustomResetCallback != null)
          CustomResetCallback?.Invoke(this);
        else
          base.Reset();
      }
    }

    /// <inheritdoc />
    public override object Clone()
    {
      var clone = (BuildableVisaDevice) base.Clone();
      clone.CustomSupportedInterfaces = CustomSupportedInterfaces;
      CustomAsyncProperties
        .Select(ownedAsyncProperty => (IOwnedAsyncProperty<IVisaDevice>) ownedAsyncProperty.Clone())
        .ToList()
        .ForEach(ownedAsyncPropertyClone => clone.CustomAsyncProperties.Add(ownedAsyncPropertyClone));
      CustomDeviceActions
        .Select(ownedDeviceAction => (IOwnedDeviceAction<IVisaDevice>) ownedDeviceAction.Clone())
        .ToList()
        .ForEach(ownedDeviceActionClone => clone.CustomDeviceActions.Add(ownedDeviceActionClone));
      clone.CustomInitializeCallback = CustomInitializeCallback;
      clone.CustomDeInitializeCallback = CustomDeInitializeCallback;
      clone.CustomGetIdentifierCallback = CustomGetIdentifierCallback;
      clone.CustomResetCallback = CustomResetCallback;
      return clone;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
      CustomDisposables.ForEach(disposable => disposable.Dispose());
      base.Dispose();
    }
  }
}
