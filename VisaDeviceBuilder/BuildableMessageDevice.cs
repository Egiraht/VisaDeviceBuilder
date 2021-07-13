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
  public class BuildableMessageDevice : MessageDevice, IBuildableMessageDevice<IMessageDevice>
  {
    /// <inheritdoc />
    public HardwareInterfaceType[]? CustomSupportedInterfaces { get; set; }

    /// <inheritdoc />
    public ObservableCollection<IOwnedAsyncProperty<IMessageDevice>> CustomAsyncProperties { get; } = new();

    /// <inheritdoc />
    public ObservableCollection<IOwnedDeviceAction<IMessageDevice>> CustomDeviceActions { get; } = new();

    /// <inheritdoc />
    public Action<IMessageDevice>? CustomInitializeCallback { get; set; }

    /// <inheritdoc />
    public Action<IMessageDevice>? CustomDeInitializeCallback { get; set; }

    /// <inheritdoc />
    public Func<IMessageDevice, string>? CustomGetIdentifierCallback { get; set; }

    /// <inheritdoc />
    public Action<IMessageDevice>? CustomResetCallback { get; set; }

    /// <inheritdoc />
    public Func<IMessageDevice, string, string>? CustomMessageProcessor { get; set; }

    /// <inheritdoc />
    public override IEnumerable<IAsyncProperty> AsyncProperties => base.AsyncProperties.Concat(CustomAsyncProperties);

    /// <inheritdoc />
    public override IEnumerable<IDeviceAction> DeviceActions => base.DeviceActions.Concat(CustomDeviceActions);

    /// <inheritdoc />
    public override HardwareInterfaceType[] SupportedInterfaces =>
      CustomSupportedInterfaces ?? base.SupportedInterfaces;

    /// <summary>
    ///   Initializes a new buildable message-based VISA device instance.
    /// </summary>
    public BuildableMessageDevice()
    {
      // Automatically take ownership of owned asynchronous properties and device actions when adding them into the
      // corresponding observable collections.
      CustomAsyncProperties.CollectionChanged += (_, args) => args.NewItems?
        .Cast<IOwnedAsyncProperty<IMessageDevice>>()
        .ToList()
        .ForEach(ownedAsyncProperty => ownedAsyncProperty.Owner = this);
      CustomDeviceActions.CollectionChanged += (_, args) => args.NewItems?
        .Cast<IOwnedDeviceAction<IMessageDevice>>()
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
    public override string SendMessage(string message)
    {
      ThrowWhenNoVisaSessionIsOpened();

      lock (SessionLock)
      {
        return CustomMessageProcessor != null
          ? CustomMessageProcessor.Invoke(this, message)
          : base.SendMessage(message);
      }
    }

    /// <inheritdoc />
    public override object Clone()
    {
      var clone = (BuildableMessageDevice) base.Clone();
      clone.CustomSupportedInterfaces = CustomSupportedInterfaces;
      CustomAsyncProperties
        .Select(ownedAsyncProperty => (IOwnedAsyncProperty<IMessageDevice>) ownedAsyncProperty.Clone())
        .ToList()
        .ForEach(ownedAsyncPropertyClone => clone.CustomAsyncProperties.Add(ownedAsyncPropertyClone));
      CustomDeviceActions
        .Select(ownedDeviceAction => (IOwnedDeviceAction<IMessageDevice>) ownedDeviceAction.Clone())
        .ToList()
        .ForEach(ownedDeviceActionClone => clone.CustomDeviceActions.Add(ownedDeviceActionClone));
      clone.CustomInitializeCallback = CustomInitializeCallback;
      clone.CustomDeInitializeCallback = CustomDeInitializeCallback;
      clone.CustomGetIdentifierCallback = CustomGetIdentifierCallback;
      clone.CustomResetCallback = CustomResetCallback;
      clone.CustomMessageProcessor = CustomMessageProcessor;
      return clone;
    }
  }
}
