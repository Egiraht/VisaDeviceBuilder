using System;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  public interface IMessageDeviceBuilder : IVisaDeviceBuilder
  {
    IMessageDeviceBuilder SetMessageProcessor(Func<IMessageBasedSession, string, string> processor);
  }
}
