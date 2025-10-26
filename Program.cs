using System;
using System.Linq;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;

namespace AudioRouter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Starting AudioRouter web server on port 8888...");
            AudioRouter.Webserver.StartWebServer();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Listening for requests...");
            while(true) {
                Thread.Sleep(1000);
            }
        }

        public static Dictionary<string, string> GetDevices() {
            Dictionary<string, string> devices = new Dictionary<string, string>();
            foreach (var dev in DirectSoundOut.Devices)
            {
                devices.Add(dev.Guid.ToString(), dev.Description);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Found device: {dev.Description} ({dev.Guid})");
            }
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Total devices found: {devices.Count}");
            return devices;
        }

        public static void PlayOnDevice(string deviceGUID, string filename, string volume) {
            try {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ===== Play Request Received =====");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Device GUID: {deviceGUID}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] File: {filename}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Volume: {volume ?? "default"}");

                if (!System.IO.File.Exists(filename)) {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERROR: File not found: {filename}");
                    return;
                }

                var audioFile = new AudioFileReader(filename);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Audio file loaded. Duration: {audioFile.TotalTime}, Sample Rate: {audioFile.WaveFormat.SampleRate}Hz");

                ISampleProvider sampleProvider = audioFile;

                if(volume != null) {
                    float volumeLevel = float.Parse(volume);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Setting volume to: {volumeLevel} ({volumeLevel * 100}%)");

                    // Apply volume boost
                    var volumeProvider = new VolumeSampleProvider(sampleProvider);
                    volumeProvider.Volume = volumeLevel;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] VolumeSampleProvider.Volume = {volumeProvider.Volume}");
                    sampleProvider = volumeProvider;

                    // If overdriving (>1.0), apply soft clipping to prevent harsh distortion
                    // This allows the audio to be genuinely louder without hard clipping
                    if (volumeLevel > 1.0f) {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Applying soft clipping for overdrive protection");
                        sampleProvider = new SoftClippingSampleProvider(sampleProvider);
                    }
                } else {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Using default volume (100%)");
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Initializing output device...");
                var outputDevice = new DirectSoundOut(Guid.Parse(deviceGUID));
                outputDevice.Init(sampleProvider);

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Starting playback...");
                outputDevice.Play();

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(1000);
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Playback finished.");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ===== Play Request Complete =====");
            } catch (Exception ex) {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Stack trace: {ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Applies soft clipping (tanh saturation) to prevent harsh distortion when overdriving
    /// This allows audio to be genuinely louder while maintaining quality
    /// </summary>
    public class SoftClippingSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;

        public SoftClippingSampleProvider(ISampleProvider source)
        {
            this.source = source;
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);

            for (int i = offset; i < offset + samplesRead; i++)
            {
                // Apply tanh soft clipping - allows values >1.0 to be compressed smoothly
                // This is what guitar distortion pedals do!
                buffer[i] = (float)Math.Tanh(buffer[i]);
            }

            return samplesRead;
        }
    }
}
