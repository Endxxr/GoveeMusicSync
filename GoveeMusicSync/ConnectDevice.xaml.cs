using GoveeMusicSync.Managers;
using System.Windows;
using System.Windows.Controls;
using Windows.Devices.Bluetooth;

namespace GoveeMusicSync;

/// <summary>
/// Interaction logic for ConnectDevice.xaml
/// </summary>
public partial class ConnectWindow : Window
{

    private readonly AppManagers appManagers;

    public ConnectWindow()
    {
        InitializeComponent();
        appManagers = new AppManagers();
        
        BluetoothConnectionManager manager = new BluetoothConnectionManager();
        appManagers.BluetoothConnectionManager = manager;
        manager.FoundDevices.CollectionChanged += Device_CollectionChanged;
        manager.GetAvailableDevices();
    }

   
    private void Device_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var item = new ListViewItem();
                    foreach (var device in e.NewItems)
                    {
                        var bleDevice = (BluetoothLEDevice)device;
                        item.Content = bleDevice.Name;
                        item.Tag = bleDevice;
                        DeviceList.Items.Add(item);
                    }
                });
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var device in e.OldItems)
                    {
                        var bleDevice = (BluetoothLEDevice)device;
                        var item = DeviceList.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == bleDevice);
                        DeviceList.Items.Remove(item);
                    }
                });
                break;
            default:
                break;
        }
    }


    private void Device_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceList.SelectedItem != null)
        {
            Connect_Button.Cursor = System.Windows.Input.Cursors.Arrow;
        }
        else
        {
            Connect_Button.Cursor = System.Windows.Input.Cursors.No;
        }
    }


    private async void Connect_Button_Click(object sender, RoutedEventArgs e)
    {
        if (Connect_Button.Cursor == System.Windows.Input.Cursors.No)
        {
            return;
        }
        
        var deviceView = (ListViewItem)DeviceList.SelectedItem;
        var bluetoothDevice = (BluetoothLEDevice)deviceView.Tag;

        Connect_Button.Content = "Connecting...";
        this.Title = "Connecting to " + bluetoothDevice.Name;
        ProgressBar.Visibility = Visibility.Visible;

        bool connected = await appManagers.BluetoothConnectionManager.ConnectToDevice(bluetoothDevice);
        if (connected)
        {
            Window connectWindow = App.Current.MainWindow;

            ControlWindow controlWindow = new ControlWindow(appManagers);
            App.Current.MainWindow = controlWindow;

            connectWindow.Close();
            controlWindow.Show();
        }

    }


}