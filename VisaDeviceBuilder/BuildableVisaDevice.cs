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
  internal class BuildableVisaDevice : VisaDevice, IBuildableVisaDevice
  {
    /// <inheritdoc />
    public List<HardwareInterfaceType> CustomSupportedInterfaces { get; } = new();

    /// <inheritdoc />
    public List<IAsyncProperty> CustomAsyncProperties { get; } = new();

    /// <inheritdoc />
    public List<IDeviceAction> CustomDeviceActions { get; } = new();

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
    public override HardwareInterfaceType[] SupportedInterfaces => CustomSupportedInterfaces.Any()
      ? CustomSupportedInterfaces.ToArray()
      : base.SupportedInterfaces;

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
    public override void Dispose()
    {
      CustomDisposables.ForEach(disposable => disposable.Dispose());
      base.Dispose();
    }
  }
}
