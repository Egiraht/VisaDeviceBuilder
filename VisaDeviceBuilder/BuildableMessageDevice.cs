using System;
using System.Collections.Generic;
using System.Linq;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   A class representing a message-based VISA device that can be created using a VISA device builder class.
  /// </summary>
  internal class BuildableMessageDevice : MessageDevice, IBuildableMessageDevice
  {
    /// <inheritdoc />
    public HardwareInterfaceType[] CustomSupportedInterfaces { get; set; } = Array.Empty<HardwareInterfaceType>();

    /// <inheritdoc />
    public List<IAsyncProperty> CustomAsyncProperties { get; } = new();

    /// <inheritdoc />
    public List<IDeviceAction> CustomDeviceActions { get; } = new();

    /// <inheritdoc />
    public Action<IMessageDevice>? CustomInitializeCallback { get; set; }

    /// <inheritdoc />
    Action<IVisaDevice>? IBuildableVisaDevice.CustomInitializeCallback
    {
      get => CustomInitializeCallback != null
        ? session => CustomInitializeCallback.Invoke((IMessageDevice) session)
        : null;
      set => CustomInitializeCallback = value;
    }

    /// <inheritdoc />
    public Action<IMessageDevice>? CustomDeInitializeCallback { get; set; }

    /// <inheritdoc />
    Action<IVisaDevice>? IBuildableVisaDevice.CustomDeInitializeCallback
    {
      get => CustomDeInitializeCallback != null
        ? session => CustomDeInitializeCallback.Invoke((IMessageDevice) session)
        : null;
      set => CustomDeInitializeCallback = value;
    }

    /// <inheritdoc />
    public Func<IMessageDevice, string>? CustomGetIdentifierCallback { get; set; }

    /// <inheritdoc />
    Func<IVisaDevice, string>? IBuildableVisaDevice.CustomGetIdentifierCallback
    {
      get => CustomGetIdentifierCallback != null
        ? session => CustomGetIdentifierCallback.Invoke((IMessageDevice) session)
        : null;
      set => CustomGetIdentifierCallback = value;
    }

    /// <inheritdoc />
    public Action<IMessageDevice>? CustomResetCallback { get; set; }

    /// <inheritdoc />
    Action<IVisaDevice>? IBuildableVisaDevice.CustomResetCallback
    {
      get => CustomResetCallback != null
        ? session => CustomResetCallback.Invoke((IMessageDevice) session)
        : null;
      set => CustomResetCallback = value;
    }

    /// <inheritdoc />
    public Func<IMessageDevice, string, string>? CustomMessageProcessor { get; set; }

    /// <inheritdoc />
    public List<IDisposable> CustomDisposables { get; } = new();

    /// <inheritdoc />
    public override IEnumerable<IAsyncProperty> AsyncProperties => base.AsyncProperties.Concat(CustomAsyncProperties);

    /// <inheritdoc />
    public override IEnumerable<IDeviceAction> DeviceActions => base.DeviceActions.Concat(CustomDeviceActions);

    /// <inheritdoc />
    public override HardwareInterfaceType[] SupportedInterfaces => CustomSupportedInterfaces.Any()
      ? CustomSupportedInterfaces.ToArray()
      : base.SupportedInterfaces;

    /// <summary>
    ///   Throws a <see cref="VisaDeviceException" /> exception when no VISA session is opened.
    /// </summary>
    private void ThrowOnNoSession()
    {
      if (Session == null)
        throw new VisaDeviceException(this, new InvalidOperationException(
          $"There is no opened VISA session to perform an operation or the device \"{AliasName}\" does not support message-based sessions."));
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
      ThrowOnNoSession();

      lock (SessionLock)
        return CustomGetIdentifierCallback?.Invoke(this) ?? base.GetIdentifier();
    }

    /// <inheritdoc />
    public override void Reset()
    {
      ThrowOnNoSession();

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
      ThrowOnNoSession();

      lock (SessionLock)
      {
        return CustomMessageProcessor != null
          ? CustomMessageProcessor.Invoke(this, message)
          : base.SendMessage(message);
      }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
      CustomDisposables.ForEach(disposable => disposable.Dispose());
      base.Dispose();
    }
  }
}
