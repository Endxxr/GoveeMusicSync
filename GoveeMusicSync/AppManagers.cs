using GoveeMusicSync.Managers;

namespace GoveeMusicSync
{
    public class AppManagers
    {
        public BluetoothConnectionManager? BluetoothConnectionManager { get; set; }
        public VideoSyncManager? VideoSyncManager { get; set; }

        public AudioSyncManager? AudioSyncManager { get; set; }

    }
}
