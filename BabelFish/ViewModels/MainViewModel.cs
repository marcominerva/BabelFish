using BabelFish.Models;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using BabelFish.Common;
using Template10.Utils;
using System.Globalization;
using BabelFish.Services;
using Windows.Media.MediaProperties;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Media;
using Windows.Media.Devices;
using Windows.Media.Capture;
using GalaSoft.MvvmLight.Threading;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System.UserProfile;
using Windows.System.Profile;
using System.IO;
using System.Diagnostics;
using Windows.Storage;
using Newtonsoft.Json;
using Windows.Foundation.Metadata;
using Windows.System;

namespace BabelFish.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private AudioGraph audioGraph;
        private AudioFrameOutputNode speechTranslateOutputMode;
        private AudioFrameInputNode textToSpeechOutputNode;
        private AudioDeviceOutputNode speakerOutputNode;

        private SpeechTranslateClient speechClient;
        private readonly Dictionary<string, IList<Voice>> voices = new Dictionary<string, IList<Voice>>();

        private bool isTalking = false;

        private string message;
        public string Message
        {
            get => message;
            set => Set(ref message, value);
        }

        private bool isConnected;
        public bool IsConnected
        {
            get => isConnected;
            set => Set(ref isConnected, value);
        }

        private Language selectedSourceLanguage;
        public Language SelectedSourceLanguage
        {
            get => selectedSourceLanguage;
            set => Set(ref selectedSourceLanguage, value);
        }

        private Language selectedTranslationLanguage;
        public Language SelectedTranslationLanguage
        {
            get => selectedTranslationLanguage;
            set
            {
                if (Set(ref selectedTranslationLanguage, value))
                {
                    RaisePropertyChanged(() => Voices);
                    SelectedVoice = Voices?.FirstOrDefault();
                }
            }
        }

        private AudioDevice selectedInputDevice;
        public AudioDevice SelectedInputDevice
        {
            get => selectedInputDevice;
            set => Set(ref selectedInputDevice, value);
        }

        private AudioDevice selectedOutputDevice;
        public AudioDevice SelectedOutputDevice
        {
            get { return selectedOutputDevice; }
            set { Set(ref selectedOutputDevice, value); }
        }

        private Voice selectedVoice;
        public Voice SelectedVoice
        {
            get => selectedVoice;
            set => Set(ref selectedVoice, value);
        }

        private string statusMessage;
        public string StatusMessage
        {
            get => statusMessage ?? "Babel Fish";
            set => Set(ref statusMessage, value);
        }

        public ObservableCollection<AudioDevice> InputDevices { get; set; } = new ObservableCollection<AudioDevice>();

        public ObservableCollection<AudioDevice> OutputDevices { get; set; } = new ObservableCollection<AudioDevice>();

        public ObservableCollection<Language> SourceLanguages { get; set; } = new ObservableCollection<Language>();

        public ObservableCollection<Language> TranslationLanguages { get; set; } = new ObservableCollection<Language>();

        public IEnumerable<Voice> Voices
            => selectedTranslationLanguage != null
            && voices.ContainsKey(selectedTranslationLanguage.Code) ? voices[selectedTranslationLanguage.Code] : null;

        public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();

        public RelayCommand ConnectCommand { get; private set; }

        public RelayCommand DisconnectCommand { get; private set; }

        public RelayCommand StartTalkingCommand { get; private set; }

        public RelayCommand StopTalkingCommand { get; private set; }

        public RelayCommand ShutdownCommand { get; private set; }

        public MainViewModel()
        {
            CreateCommands();
        }

        private void CreateCommands()
        {
            ConnectCommand = new RelayCommand(async () => await DoConnectAsync());
            DisconnectCommand = new RelayCommand(async () => await DoDisconnectAsync());
            ShutdownCommand = new RelayCommand(DoShutdown);

            StartTalkingCommand = new RelayCommand(() =>
            {
                if (isConnected)
                {
                    isTalking = true;
                }
            });

            StopTalkingCommand = new RelayCommand(async () =>
            {
                await Task.Delay(2000);
                isTalking = false;
            });
        }

        private async Task<Settings> GetSettingsAsync()
        {
            try
            {
                var storageFolder = KnownFolders.DocumentsLibrary;
                var settingsFile = await storageFolder.GetFileAsync("settings.babelfish");
                var content = await FileIO.ReadTextAsync(settingsFile);

                return JsonConvert.DeserializeObject<Settings>(content);
            }
            catch
            {
                return null;
            }
        }

        private async Task DoConnectAsync()
        {
            try
            {
                StatusMessage = "Connecting to Speech Translate Service";

                await speechClient.Connect(selectedSourceLanguage.Code, selectedTranslationLanguage.Code, selectedVoice.Code, DisplayResult, SendAudioOutput, ConnectionClosed);

                StatusMessage = "Creating AudioGraph";

                var inputPcmEncoding = AudioEncodingProperties.CreatePcm(16000, 1, 16);
                var outputPcmEncoding = AudioEncodingProperties.CreatePcm(24000, 1, 16);

                // Construct the audio graph
                // mic -> Machine Translate Service
                var result = await AudioGraph.CreateAsync(
                  new AudioGraphSettings(AudioRenderCategory.Speech)
                  {
                      DesiredRenderDeviceAudioProcessing = AudioProcessing.Raw,
                      AudioRenderCategory = AudioRenderCategory.Speech,
                      EncodingProperties = inputPcmEncoding
                  });

                if (result.Status == AudioGraphCreationStatus.Success)
                {
                    audioGraph = result.Graph;

                    // mic -> machine translation speech translate
                    var microphone = await DeviceInformation.CreateFromIdAsync(selectedInputDevice.Id);

                    speechTranslateOutputMode = audioGraph.CreateFrameOutputNode(inputPcmEncoding);
                    audioGraph.QuantumProcessed += (s, a) =>
                    {
                        try
                        {
                            SendToSpeechTranslate(speechTranslateOutputMode.GetFrame());
                        }
                        catch
                        {
                        }
                    };

                    speechTranslateOutputMode.Start();

                    var micInputResult = await audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Speech, inputPcmEncoding, microphone);
                    if (micInputResult.Status == AudioDeviceNodeCreationStatus.Success)
                    {
                        micInputResult.DeviceInputNode.AddOutgoingConnection(speechTranslateOutputMode);
                        micInputResult.DeviceInputNode.Start();
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    // machine translation text to speech output -> speaker
                    var speakerOutputResult = await audioGraph.CreateDeviceOutputNodeAsync();
                    if (speakerOutputResult.Status == AudioDeviceNodeCreationStatus.Success)
                    {
                        speakerOutputNode = speakerOutputResult.DeviceOutputNode;
                        speakerOutputNode.Start();
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    textToSpeechOutputNode = audioGraph.CreateFrameInputNode(outputPcmEncoding);
                    textToSpeechOutputNode.AddOutgoingConnection(speakerOutputNode);
                    textToSpeechOutputNode.Start();

                    // start the graph
                    audioGraph.Start();
                }

                IsConnected = true;
                StatusMessage = "Ready";
                Play(Sounds.Connected);
            }
            catch
            {
                IsConnected = false;
                StatusMessage = "Connection erorr";
                Play(Sounds.ConnectionError);
            }
        }

        private Task DoDisconnectAsync()
        {
            try
            {
                // Disconnect from the service.
                if (speechClient.IsSocketConnected)
                {
                    speechClient.Close();
                }

                if (audioGraph != null)
                {
                    audioGraph.Stop();
                    audioGraph.Dispose();
                    audioGraph = null;
                }
            }
            catch
            {
            }

            IsConnected = false;
            StatusMessage = null;
            isTalking = false;
            Play(Sounds.Disconnected);

            return Task.CompletedTask;
        }

        private async void DisplayResult(RecognitionResult result)
        {
            var chatMessage = new ChatMessage
            {
                SourceLanguage = selectedSourceLanguage.Code,
                SourceText = result.Recognition,
                TranslationLanguage = selectedTranslationLanguage.Code,
                TranslatedText = result.Translation
            };

            await Dispatcher.DispatchAsync(() =>
            {
                if (result.HasError)
                {
                    // Plays the error sound.
                    Play(Sounds.Error);
                }

                // Adds the message to the list.
                Messages.Add(chatMessage);
            });
        }

        private async void ConnectionClosed()
        {
            await Dispatcher.DispatchAsync(async () =>
            {
                await DoDisconnectAsync();
            });
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            var settings = await GetSettingsAsync();
            speechClient = new SpeechTranslateClient(settings?.SpeechSubscriptionKey ?? Constants.SpeechSubscriptionKey);

            try
            {
                // Initializes services.
                await speechClient.InitializeAsync;

                // Gets speech languages.
                await LoadSpeechLanguagesAsync();

                // Gets audio input and output devices.
                await LoadInputDevicesAsync();
                await LoadOutputDevicesAsync();

                if (settings != null)
                {
                    SelectedSourceLanguage = SourceLanguages.FirstOrDefault(l => l.Code == settings.Source);
                    SelectedTranslationLanguage = TranslationLanguages.FirstOrDefault(l => l.Code == settings.Translation);
                    SelectedVoice = Voices.FirstOrDefault(l => l.Code == settings.Voice);

                    if (settings.AutoConnect)
                    {
                        await DoConnectAsync();
                    }
                }
            }
            catch
            {
                Play(Sounds.ConnectionError);
            }

            await base.OnNavigatedToAsync(parameter, mode, state);
        }

        private async Task LoadInputDevicesAsync()
        {
            foreach (var device in await DeviceInformation.FindAllAsync(MediaDevice.GetAudioCaptureSelector()))
            {
                InputDevices.Add(new AudioDevice(device.Id, device.Name));
            }

            SelectedInputDevice = InputDevices.FirstOrDefault();
        }

        private async Task LoadOutputDevicesAsync()
        {
            foreach (var device in await DeviceInformation.FindAllAsync(MediaDevice.GetAudioRenderSelector()))
            {
                OutputDevices.Add(new AudioDevice(device.Id, device.Name));
            }

            SelectedOutputDevice = OutputDevices.FirstOrDefault();
        }

        private string GetLanguage()
        {
            var language = GlobalizationPreferences.Languages[0];
            var culture = new CultureInfo(language);

            return culture.TwoLetterISOLanguageName;
        }

        private async Task LoadSpeechLanguagesAsync()
        {
            // Retrieves languages for speech recognition.
            var languages = await speechClient.GetLanguagesAndVoicesAsync();

            languages.Speech.ForEach(lang =>
            {
                SourceLanguages.Add(lang);
            });

            languages.Text.ForEach(lang =>
            {
                TranslationLanguages.Add(lang);
            });

            languages.Voices.ForEach(voice =>
            {
                voices.Add(voice.Key, voice.Value);
            });

            var language = GetLanguage();
            SelectedSourceLanguage = SourceLanguages.FirstOrDefault(l => l.Code == language) ?? SourceLanguages.FirstOrDefault(l => l.Code == "en") ?? SourceLanguages.FirstOrDefault();
            SelectedTranslationLanguage = TranslationLanguages.FirstOrDefault(l => l.Code == language) ?? TranslationLanguages.FirstOrDefault(l => l.Code == "en") ?? TranslationLanguages.FirstOrDefault();
        }

        /// <summary>
        /// Send the data from the mic to the speech translate client
        /// </summary>
        /// <param name="frame"></param>
        private void SendToSpeechTranslate(AudioFrame frame)
        {
            if (isTalking)
            {
                speechClient.SendAudioFrame(frame);
            }
        }

        /// <summary>
        /// Send the audio result to the speaker output node.
        /// </summary>
        /// <param name="frame"></param>
        private void SendAudioOutput(AudioFrame frame) => textToSpeechOutputNode.AddFrame(frame);

        private void Play(Sounds sound) => Play(sound.ToString());

        private void Play(string sound) => SoundPlayer.Instance.Play(sound);

        public void DoShutdown()
        {
            try
            {
                // Shutdowns the device immediately.
                if (ApiInformation.IsTypePresent("Windows.System.ShutdownManager"))
                {
                    SoundPlayer.Instance.Play(Sounds.Shutdown);
                    ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(3));
                }
            }
            catch
            {
            }
        }
    }
}
