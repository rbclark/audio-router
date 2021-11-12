using System;
using System.Threading;
using NAudio.Wave;
using System.Collections.Generic;

namespace AudioRouter
{
    class Program
    {
        static void Main(string[] args)
        {
            AudioRouter.Webserver.StartWebServer();
            while(true) {
                Thread.Sleep(1000);
            }
        }

        public static Dictionary<string, string> GetDevices() {
            Dictionary<string, string> devices = new Dictionary<string, string>();
            foreach (var dev in DirectSoundOut.Devices)
            {
                devices.Add(dev.Guid.ToString(), dev.Description);
            }
            return devices;
        }

        public static void PlayOnDevice(string deviceGUID, string filename, string volume) {
            var audioFile = new AudioFileReader(filename);
            if(volume != null) {
                audioFile.Volume = float.Parse(volume);
            }
            var outputDevice = new DirectSoundOut(Guid.Parse(deviceGUID));
            outputDevice.Init(audioFile);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
