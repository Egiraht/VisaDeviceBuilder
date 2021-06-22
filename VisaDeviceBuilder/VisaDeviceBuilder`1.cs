using System;
using System.Linq;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  public class VisaDeviceBuilder<TBuildableVisaDevice> : IVisaDeviceBuilder
    where TBuildableVisaDevice : IBuildableVisaDevice, new()
  {
    protected TBuildableVisaDevice VisaDevice = new();

    public IVisaDeviceBuilder UseGlobalVisaResourceManager()
    {
      VisaDevice.ResourceManager = null;
      return this;
    }

    public IVisaDeviceBuilder UseCustomVisaResourceManager(IResourceManager resourceManager)
    {
      VisaDevice.ResourceManager = resourceManager;
      return this;
    }

    public IVisaDeviceBuilder UseResourceName(string resourceName)
    {
      VisaDevice.ResourceName = resourceName;
      return this;
    }

    public IVisaDeviceBuilder UseConnectionTimeout(int timeout)
    {
      VisaDevice.ConnectionTimeout = timeout;
      return this;
    }

    public IVisaDeviceBuilder UseVisaResourceManagerSelector(Func<IResourceManager> resourceManagerSelector)
    {
      VisaDevice.CustomResourceManagerSelector = resourceManagerSelector;
      return this;
    }

    public IVisaDeviceBuilder UseVisaResourceNameSelector(Func<string> resourceNameSelector)
    {
      VisaDevice.CustomResourceNameSelector = resourceNameSelector;
      return this;
    }

    public IVisaDeviceBuilder DefineSupportedHardwareInterfaces(params HardwareInterfaceType[] interfaces)
    {
      VisaDevice.CustomSupportedInterfaces.Clear();
      VisaDevice.CustomSupportedInterfaces.AddRange(interfaces.Distinct());
      return this;
    }

    public IVisaDeviceBuilder AddProperty<TValue>(IAsyncProperty<TValue> asyncProperty)
    {
      VisaDevice.CustomAsyncProperties.Add(asyncProperty);
      return this;
    }

    public IVisaDeviceBuilder AddProperty<TValue>(string name, Func<TValue> getterDelegate) =>
      AddProperty(new AsyncProperty<TValue>(getterDelegate) {Name = name, LocalizedName = name});

    public IVisaDeviceBuilder AddProperty<TValue>(string name, Func<TValue> getterDelegate,
      Converter<TValue, string> valueToStringConverter, Converter<string, TValue> stringToValueConverter) =>
      AddProperty(new AsyncProperty<TValue>(getterDelegate, valueToStringConverter, stringToValueConverter)
        {Name = name, LocalizedName = name});

    public IVisaDeviceBuilder AddProperty<TValue>(string name, Action<TValue> setterDelegate) =>
      AddProperty(new AsyncProperty<TValue>(setterDelegate) {Name = name, LocalizedName = name});

    public IVisaDeviceBuilder AddProperty<TValue>(string name, Action<TValue> setterDelegate,
      Converter<TValue, string> valueToStringConverter, Converter<string, TValue> stringToValueConverter) =>
      AddProperty(new AsyncProperty<TValue>(setterDelegate, valueToStringConverter, stringToValueConverter)
        {Name = name, LocalizedName = name});

    public IVisaDeviceBuilder AddProperty<TValue>(string name, Func<TValue> getterDelegate,
      Action<TValue> setterDelegate) =>
      AddProperty(new AsyncProperty<TValue>(getterDelegate, setterDelegate) {Name = name, LocalizedName = name});

    public IVisaDeviceBuilder AddProperty<TValue>(string name, Func<TValue> getterDelegate,
      Action<TValue> setterDelegate, Converter<TValue, string> valueToStringConverter,
      Converter<string, TValue> stringToValueConverter) =>
      AddProperty(new AsyncProperty<TValue>(getterDelegate, setterDelegate, valueToStringConverter,
        stringToValueConverter) {Name = name, LocalizedName = name});

    public IVisaDeviceBuilder AddProperties(params IAsyncProperty[] asyncProperties)
    {
      VisaDevice.CustomAsyncProperties.AddRange(asyncProperties);
      return this;
    }

    public IVisaDeviceBuilder ClearAllProperties()
    {
      VisaDevice.CustomAsyncProperties.Clear();
      return this;
    }

    public IVisaDeviceBuilder AddAction(IDeviceAction deviceAction)
    {
      VisaDevice.CustomDeviceActions.Add(deviceAction);
      return this;
    }

    public IVisaDeviceBuilder AddAction(string name, Action action) =>
      AddAction(new DeviceAction(action) {Name = name, LocalizedName = name});

    public IVisaDeviceBuilder AddActions(params IDeviceAction[] deviceActions)
    {
      VisaDevice.CustomDeviceActions.AddRange(deviceActions);
      return this;
    }

    public IVisaDeviceBuilder ClearAllActions()
    {
      VisaDevice.CustomDeviceActions.Clear();
      return this;
    }

    public IVisaDeviceBuilder UseInitializeCallback(Action<IMessageBasedSession> callback)
    {
      VisaDevice.CustomInitializeCallback = callback;
      return this;
    }

    public IVisaDeviceBuilder UseDeInitializeCallback(Action<IMessageBasedSession> callback)
    {
      VisaDevice.CustomDeInitializeCallback = callback;
      return this;
    }

    public IVisaDeviceBuilder UseGetIdentifierCallback(Func<IMessageBasedSession, string> callback)
    {
      VisaDevice.CustomGetIdentifierCallback = callback;
      return this;
    }

    public IVisaDeviceBuilder UseResetCallback(Action<IMessageBasedSession> callback)
    {
      VisaDevice.CustomResetCallback = callback;
      return this;
    }

    public IVisaDeviceBuilder RegisterDisposable(IDisposable disposable)
    {
      VisaDevice.CustomDisposables.Add(disposable);
      return this;
    }

    public IVisaDeviceBuilder RegisterDisposables(params IDisposable[] disposables)
    {
      VisaDevice.CustomDisposables.AddRange(disposables);
      return this;
    }

    public IVisaDevice BuildVisaDevice() => VisaDevice;
  }
}
