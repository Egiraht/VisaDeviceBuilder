using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.WPF.Components;

namespace VisaDeviceBuilder.WPF
{
  /// <summary>
  ///   The view model class for the <see cref="DeviceControlPanel" /> WPF control. It is based on the
  ///   <see cref="VisaDeviceController" /> class and adds the WPF-specific command interaction logic.
  /// </summary>
  public class DeviceControlPanelViewModel : VisaDeviceController
  {
    /* The private backing fields. */
    private string _deviceLabel = string.Empty;
    private bool _isMessageInputPanelEnabled = false;
    private string _requestMessage = string.Empty;
    private string _responseMessage = string.Empty;
    private ICommand? _updateResourcesListCommand;
    private ICommand? _connectCommand;
    private ICommand? _disconnectCommand;
    private ICommand? _updateAsyncPropertiesCommand;
    private ICommand? _sendMessageCommand;

    /// <summary>
    ///   Gets or sets the text label used for device distinguishing among the devices of similar type.
    /// </summary>
    public string DeviceLabel
    {
      get => !string.IsNullOrEmpty(_deviceLabel) ? _deviceLabel : DeviceType.Name;
      set
      {
        _deviceLabel = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets or sets the flag indicating if the custom message input should be enabled.
    /// </summary>
    public bool IsMessageInputPanelEnabled
    {
      get => _isMessageInputPanelEnabled;
      set
      {
        _isMessageInputPanelEnabled = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets or sets the command message string to be sent to the device.
    /// </summary>
    public string RequestMessage
    {
      get => _requestMessage;
      set
      {
        _requestMessage = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets the command response string received from the device for the last command.
    /// </summary>
    public string ResponseMessage
    {
      get => _responseMessage;
      protected set
      {
        _responseMessage = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   The command for updating the list of available VISA resources.
    /// </summary>
    public ICommand UpdateResourcesListCommand => _updateResourcesListCommand ??=
      new RelayCommand(_ => UpdateResourcesListAsync());

    /// <summary>
    ///   The command for connecting to the device.
    /// </summary>
    public ICommand ConnectCommand => _connectCommand ??=
      new RelayCommand(_ => Connect(), _ => !string.IsNullOrWhiteSpace(ResourceName) && CanConnect);

    /// <summary>
    ///   The command for disconnecting from the device.
    /// </summary>
    public ICommand DisconnectCommand => _disconnectCommand ??=
      new RelayCommand(_ => DisconnectAsync(), _ => !CanConnect && !IsDisconnectionRequested);

    /// <summary>
    ///   The command for updating the asynchronous properties of the connected device.
    /// </summary>
    public ICommand UpdateAsyncPropertiesCommand => _updateAsyncPropertiesCommand ??=
      new RelayCommand(_ => UpdateAsyncPropertiesAsync(), _ => !IsUpdatingAsyncProperties);

    /// <summary>
    ///   The command for sending a message to the connected device.
    /// </summary>
    public ICommand SendMessageCommand => _sendMessageCommand ??=
      new RelayCommand(_ => SendMessageAsync(), _ => IsMessageDevice && IsDeviceReady);

    /// <summary>
    ///   Asynchronously sends the message to the connected device.
    /// </summary>
    public virtual async Task SendMessageAsync()
    {
      try
      {
        if (!IsMessageDevice || !IsMessageInputPanelEnabled)
          return;

        ResponseMessage = await Task.Run(() =>
        {
          lock (DisconnectionLock)
            return ((IMessageDevice) Device!).SendMessage(RequestMessage);
        });
      }
      catch (Exception exception)
      {
        OnException(exception);
      }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      base.OnPropertyChanged(propertyName);

      // Forces all RelayCommand instances to call their CanExecuteChanged events.
      // This fixes some buttons that do not get their states updated after corresponding RelayCommand execution.
      CommandManager.InvalidateRequerySuggested();
    }
  }
}
