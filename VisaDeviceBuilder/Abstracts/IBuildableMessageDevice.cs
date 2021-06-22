using System;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  public interface IBuildableMessageDevice : IBuildableVisaDevice
  {
    Func<IMessageBasedSession, string, string>? CustomMessageProcessor { get; set; }
  }
}
