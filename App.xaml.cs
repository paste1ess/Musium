using Musium.Models;
using Musium.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using System.Threading.Tasks;
using System.Net.Http;

namespace Musium
{
    public partial class App : Application
    {
        public static Window MainWindow { get; private set; }
        public static HttpClient LyricHttpClient { get; private set; }
        public App()
        {
            InitializeComponent();
        }
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();

            LyricHttpClient = new HttpClient();
            LyricHttpClient.BaseAddress = new Uri("https://lrclib.net/api/");

            var Audio = AudioService.Instance;
            await Task.Run(async () =>
            {
                Audio.SetLibrary(SettingsService.Instance.LibraryPath);
            });
        }
    }
}
