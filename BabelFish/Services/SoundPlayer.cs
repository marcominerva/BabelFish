using BabelFish.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace BabelFish.Services
{
    public enum Sounds
    {
        NoInternetConnection,
        Connected,
        Disconnected,
        Error
    }

    /// <summary>
    /// Delivers foreground sounds.
    /// </summary>
    public class SoundPlayer
    {
        private static MediaPlayer mediaPlayer;

        public static SoundPlayer Instance { get; } = new SoundPlayer();

        static SoundPlayer()
        {
            // Register media elements to the Sound Service.
            mediaPlayer = new MediaPlayer
            {
                Volume = 1,
                AudioCategory = MediaPlayerAudioCategory.Speech,
                AudioDeviceType = MediaPlayerAudioDeviceType.Communications
            };
        }

        public async void Play(byte[] buffer, string mimeType)
        {
            var stream = await ToInMemoryRandomAccessStreamAsync(buffer);
            Play(stream, mimeType);
        }

        public void Play(Stream stream, string mimeType)
            => Play(stream.AsRandomAccessStream(), mimeType);

        public void Play(IRandomAccessStream stream, string mimeType)
        {
            mediaPlayer.Source = MediaSource.CreateFromStream(stream, mimeType);
            mediaPlayer.Play();
        }

        public void Play(Sounds sound) => Play(sound.ToString());

        public void Play(string sound)
        {
            var source = $"ms-appx:///Audio/{sound}.mp3";

            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(source));
            mediaPlayer.Play();
        }

        private async Task<InMemoryRandomAccessStream> ToInMemoryRandomAccessStreamAsync(byte[] buffer)
        {
            var randomAccessStream = new InMemoryRandomAccessStream();
            await randomAccessStream.WriteAsync(buffer.AsBuffer());
            randomAccessStream.Seek(0);
            return randomAccessStream;
        }
    }
}
