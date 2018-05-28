using BabelFish.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace BabelFish
{
    sealed partial class App : Template10.Common.BootStrapper
    {
        public override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {            
            NavigationService.Navigate(typeof(MainPage));
            return Task.CompletedTask;
        }
    }
}
