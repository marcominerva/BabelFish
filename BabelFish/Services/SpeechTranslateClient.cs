using BabelFish.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace BabelFish.Services
{
    public class SpeechTranslateClient
    {
        private const string SpeechTranslateUrl = @"wss://dev.microsofttranslator.com/speech/translate?from={0}&to={1}{2}&api-version=1.0";

        public delegate void OnSpeechResult(RecognitionResult result);
        public delegate void OnTextToSpeechData(AudioFrame frame);
        public delegate void OnConnectionClose();

        private readonly Authentication auth;
        private static readonly Encoding UTF8 = new UTF8Encoding();

        private MessageWebSocket webSocket;
        private DataWriter dataWriter;
        private Timer timer;

        private OnSpeechResult onSpeechTranslateResult;
        private OnTextToSpeechData onTextToSpeechData;
        private OnConnectionClose onConnectionClose;

        public bool IsSocketConnected { get; private set; }

        /// <summary>
        /// Create a speech tarnslate client that will talk to the MT Service
        /// </summary>
        public SpeechTranslateClient(string subscriptionKey)
        {
            auth = new Authentication(subscriptionKey);
        }

        public async Task<(IEnumerable<Language> Speech, IEnumerable<Language> Text, Dictionary<string, IList<Voice>> Voices)> GetLanguagesAndVoicesAsync()
        {
            var speech = new List<Language>();
            var text = new List<Language>();
            var voices = new Dictionary<string, IList<Voice>>();

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync("https://dev.microsofttranslator.com/languages?api-version=1.0&scope=speech,text,tts");
                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonObject = JObject.Parse(jsonString);

                foreach (var s in jsonObject["speech"])
                {
                    speech.Add(new Language(s.First()["language"].ToString(), s.First()["name"].ToString()));
                }

                foreach (var s in jsonObject["text"])
                {
                    text.Add(new Language(((JProperty)s).Name, s.First()["name"].ToString()));
                }

                foreach (var s in jsonObject["tts"])
                {
                    var lang = s.First()["language"].ToString();
                    if (!voices.ContainsKey(lang))
                    {
                        voices[lang] = new List<Voice>();
                    }

                    var value = s.First();
                    voices[lang].Add(new Voice(((JProperty)s).Name, $"{value["displayName"]} ({value["gender"]} {value["regionName"]})"));
                }
            }

            return (speech, text, voices);
        }

        /// <summary>
        /// Connect to the server before sending audio
        /// It will get the ADM credentials and add it to the header
        /// </summary>
        /// <returns></returns>
        public async Task Connect(string from, string to, string voice, OnSpeechResult onSpeechTranslateResult, OnTextToSpeechData onTextToSpeechData, OnConnectionClose onConnectionClose = null)
        {
            this.onSpeechTranslateResult = onSpeechTranslateResult;
            this.onTextToSpeechData = onTextToSpeechData;
            this.onConnectionClose = onConnectionClose;

            await DoConnectAsync(from, to, voice);
            IsSocketConnected = true;
        }

        public async Task InitializeAsync()
        {
            await auth.GetAccessTokenAsync();
        }

        private async Task DoConnectAsync(string from, string to, string voice)
        {
            webSocket = new MessageWebSocket();
            webSocket.MessageReceived += WebSocket_MessageReceived;
            webSocket.Closed += WebSocket_Closed;

            var token = "Bearer " + await auth.GetAccessTokenAsync();
            webSocket.SetRequestHeader("Authorization", token);

            var url = string.Format(SpeechTranslateUrl, from, to, voice == null ? string.Empty : "&features=texttospeech&voice=" + Uri.EscapeDataString(voice));

            try
            {
                dataWriter?.Dispose();
            }
            catch
            {
            }

            // setup the data writer
            dataWriter = new DataWriter(webSocket.OutputStream)
            {
                ByteOrder = ByteOrder.LittleEndian
            };
            dataWriter.WriteBytes(GetWaveHeader());

            // connect to the service
            await webSocket.ConnectAsync(new Uri(url));

            // flush the dataWriter periodically
            timer = new Timer(async (s) =>
            {
                if (dataWriter.UnstoredBufferLength > 0)
                {
                    try
                    {
                        await dataWriter.StoreAsync();
                    }
                    catch (Exception ex)
                    {
                        onSpeechTranslateResult?.Invoke(new RecognitionResult { Status = "DataWriter Failed: " + ex.Message });
                    }
                }

                // reset the timer
                timer?.Change(TimeSpan.FromMilliseconds(250), Timeout.InfiniteTimeSpan);
            },
            null, TimeSpan.FromMilliseconds(250), Timeout.InfiniteTimeSpan);
        }

        private void WebSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            IsSocketConnected = false;
            ResetTimer();

            onConnectionClose?.Invoke();

            onSpeechTranslateResult = null;
            onTextToSpeechData = null;
            onConnectionClose = null;
        }

        /// <summary>
        /// Process the response from the websocket
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void WebSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                if (args.MessageType == SocketMessageType.Utf8)
                {
                    // parse the text result that contains the recognition and translation
                    // {"type":"final","id":"0","recognition":"Hello, can you hear me now?","translation":"Hallo, kannst du mich jetzt hören?"}
                    string jsonOutput = null;
                    using (var dataReader = args.GetDataReader())
                    {
                        dataReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                        jsonOutput = dataReader.ReadString(dataReader.UnconsumedBufferLength);
                    }

                    var result = JsonConvert.DeserializeObject<RecognitionResult>(jsonOutput);
                    onSpeechTranslateResult(result);
                }
                else if (args.MessageType == SocketMessageType.Binary)
                {
                    // the binary output is the text to speech audio
                    using (var dataReader = args.GetDataReader())
                    {
                        dataReader.ByteOrder = ByteOrder.LittleEndian;
                        onTextToSpeechData(AudioFrameHelper.GetAudioFrame(dataReader));
                    }
                }
            }
            catch (Exception ex)
            {
                onSpeechTranslateResult(new RecognitionResult { Status = ex.Message });
            }
        }

        /// <summary>
        /// Send audio frame to the Machine Translation Service
        /// </summary>
        /// <param name="frame"></param>
        public void SendAudioFrame(AudioFrame frame)
            => AudioFrameHelper.SendAudioFrame(frame, dataWriter);

        /// <summary>
        /// Disconnect the service
        /// </summary>
        public void Close()
        {
            IsSocketConnected = false;
            ResetTimer();

            webSocket.Close(1000, "connection closed by client");
        }

        /// <summary>
        /// Create a RIFF Wave Header for PCM 16bit 16kHz Mono
        /// </summary>
        /// <returns></returns>
        private byte[] GetWaveHeader()
        {
            var channels = (short)1;
            var sampleRate = 16000;
            var bitsPerSample = (short)16;
            var extraSize = 0;
            var blockAlign = (short)(channels * (bitsPerSample / 8));
            var averageBytesPerSecond = sampleRate * blockAlign;

            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream, Encoding.UTF8);
                writer.Write(Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(0);
                writer.Write(Encoding.UTF8.GetBytes("WAVE"));
                writer.Write(Encoding.UTF8.GetBytes("fmt "));
                writer.Write(18 + extraSize); // wave format length
                writer.Write((short)1);// PCM
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(averageBytesPerSecond);
                writer.Write(blockAlign);
                writer.Write(bitsPerSample);
                writer.Write((short)extraSize);

                writer.Write(Encoding.UTF8.GetBytes("data"));
                writer.Write(0);

                stream.Position = 0;
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        private void ResetTimer()
        {
            if (timer != null)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
                timer = null;
            }
        }

        /// <summary>
        /// Dispose the websocket client object
        /// </summary>
        public void Dispose()
        {
            ResetTimer();

            if (webSocket != null)
            {
                webSocket.Dispose();
                webSocket = null;
            }
        }
    }

    /// <summary>
    /// IMemoryBuferByteAccess is used to access the underlying audioframe for read and write
    /// </summary>
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    internal static class AudioFrameHelper
    {
        /// <summary>
        /// This is a way to write to the audioframe
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="writer"></param>
        internal static void SendAudioFrame(AudioFrame frame, DataWriter writer)
        {
            var audioBuffer = frame.LockBuffer(AudioBufferAccessMode.Read);
            var buffer = Windows.Storage.Streams.Buffer.CreateCopyFromMemoryBuffer(audioBuffer);
            buffer.Length = audioBuffer.Length;

            using (var dataReader = DataReader.FromBuffer(buffer))
            {
                dataReader.ByteOrder = ByteOrder.LittleEndian;
                while (dataReader.UnconsumedBufferLength > 0)
                {
                    writer.WriteInt16(FloatToInt16(dataReader.ReadSingle()));
                }
            }
        }

        /// <summary>
        /// AudioFrame is in IEEE 32bit format.  We need to convert it to 16 bit PCM and send it to the datawriter
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="writer"></param>
        unsafe internal static void SendAudioFrameNative(AudioFrame frame, DataWriter writer)
        {
            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            {
                using (var reference = buffer.CreateReference())
                {
                    // Get the buffer from the AudioFrame
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out var dataInBytes, out var capacityInBytes);

                    // convert the bytes into float
                    var dataInFloat = (float*)dataInBytes;

                    for (var i = 0; i < capacityInBytes / sizeof(float); i++)
                    {
                        // convert the float into a double byte for 16 bit PCM
                        writer.WriteInt16(FloatToInt16(dataInFloat[i]));
                    }
                }
            }
        }

        /// <summary>
        /// The bytes that we get from audiograph is in IEEE float, we need to covert that to 16 bit
        /// before sending it to the speech translate service
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Int16 FloatToInt16(float value)
        {
            var f = value * Int16.MaxValue;

            if (f > Int16.MaxValue)
            {
                f = Int16.MaxValue;
            }
            else if (f < Int16.MinValue)
            {
                f = Int16.MinValue;
            }

            return (Int16)f;
        }

        /// <summary>
        /// Get the Text To Speech output and create an audio frame for the audio graph
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        unsafe internal static AudioFrame GetAudioFrame(DataReader reader)
        {
            var numBytes = reader.UnconsumedBufferLength;

            // The Text to Speech output contains the RIFF header for PCM 16bit 16kHz mono output
            // We do not need this for the audio graph
            var headerSize = 44;
            var bytes = new byte[headerSize];
            reader.ReadBytes(bytes);

            // skip the header
            var numSamples = (uint)(numBytes - headerSize);
            var frame = new AudioFrame(numSamples);

            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            {
                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    // Get the buffer from the AudioFrame to write to
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out var dataInBytes, out var capacityInBytes);

                    var dataInInt16 = (Int16*)dataInBytes;
                    for (var i = 0; i < capacityInBytes / sizeof(Int16); i++)
                    {
                        // write to the underlying stream
                        dataInInt16[i] = reader.ReadInt16();
                    }
                }
            }

            // return the frame for the audiograph to process
            return frame;
        }
    }
}
