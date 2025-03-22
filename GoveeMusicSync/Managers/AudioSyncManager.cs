using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;
using SpotifyAPI.Web;
using System.Drawing;
using System.Net.Http;
using GoveeMusicSync.Dialogs;
using System.IO;

namespace GoveeMusicSync.Managers
{
    public class AudioSyncManager
    {
        public bool Loop = false;
        public float Threshold = 0.07f;
        public float SmoothingFactor = 0.8f;
        private SpotifyClient spotifyClient;
        private string? clientID;
        private string? clientSecret;
        private ControlWindow controlWindow;
        private float smoothedBrightness = 0;
        private int sampleRate = 44100;
        private WasapiLoopbackCapture capture;
        private const int fftSize = 1024;

        private BluetoothConnectionManager bluetoothConnectionManager;



        public AudioSyncManager(ControlWindow control, BluetoothConnectionManager bluetoothConnectionManager)
        {
            this.controlWindow = control;
            this.bluetoothConnectionManager = bluetoothConnectionManager;
        }


        public async Task<FullTrack?> GetCurrentPlaying()
        {
            var response = await spotifyClient.Player.GetCurrentPlayback();
            var playableItem = response.Item;

            if (playableItem is FullTrack)
            {
                return (FullTrack)playableItem;
            }

            return null;
                
        }

        public async Task<Bitmap> GetAlbumImage(FullTrack track)
        {
            var albumImageUrl = track.Album.Images[0].Url;
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(albumImageUrl);
            var response = await httpClient.GetAsync(albumImageUrl);
            var stream = await response.Content.ReadAsStreamAsync();
            httpClient.Dispose();
            return new Bitmap(stream);
        }


        private async Task Authorize(bool refresh)
        {

            (clientID, clientSecret) = getClientFromConfig();

            if (clientID == null || clientSecret == null)
            {
                SpotifyDialog dialog = new SpotifyDialog();
                dialog.ShowDialog();
                clientID = dialog.ClientId;
                clientSecret = dialog.ClientSecret;

                if (dialog.DialogResult == false)
                {
                    return;
                }
                saveClientToConfig(clientID, clientSecret);
            }

            SpotifyEmbeddedAuth auth = new SpotifyEmbeddedAuth(clientID, clientSecret);
            auth.SpotifyClientGenerated += (sender, client) =>
            {
                spotifyClient = client;
                return Task.CompletedTask;
            };

            await auth.Auth(refresh);
        }


        public async void ToggleSync()
        {


            if (Loop)
            {
                Loop = false;
                capture.StopRecording();
                controlWindow.Currently_Playing.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                Loop = true;
                if (spotifyClient == null)
                {
                    await Authorize(false);
                }

                Thread audioThread = new(() => SyncAudio());
                audioThread.Start();
                Thread spotifyThread = new(async () => await SyncAlbumColor());
                spotifyThread.Start();
            }
        }



        private void SyncAudio()
        {
            capture = new WasapiLoopbackCapture();
            capture.DataAvailable += ProcessAudioData;
            capture.StartRecording();
            sampleRate = capture.WaveFormat.SampleRate;
        }

        private async Task SyncAlbumColor()
        {
            while (Loop)
            {   
                if (spotifyClient != null) 
                {
                    try 
                    {
                        FullTrack? track = await GetCurrentPlaying();
                            if (track != null)
                            {
                                Bitmap albumImage = await GetAlbumImage(track);
                                Color dominantColor = ImageHelper.GetDominantColor(albumImage);
                                if (ImageHelper.GetEuclideanDist(Color.Black, dominantColor) < 50)
                                {
                                    dominantColor = Color.White;
                                }

                                bluetoothConnectionManager.SetColor(dominantColor);
                            }
                    }
                    catch (APIUnauthorizedException exception)
                    {
                        await Authorize(true);
                    } finally
                    {
                        Thread.Sleep(5000);
                    }
                }
            }
        }

        private void ProcessAudioData(object sender, WaveInEventArgs e)
        {
            float[] samples = ConvertToFloatArray(e.Buffer, e.BytesRecorded);
            if (samples.Length == 0)
            {
                setNewBrightness(1);
                return;
            }

            // Analyze loudness
            float rms = (float)Math.Sqrt(samples.Average(s => s * s));
            float loudness = rms * 10;

            if (loudness == 0 || loudness <= Threshold)
            {
                setNewBrightness(1);
                return;
            }

            //Normalize loudness to 0.15f, to ensure that values are not overly boosted
            float normalizationFactor = 0.15f / loudness;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= normalizationFactor;
            }
            

            // Analyze frequency spectrum using bin normalization
            var (bassIntensity, mediumIntensity, highIntensity) = AnalyzeFrequencies(samples);

            if (bassIntensity == 0 && mediumIntensity == 0 && highIntensity == 0 && loudness == 0)
                return;

            if (bassIntensity == float.NaN || mediumIntensity == float.NaN || highIntensity == float.NaN || loudness == float.NaN)
                return;


            float newBrightness = (bassIntensity + mediumIntensity + highIntensity + loudness);
            setNewBrightness(newBrightness);
        }

        private void setNewBrightness(float newBrightness)
        {
            smoothedBrightness = SmoothingFactor * newBrightness + (1 - SmoothingFactor) * smoothedBrightness;
            smoothedBrightness = NormalizeValue(smoothedBrightness, 1, 10);

            // Convert to integer (0-10) and send to LED device
            int brightnessLevel = (int)Math.Round(smoothedBrightness);
            bluetoothConnectionManager.SetBrightness(brightnessLevel);
        }


        private float[] ConvertToFloatArray(byte[] buffer, int bytesRecorded)
        {
            int sampleCount = bytesRecorded / 4;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
                samples[i] = BitConverter.ToSingle(buffer, i * 4);

            return samples;
        }

        private (float bass, float mid, float high) AnalyzeFrequencies(float[] samples)
        {

            Complex[] fftBuffer = new Complex[fftSize];
            for (int i = 0; i < fftSize && i < samples.Length; i++)
            {
                fftBuffer[i] = new Complex(samples[i], 0);
            }

            Fourier.Forward(fftBuffer);

            float binSize = (float)sampleRate / fftSize;
            int bassStart = 0, bassEnd = (int)(250 / binSize);
            int midStart = bassEnd + 1, midEnd = (int)(4000 / binSize);
            int highStart = midEnd + 1, highEnd = (int)(20000 / binSize);

            float bass = fftBuffer.Skip(bassStart).Take(bassEnd - bassStart + 1).Sum(c => (float)Complex.Abs(c)) / (bassEnd - bassStart + 1);
            float mid = fftBuffer.Skip(midStart).Take(midEnd - midStart + 1).Sum(c => (float)Complex.Abs(c)) / (midEnd - midStart + 1);
            float high = fftBuffer.Skip(highStart).Take(highEnd - highStart + 1).Sum(c => (float)Complex.Abs(c)) / (highEnd - highStart + 1);

            return (bass*100, mid*400, high*400);
        }

        private float NormalizeValue(float value, float min, float max)
        {
            return Math.Clamp((value - min) / (max - min) * 10, 1, 10);
        }

        private void saveClientToConfig(string client_id, string client_secret)
        {

            StreamWriter streamWriter = new StreamWriter("client.txt");

            streamWriter.WriteLine(client_id);
            streamWriter.WriteLine(client_secret);

            streamWriter.Close();
        }

        private (string? client_id, string? client_secret) getClientFromConfig()
        {
            try
            {
                if (!File.Exists("client.txt"))
                {
                    return (null, null);
                }

                StreamReader streamReader = new StreamReader("client.txt");
                string? client_id = streamReader.ReadLine();
                string? client_secret = streamReader.ReadLine();
                streamReader.Close();
                return (client_id, client_secret);
            }
            catch (Exception ex)
            {
                return (null, null);
            }
        }
    }
}
