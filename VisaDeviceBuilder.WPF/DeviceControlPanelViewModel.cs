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
    /// <summary>
    ///   Gets or sets the text label used for device distinguishing among the devices of similar type.
    /// </summary>
    public string DeviceLabel
    {
      get => !string.IsNullOrEmpty(_deviceLabel) ? _deviceLabel : Device.GetType().Name;
      set
      {
        _deviceLabel = value;
        OnPropertyChanged();
      }
    }
    private string _deviceLabel = string.Empty;

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
    private bool _isMessageInputPanelEnabled = false;

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
    private string _requestMessage = string.Empty;

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
    private string _responseMessage = string.Empty;

    /// <summary>
    ///   The command for updating the list of available VISA resources.
    /// </summary>
    public ICommand UpdateResourcesListCommand => _updateResourcesListCommand ??=
      new RelayCommand(_ => UpdateResourcesListAsync());
    private ICommand? _updateResourcesListCommand;

    /// <summary>
    ///   The command for connecting to the device.
    /// </summary>
    public ICommand ConnectCommand => _connectCommand ??=
      new RelayCommand(_ => BeginConnect(), _ => !string.IsNullOrWhiteSpace(ResourceName) && CanConnect);
    private ICommand? _connectCommand;

    /// <summary>
    ///   The command for disconnecting from the device.
    /// </summary>
    public ICommand DisconnectCommand => _disconnectCommand ??=
      new RelayCommand(_ => BeginDisconnect(), _ => !CanConnect && !IsDisconnectionRequested);
    private ICommand? _disconnectCommand;

    /// <summary>
    ///   The command for updating the asynchronous properties of the connected device.
    /// </summary>
    public ICommand UpdateAsyncPropertiesCommand => _updateAsyncPropertiesCommand ??=
      new RelayCommand(_ => UpdateAsyncPropertiesAsync(), _ => !IsUpdatingAsyncProperties);
    private ICommand? _updateAsyncPropertiesCommand;

    /// <summary>
    ///   The command for sending a message to the connected device.
    /// </summary>
    public ICommand SendMessageCommand => _sendMessageCommand ??=
      new RelayCommand(_ => SendMessageAsync(), _ => IsMessageDevice && IsDeviceReady);
    private ICommand? _sendMessageCommand;

    /// <summary>
    ///   Initializes a new view-model instance.
    /// </summary>
    /// <inheritdoc />
    public DeviceControlPanelViewModel(IVisaDevice device) : base(device)
    {
    }

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
            return ((IMessageDevice) Device).SendMessage(RequestMessage);
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
