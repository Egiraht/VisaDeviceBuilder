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
  public class BuildableMessageDevice : MessageDevice, IBuildableMessageDevice<IMessageDevice>
  {
    /// <inheritdoc />
    public HardwareInterfaceType[]? CustomSupportedInterfaces { get; set; }

    /// <inheritdoc />
    public List<IOwnedAsyncProperty<IMessageDevice>> CustomAsyncProperties { get; init; } = new();

    /// <inheritdoc />
    public List<IOwnedDeviceAction<IMessageDevice>> CustomDeviceActions { get; init; } = new();

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
    public List<IDisposable> CustomDisposables { get; init; } = new();

    /// <inheritdoc />
    public override IEnumerable<IAsyncProperty> AsyncProperties => base.AsyncProperties.Concat(CustomAsyncProperties);

    /// <inheritdoc />
    public override IEnumerable<IDeviceAction> DeviceActions => base.DeviceActions.Concat(CustomDeviceActions);

    /// <inheritdoc />
    public override HardwareInterfaceType[] SupportedInterfaces =>
      CustomSupportedInterfaces ?? base.SupportedInterfaces;

    /// <summary>
    ///   Throws a <see cref="VisaDeviceException" /> exception when no VISA session is opened.
    /// </summary>
    private void ThrowOnNoSession()
    {
      if (Session == null)
        throw new VisaDeviceException(this,
          new InvalidOperationException("There is no opened VISA session to perform an operation."));
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
