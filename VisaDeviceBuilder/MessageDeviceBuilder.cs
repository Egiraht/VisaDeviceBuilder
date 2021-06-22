using System;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  public class MessageDeviceBuilder : VisaDeviceBuilder<BuildableMessageDevice>, IMessageDeviceBuilder
  {
    internal MessageDeviceBuilder()
    {
    }

    public IMessageDeviceBuilder SetMessageProcessor(Func<IMessageBasedSession, string, string> processor)
    {
      VisaDevice.CustomMessageProcessor = processor;
      return this;
    }
  }
}
