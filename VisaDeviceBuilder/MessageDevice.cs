using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;

namespace VisaDeviceBuilder
{
  /// <summary>
  ///   The class for connectable VISA devices that use message-based communication.
  /// </summary>
  public class MessageDevice : VisaDevice, IBuildableMessageDevice<IMessageDevice>
  {
    /// <summary>
    ///   Defines the array of hardware interface types that support message-based communication.
    /// </summary>
    public static readonly HardwareInterfaceType[] MessageBasedHardwareInterfaceTypes =
    {
      HardwareInterfaceType.Gpib,
      HardwareInterfaceType.Serial,
      HardwareInterfaceType.Tcp,
      HardwareInterfaceType.Usb,
      HardwareInterfaceType.Vxi,
      HardwareInterfaceType.GpibVxi
    };

    /// <inheritdoc />
    protected override HardwareInterfaceType[] DefaultSupportedInterfaces => MessageBasedHardwareInterfaceTypes;

    /// <inheritdoc cref="IBuildableMessageDevice{TMessageDevice}.CustomMessageProcessor" />
    protected Func<IMessageDevice, string, string>? CustomMessageProcessor { get; set; }

    /// <inheritdoc />
    HardwareInterfaceType[]? IBuildableVisaDevice<IMessageDevice>.CustomSupportedInterfaces
    {
      get => CustomSupportedInterfaces;
      set => CustomSupportedInterfaces = value;
    }

    /// <inheritdoc />
    ObservableCollection<IAsyncProperty> IBuildableVisaDevice<IMessageDevice>.CustomAsyncProperties =>
      CustomAsyncProperties;

    /// <inheritdoc />
    ObservableCollection<IDeviceAction> IBuildableVisaDevice<IMessageDevice>.CustomDeviceActions =>
      CustomDeviceActions;

    /// <inheritdoc />
    Action<IMessageDevice>? IBuildableVisaDevice<IMessageDevice>.CustomInitializeCallback
    {
      get => CustomInitializeCallback;
      set => CustomInitializeCallback = value != null ? device => value.Invoke((IMessageDevice) device) : null;
    }

    /// <inheritdoc />
    Action<IMessageDevice>? IBuildableVisaDevice<IMessageDevice>.CustomDeInitializeCallback
    {
      get => CustomDeInitializeCallback;
      set => CustomDeInitializeCallback = value != null ? device => value.Invoke((IMessageDevice) device) : null;
    }

    /// <inheritdoc />
    Func<IMessageDevice, string>? IBuildableVisaDevice<IMessageDevice>.CustomGetIdentifierCallback
    {
      get => CustomGetIdentifierCallback;
      set => CustomGetIdentifierCallback = value != null ? device => value.Invoke((IMessageDevice) device) : null;
    }

    /// <inheritdoc />
    Action<IMessageDevice>? IBuildableVisaDevice<IMessageDevice>.CustomResetCallback
    {
      get => CustomResetCallback;
      set => CustomResetCallback = value != null ? device => value.Invoke((IMessageDevice) device) : null;
    }

    /// <inheritdoc />
    Func<IMessageDevice, string, string>? IBuildableMessageDevice<IMessageDevice>.CustomMessageProcessor
    {
      get => CustomMessageProcessor;
      set => CustomMessageProcessor = value;
    }

    /// <inheritdoc />
    public new IMessageBasedSession? Session => (IMessageBasedSession?) base.Session;

    /// <inheritdoc />
    /// <exception cref="VisaDeviceException">
    ///   The connected device does not support message-based VISA sessions.
    /// </exception>
    protected override void DefaultInitializeCallback()
    {
      if (base.Session is not IMessageBasedSession)
        throw new VisaDeviceException(this, new NotSupportedException(
          $"The connected device \"{AliasName}\" does not support message-based VISA sessions."));

      base.DefaultInitializeCallback();
    }

    /// <inheritdoc />
    public string SendMessage(string message)
    {
      ThrowWhenNoVisaSessionIsOpened();

      lock (SessionLock)
        return CustomMessageProcessor != null
          ? CustomMessageProcessor.Invoke(this, message)
          : DefaultMessageProcessor(message);
    }

    /// <inheritdoc />
    public Task<string> SendMessageAsync(string message) => Task.Run(() => SendMessage(message));

    /// <summary>
    ///   Defines the default message processor method.
    ///   It is called when no <see cref="CustomMessageProcessor" /> is set.
    ///   Can be overriden in derived classes.
    /// </summary>
    /// <param name="message">
    ///   The message string to send.
    /// </param>
    /// <returns>
    ///   The message response string returned by the device.
    /// </returns>
    protected virtual string DefaultMessageProcessor(string message)
    {
      Session!.FormattedIO.WriteLine(message);
      return Session.FormattedIO.ReadLine().TrimEnd('\x0A');
    }

    /// <inheritdoc />
    public override object Clone()
    {
      var clone = (MessageDevice) base.Clone();
      clone.CustomMessageProcessor = CustomMessageProcessor;
      return clone;
    }
  }
}
