using System;
using Ivi.Visa;

namespace VisaDeviceBuilder.Abstracts
{
  public interface IVisaDeviceBuilder
  {
    IVisaDeviceBuilder UseGlobalVisaResourceManager();

    IVisaDeviceBuilder UseCustomVisaResourceManager(IResourceManager resourceManager);

    IVisaDeviceBuilder UseResourceName(string resourceName);

    IVisaDeviceBuilder UseConnectionTimeout(int timeout);

    IVisaDeviceBuilder UseVisaResourceManagerSelector(Func<IResourceManager> resourceManagerSelector);

    IVisaDeviceBuilder UseVisaResourceNameSelector(Func<string> resourceNameSelector);

    IVisaDeviceBuilder DefineSupportedHardwareInterfaces(params HardwareInterfaceType[] interfaces);

    IVisaDeviceBuilder AddProperty<TValue>(IAsyncProperty<TValue> asyncProperty);

    IVisaDeviceBuilder AddProperty<TValue>(string name, Func<TValue> getterDelegate);

    IVisaDeviceBuilder AddProperty<TValue>(string name, Action<TValue> setterDelegate);

    IVisaDeviceBuilder AddProperty<TValue>(string name, Func<TValue> getterDelegate, Action<TValue> setterDelegate);

    IVisaDeviceBuilder AddProperties(params IAsyncProperty[] asyncProperties);

    IVisaDeviceBuilder ClearAllProperties();

    IVisaDeviceBuilder AddAction(IDeviceAction deviceAction);

    IVisaDeviceBuilder AddAction(string name, Action action);

    IVisaDeviceBuilder AddActions(params IDeviceAction[] deviceActions);

    IVisaDeviceBuilder ClearAllActions();

    IVisaDeviceBuilder UseInitializeCallback(Action<IMessageBasedSession> callback);

    IVisaDeviceBuilder UseDeInitializeCallback(Action<IMessageBasedSession> callback);

    IVisaDeviceBuilder UseGetIdentifierCallback(Func<IMessageBasedSession, string> callback);

    IVisaDeviceBuilder UseResetCallback(Action<IMessageBasedSession> callback);

    IVisaDeviceBuilder RegisterDisposable(IDisposable disposable);

    IVisaDeviceBuilder RegisterDisposables(params IDisposable[] disposables);

    IVisaDevice BuildVisaDevice();
  }
}
