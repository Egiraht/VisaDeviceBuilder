using System.Threading;
using System.Windows;
using System.Windows.Threading;
using VisaDeviceBuilder.SampleApp.Components;
using VisaDeviceBuilder.WPF;
using Localization = VisaDeviceBuilder.SampleApp.Resources.Localization;

namespace VisaDeviceBuilder.SampleApp
{
  /// <summary>
  ///   The interaction logic class for the <i>MainWindow.xaml</i> window.
  /// </summary>
  public partial class MainWindow
  {
    /// <inheritdoc />
    public MainWindow()
    {
      Dispatcher.UnhandledException += OnUnhandledException;

      InitializeComponent();

      // Creating the control panel for KeysightE364xA VISA devices using the default VISA resource manager
      // (GlobalResourceManager class).
      // The assembly of the official Keysight VISA implementation library (Keysight.Visa.dll) is directly referenced
      // by the project, so the GlobalResourceManager can find and use it for device discovery and session management.
      // The Keysight IO Libraries Suite package must be installed before compilation.
      var controlPanel = new DeviceControlPanel(new KeysightE364xA())
      {
        IsAutoUpdaterEnabled = true,
        AutoUpdaterDelay = 10,
        IsMessageInputPanelEnabled = true,
        LocalizationResourceManager = Localization.ResourceManager
      };
      controlPanel.Exception += OnException;
      Content = controlPanel;
    }

    /// <summary>
    ///   The callback method handling the <see cref="Dispatcher" /> unhandled exceptions.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object.
    /// </param>
    /// <param name="args">
    ///   The exception event arguments.
    /// </param>
    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
      OnException(sender, new ThreadExceptionEventArgs(args.Exception));
      args.Handled = true;
    }

    /// <summary>
    ///   The callback method for handling the <see cref="DeviceControlPanel.Exception" /> events.
    /// </summary>
    /// <param name="sender">
    ///   The event sender object.
    /// </param>
    /// <param name="args">
    ///   The exception event arguments.
    /// </param>
    private void OnException(object sender, ThreadExceptionEventArgs args) => Dispatcher.Invoke(() =>
      MessageBox.Show(this, args.Exception.Message, Localization.Title, MessageBoxButton.OK, MessageBoxImage.Error));
  }
}
