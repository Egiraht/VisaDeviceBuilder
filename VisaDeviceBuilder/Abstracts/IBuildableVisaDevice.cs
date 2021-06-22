using System;
using System.Collections.Generic;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  public interface IBuildableVisaDevice : IVisaDevice
  {
    Func<IResourceManager>? CustomResourceManagerSelector { get; set; }

    Func<string>? CustomResourceNameSelector { get; set; }

    List<HardwareInterfaceType> CustomSupportedInterfaces { get; }

    List<IAsyncProperty> CustomAsyncProperties { get; }

    List<IDeviceAction> CustomDeviceActions { get; }

    Action<IMessageBasedSession>? CustomInitializeCallback { get; set; }

    Action<IMessageBasedSession>? CustomDeInitializeCallback { get; set; }

    Func<IMessageBasedSession, string>? CustomGetIdentifierCallback { get; set; }

    Action<IMessageBasedSession>? CustomResetCallback { get; set; }

    List<IDisposable> CustomDisposables { get; }
  }
}
