// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Copyright © 2020-2021 Maxim Yudin

using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Ivi.Visa;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.WPF.Components;
using LocalizationResourceManager = System.Resources.ResourceManager;

namespace VisaDeviceBuilder.WPF
{
  /// <summary>
  ///   The view model class for the <see cref="DeviceControlPanel" /> WPF control. It is based on the
  ///   <see cref="VisaDeviceController" /> class and adds the WPF-specific command interaction logic.
  /// </summary>
  public class DeviceControlPanelViewModel : VisaDeviceController
  {
    private string _deviceLabel = string.Empty;

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

    /// <inheritdoc cref="IVisaDevice.ResourceManager" />
    public IResourceManager? ResourceManager
    {
      get => Device.ResourceManager;
      set
      {
        Device.ResourceManager = value;
        OnPropertyChanged();
      }
    }

    /// <inheritdoc cref="IVisaDevice.ResourceName" />
    public string ResourceName
    {
      get => Device.ResourceName;
      set
      {
        Device.ResourceName = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Checks if the device is a message device (its type implements the <see cref="IMessageDevice" /> interface).
    /// </summary>
    public bool IsMessageDevice => Device is IMessageDevice;

    /// <summary>
    ///   Gets the mutable collection of VISA resources available in the system. The collection may contain both
    ///   canonical VISA resource names and corresponding alias names if they are available.
    ///   The <see cref="UpdateAvailableVisaResourcesAsync" /> method can be used to update the collection.
    /// </summary>
    private ObservableCollection<string> AvailableVisaResourceEntries { get; } = new();

    /// <summary>
    ///   Gets the read-only collection of VISA resources available in the system. The collection may contain both
    ///   canonical VISA resource names and corresponding alias names if they are available.
    ///   The <see cref="UpdateAvailableVisaResourcesAsync" /> method can be used to update the collection.
    /// </summary>
    public ReadOnlyObservableCollection<string> AvailableVisaResources { get; }

    /// <summary>
    ///   Gets the mutable collection of asynchronous properties defined for the device.
    ///   The names of the asynchronous properties are localized using the <see cref="LocalizationResourceManager" /> if
    ///   possible (original names are used as keys when searching the manager for the localized name).
    /// </summary>
    private ObservableCollection<IAsyncProperty> LocalizedAsyncPropertyEntries { get; } = new();

    private bool _isUpdatingVisaResources;

    /// <summary>
    ///   Checks if the <see cref="AvailableVisaResources" /> property is being updated.
    /// </summary>
    public bool IsUpdatingVisaResources
    {
      get => _isUpdatingVisaResources;
      private set
      {
        _isUpdatingVisaResources = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Gets the read-only collection of asynchronous properties defined for the device.
    ///   The names of the asynchronous properties are localized using the <see cref="LocalizationResourceManager" /> if
    ///   possible (original names are used as keys when searching the manager for the localized name).
    /// </summary>
    public ReadOnlyObservableCollection<IAsyncProperty> LocalizedAsyncProperties { get; }

    /// <summary>
    ///   Gets the mutable collection of device actions defined for the device.
    ///   The names of the device actions are localized using the <see cref="LocalizationResourceManager" /> if
    ///   possible (original names are used as keys when searching the manager for the localized name).
    /// </summary>
    private ObservableCollection<IDeviceAction> LocalizedDeviceActionEntries { get; } = new();

    /// <summary>
    ///   Gets the read-only collection of device actions defined for the device.
    ///   The names of the device actions are localized using the <see cref="LocalizationResourceManager" /> if
    ///   possible (original names are used as keys when searching the manager for the localized name).
    /// </summary>
    public ReadOnlyObservableCollection<IDeviceAction> LocalizedDeviceActions { get; }

    private LocalizationResourceManager? _localizationResourceManager;

    /// <summary>
    ///   Gets or sets the optional ResX resource manager instance used for localization of the names of available
    ///   asynchronous properties and actions.
    ///   The provided localization resource manager must be able to accept the original names of the asynchronous
    ///   properties and actions and return their localized names.
    ///   If not provided, the original names will be used without localization.
    /// </summary>
    public LocalizationResourceManager? LocalizationResourceManager
    {
      get => _localizationResourceManager;
      set
      {
        _localizationResourceManager = value;
        OnPropertyChanged();
        RebuildCollections();
      }
    }

    private bool _isMessageInputPanelEnabled;

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

    private string _requestMessage = string.Empty;

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

    private string _responseMessage = string.Empty;

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

    private ICommand? _updateResourcesListCommand;

    /// <summary>
    ///   The command for updating the list of available VISA resources.
    /// </summary>
    public ICommand UpdateResourcesListCommand => _updateResourcesListCommand ??=
      new RelayCommand(_ => UpdateAvailableVisaResourcesAsync());

    private ICommand? _connectCommand;

    /// <summary>
    ///   The command for connecting to the device.
    /// </summary>
    public ICommand ConnectCommand => _connectCommand ??=
      new RelayCommand(_ => BeginConnect(), _ => !string.IsNullOrWhiteSpace(ResourceName) && CanConnect);

    private ICommand? _disconnectCommand;

    /// <summary>
    ///   The command for disconnecting from the device.
    /// </summary>
    public ICommand DisconnectCommand => _disconnectCommand ??=
      new RelayCommand(_ => BeginDisconnect(), _ => !CanConnect && !IsDisconnectionRequested);

    private ICommand? _updateAsyncPropertiesCommand;

    /// <summary>
    ///   The command for updating the asynchronous properties of the connected device.
    /// </summary>
    public ICommand UpdateAsyncPropertiesCommand => _updateAsyncPropertiesCommand ??=
      new RelayCommand(_ => UpdateAsyncPropertiesAsync(), _ => !IsUpdatingAsyncProperties);

    private ICommand? _sendMessageCommand;

    /// <summary>
    ///   The command for sending a message to the connected device.
    /// </summary>
    public ICommand SendMessageCommand => _sendMessageCommand ??=
      new RelayCommand(_ => SendMessageAsync(), _ => IsMessageDevice && IsDeviceReady);

    /// <summary>
    ///   Initializes a new view-model instance.
    /// </summary>
    /// <inheritdoc />
    public DeviceControlPanelViewModel(IVisaDevice device) : base(device)
    {
      AvailableVisaResources = new(AvailableVisaResourceEntries);
      LocalizedAsyncProperties = new(LocalizedAsyncPropertyEntries);
      LocalizedDeviceActions = new(LocalizedDeviceActionEntries);

      RebuildCollections();
    }

    /// <summary>
    ///   Rebuilds the collections of asynchronous properties and device actions and localizes the names using the
    ///   specified <see cref="LocalizationResourceManager" />.
    ///   If <see cref="LocalizationResourceManager" /> is not provided, the original names are used.
    /// </summary>
    private void RebuildCollections()
    {
      LocalizedAsyncPropertyEntries.Clear();
      LocalizedDeviceActionEntries.Clear();

      foreach (var asyncProperty in Device.AsyncProperties)
      {
        asyncProperty.Name = LocalizationResourceManager?.GetString(asyncProperty.Name) ?? asyncProperty.Name;
        LocalizedAsyncPropertyEntries.Add(asyncProperty);
      }

      foreach (var deviceAction in Device.DeviceActions)
      {
        deviceAction.Name = LocalizationResourceManager?.GetString(deviceAction.Name) ?? deviceAction.Name;
        LocalizedDeviceActionEntries.Add(deviceAction);
      }
    }

    /// <summary>
    ///   Asynchronously updates the <see cref="AvailableVisaResources" /> collection.
    /// </summary>
    public virtual async Task UpdateAvailableVisaResourcesAsync()
    {
      if (IsDisposed)
        throw new ObjectDisposedException(nameof(DeviceControlPanelViewModel));

      if (IsUpdatingVisaResources)
        return;
      IsUpdatingVisaResources = true;

      try
      {
        var resources = ResourceManager == null
          ? await VisaResourceLocator.LocateResourceNamesAsync()
          : await VisaResourceLocator.LocateResourceNamesAsync(ResourceManager);
        AvailableVisaResourceEntries.Clear();
        foreach (var resource in resources)
          AvailableVisaResourceEntries.Add(resource);
      }
      catch (Exception e)
      {
        OnException(e);
      }
      finally
      {
        IsUpdatingVisaResources = false;
      }
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
