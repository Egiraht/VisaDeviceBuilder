using System;
using System.Collections.Generic;
using System.Linq;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  public class BuildableMessageDevice : MessageDevice, IBuildableMessageDevice
  {
    public Func<IResourceManager>? CustomResourceManagerSelector { get; set; }

    public Func<string>? CustomResourceNameSelector { get; set; }

    public List<HardwareInterfaceType> CustomSupportedInterfaces { get; } = new();

    public List<IAsyncProperty> CustomAsyncProperties { get; } = new();

    public List<IDeviceAction> CustomDeviceActions { get; } = new();

    public Action<IMessageBasedSession>? CustomInitializeCallback { get; set; }

    public Action<IMessageBasedSession>? CustomDeInitializeCallback { get; set; }

    public Func<IMessageBasedSession, string>? CustomGetIdentifierCallback { get; set; }

    public Action<IMessageBasedSession>? CustomResetCallback { get; set; }

    public Func<IMessageBasedSession, string, string>? CustomMessageProcessor { get; set; }

    public List<IDisposable> CustomDisposables { get; } = new();

    public override IResourceManager? ResourceManager
    {
      get => CustomResourceManagerSelector?.Invoke() ?? base.ResourceManager;
      set
      {
        base.ResourceManager = value;
        CustomResourceManagerSelector = null;
      }
    }

    public override string ResourceName
    {
      get => CustomResourceNameSelector?.Invoke() ?? base.ResourceName;
      set
      {
        base.ResourceName = value;
        CustomResourceNameSelector = null;
      }
    }

    public override IEnumerable<IAsyncProperty> AsyncProperties => base.AsyncProperties.Concat(CustomAsyncProperties);

    public override IEnumerable<IDeviceAction> DeviceActions => base.DeviceActions.Concat(CustomDeviceActions);

    public override HardwareInterfaceType[] SupportedInterfaces => CustomSupportedInterfaces.Any()
      ? CustomSupportedInterfaces.ToArray()
      : base.SupportedInterfaces;

    protected override void Initialize()
    {
      base.Initialize();
      CustomInitializeCallback?.Invoke(Session!);
    }

    protected override void DeInitialize()
    {
      CustomDeInitializeCallback?.Invoke(Session!);
      base.DeInitialize();
    }

    public override string SendMessage(string message)
    {
      ThrowOnNoSession();

      lock (MessageLock)
      {
        return CustomMessageProcessor?.Invoke(Session!, message) ?? base.SendMessage(message);
      }
    }

    public override string GetIdentifier()
    {
      ThrowOnNoSession();

      return CustomGetIdentifierCallback?.Invoke(Session!) ?? base.GetIdentifier();
    }

    public override void Reset()
    {
      ThrowOnNoSession();

      if (CustomResetCallback != null)
        CustomResetCallback?.Invoke(Session!);
      else
        base.Reset();
    }

    private void ThrowOnNoSession()
    {
      if (Session == null)
        throw new VisaDeviceException(this,
          new InvalidOperationException("There is no opened VISA session to perform an operation."));
    }

    public override void Dispose()
    {
      CustomDisposables.ForEach(disposable => disposable.Dispose());
      base.Dispose();
    }
  }
}
