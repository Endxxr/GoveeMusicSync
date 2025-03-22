using System.Drawing;

namespace GoveeMusicSync.Managers
{

    //From https://github.com/tescfresc/LightstripSyncClient/blob/master/LightstripSync.Library/ColorSync.cs with some modifications

    public class VideoSyncManager
    {
        private bool _loop = false;
        public bool Loop { get { return _loop; } } 
        private Thread? videoThread;

        private readonly int bitmapRes = 150;
        private readonly double smoothSpeed = 0.8;
        private readonly int refreshRate = 50;

        public void ToggleSync(BluetoothConnectionManager bluetoothManager)
        {
            _loop = !_loop;
            if (_loop)
            {
                videoThread = new Thread(() => SyncLoop(bluetoothManager));
                videoThread.Start();
            }

        }

        private async void SyncLoop(BluetoothConnectionManager bluetoothManager)
        {
            var oldColor = Color.White;

            var width = (int) System.Windows.SystemParameters.PrimaryScreenWidth;
            var height = (int) System.Windows.SystemParameters.PrimaryScreenHeight;
            Bitmap bitmap;
            Graphics graphics;

            while (_loop)
            {
                using (bitmap = new Bitmap(width, height))
                {
                    using (graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                        bitmap = ResizeBitmap(bitmap, bitmapRes, bitmapRes);

                        var newColor = ImageHelper.GetDominantColor(bitmap);

                        newColor = ImageHelper.SmoothColor(oldColor, newColor, smoothSpeed);

                        bluetoothManager.SetColor(newColor);
                        oldColor = newColor;

                        bitmap.Dispose();
                        graphics.Dispose();
                    }
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(refreshRate);
            }

        }

        public Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            var result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }

    }
}
