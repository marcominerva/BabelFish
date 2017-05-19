using BabelFish.Common;
using BabelFish.Models;
using BabelFish.Services;
using BabelFish.ViewModels;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace BabelFish.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            ViewModel = DataContext as MainViewModel;

            TalkButton.AddHandler(PointerPressedEvent, new PointerEventHandler(TalkButton_PointerPressed), handledEventsToo: true);
            TalkButton.AddHandler(PointerReleasedEvent, new PointerEventHandler(TalkButton_PointerReleased), handledEventsToo: true);
        }

        private void TalkButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.StartTalkingCommand.Execute(null);
        }

        private void TalkButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.StopTalkingCommand.Execute(null);
        }
    }
}
