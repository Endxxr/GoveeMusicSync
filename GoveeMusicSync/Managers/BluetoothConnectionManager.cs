using System.Collections.ObjectModel;
using System.Drawing;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
namespace GoveeMusicSync.Managers
{

    // reference https://github.com/tescfresc/LightstripSyncClient/blob/master/LightstripSync.Library/BluetoothLEConnectionManager.cs

    public class BluetoothConnectionManager
    {

        private const string GattUUID = "00010203-0405-0607-0809-0a0b0c0d2b11";
        private BluetoothLEDevice? _device;
        private GattCharacteristic? lightChar;
        private bool charFound = false;
        private Timer? keepAliveTimer;
        private bool lightsOn = false;
        private List<string> hexColors = ["FF0000", "00FFFF", "0000FF", "00008B", "ADD8E6", "800080", "FFFF00", "00FF00", "FF00FF", "FFC0CB", "FFFFFF", "C0C0C0", "808080", "FFA500", "A52A2A", "800000", "008000", "808000", "7FFFD4"];
        private int currentHexColorIndex = 0;
        private bool isRGBIC = false;
        public ObservableCollection<BluetoothLEDevice> FoundDevices { get; private set; } = new ObservableCollection<BluetoothLEDevice>();

        public BluetoothConnectionManager()
        {
        }

        public void GetAvailableDevices()
        {
            StartDeviceWatcher();
        }

        private void StartDeviceWatcher()
        {

            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
            DeviceWatcher deviceWatcher =
                    DeviceInformation.CreateWatcher(
                            BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                            requestedProperties,
                            DeviceInformationKind.AssociationEndpoint);

            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;

            deviceWatcher.Start();
        }

        public void TogglePower()
        {
            lightsOn = !lightsOn;
            var lightsString = lightsOn ? "3301010000000000000000000000000000000033" : "3301000000000000000000000000000000000032";
            SendData(StringToByteArray(lightsString), false);
        }

        public void TogglePower(bool state)
        {
            lightsOn = state;

            var lightsString = state ? "3301010000000000000000000000000000000033" : "3301000000000000000000000000000000000032";
            SendData(StringToByteArray(lightsString), false);
        }

        public void SetBrightness(double value)
        {
            var hexValue = ((int)((value / 10) * 255)).ToString("X");
            hexValue = hexValue.Length == 1 ? "0" + hexValue : hexValue;

            var brightnessString = "3304" + hexValue + "00000000000000000000000000000000";
            SendData(StringToByteArray(brightnessString), true);
        }

        public void SetColor(string hexColor)
        {
            var colorString = isRGBIC ? "33051501" + hexColor + "0000000000ff7f0000000000" : "330502" + hexColor + "00000000000000000000000000";
            SendData(StringToByteArray(colorString), true);
        }

        public void SetColor(Color color)
        {

            var hexColor = color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
            SetColor(hexColor);

        }

        public void SwitchColor()
        {
            if (currentHexColorIndex == hexColors.Count)
            {
                currentHexColorIndex = 0;
            }
            SetColor(hexColors[currentHexColorIndex]);
            currentHexColorIndex++;
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation info)
        {
            var device = await BluetoothLEDevice.FromIdAsync(info.Id);
            if (device.Name.StartsWith("ihom"))
            {
                FoundDevices.Add(device);
            }
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate info)
        {
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate info)
        {
            var device = FoundDevices.FirstOrDefault(d => d.DeviceId == info.Id);
            if (device != null)
            {
                FoundDevices.Remove(device);
            }
        }

        public async Task<bool> ConnectToDevice(BluetoothLEDevice device)
        {

            _device = device;
            
            if (checkRGBIC(device))
            {
                isRGBIC = true;
            }
            
            await GetGattCharacteristic();

            if (charFound)
            {
                StartKeepAliveTimer();
                lightsOn = true;
                return true;
            }

            return false;
        }

        //https://github.com/tescfresc/LightstripSyncClient/blob/1046d76b14ad202c03a7d99822002adbd549ec3b/LightstripSync.Library/BluetoothLEConnectionManager.cs#L87C1-L107C10
        private async Task GetGattCharacteristic()
        {
            var result = await _device.GetGattServicesAsync();
            if (result.Status == GattCommunicationStatus.Success)
            {
                var services = result.Services;
                foreach (var service in services)
                {
                    GattCharacteristicsResult characteristics = await service.GetCharacteristicsAsync();
                    foreach (var characteristic in characteristics.Characteristics)
                    {
                        if (characteristic.Uuid.ToString() == GattUUID)
                        {
                            lightChar = characteristic;
                            charFound = true;
                        }
                    }
                }
            }
        }

        private void StartKeepAliveTimer()
        {
            keepAliveTimer = new Timer(KeepAliveTick, null, 0, 2000);
        }

        public void StopKeepAliveTimer()
        {
            if (keepAliveTimer != null)
                keepAliveTimer.Dispose();
        }

        private void KeepAliveTick(Object? statusInfo)
        {
            var keepAlive = "aa010000000000000000000000000000000000ab";
            var bytes = StringToByteArray(keepAlive);
            SendData(bytes, false);
        }

        private async void SendData(byte[] data, bool DoChecksum)
        {
            if (DoChecksum)
            {
                data = CalculateCheckSum(data);
            }

            var writer = new DataWriter();
            writer.WriteBytes(data);
            _ = await lightChar.WriteValueAsync(writer.DetachBuffer());
        }

        //https://github.com/tescfresc/LightstripSyncClient/blob/1046d76b14ad202c03a7d99822002adbd549ec3b/LightstripSync.Library/BluetoothLEConnectionManager.cs#L192C1-L218C10
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public bool checkRGBIC(BluetoothLEDevice device)
        {
            return device.Name.Contains("ihoment_H6143");
        }

        private byte[] CalculateCheckSum(byte[] bytes)
        {
            //calculate checksum
            var checksum = 0;
            foreach (var item in bytes)
            {
                checksum ^= item;
            }


            //add checksum to end of bytearray
            var tempArray = new byte[bytes.Length + 1];
            bytes.CopyTo(tempArray, 0);
            tempArray[tempArray.Length - 1] = (byte)checksum;

            //reassign bytearray
            bytes = tempArray;

            return bytes;
        }

    }
}
