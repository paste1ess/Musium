using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Musium.Controls;
using Musium.Models;
using Musium.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;

namespace Musium.Pages
{
    public sealed partial class InnerPlaylist : Page, INotifyPropertyChanged
    {
        private readonly AudioService Audio = AudioService.Instance;
        private readonly Random _rng = new Random();

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
       
        public InnerPlaylist()
        {
            InitializeComponent();
        }

        private BitmapImage? _displayedCoverArt;
        public BitmapImage? DisplayedCoverArt
        {
            get => _displayedCoverArt;
            set
            {
                _displayedCoverArt = value;
                OnPropertyChanged();
            }
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (Audio.CurrentViewedPlaylist is Playlist currentPlaylist)
            {
                if (currentPlaylist.ImageData is byte[] imageData)
                {
                    var stream = new InMemoryRandomAccessStream();
                    await stream.WriteAsync(imageData.AsBuffer());
                    stream.Seek(0);

                    var bitmapImage = new BitmapImage();
                    bitmapImage.DecodePixelWidth = 240;
                    await bitmapImage.SetSourceAsync(stream);

                    DisplayedCoverArt = bitmapImage;
                }
            }
        }
        private async void TrackItemControl_Clicked(object sender, RoutedEventArgs e)
        {
            var clickedControl = sender as TrackItemControl;
            if (clickedControl != null)
            {
                Audio.PlayAlbum(clickedControl.Song);
                Frame.Navigate(typeof(NowPlaying));
                MainWindow.UpdateNavigationViewSelection(typeof(NowPlaying));
            }
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (Audio.CurrentViewedPlaylist == null)
                return;
            var firstSong = Audio.CurrentViewedPlaylist.Songs.FirstOrDefault();
            if (firstSong == null)
                return;
            Audio.PlayAlbum(firstSong);
            Audio.SetShuffle(ShuffleState.Off);
            Frame.Navigate(typeof(NowPlaying));
            MainWindow.UpdateNavigationViewSelection(typeof(NowPlaying));
        }

        private async void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            if (Audio.CurrentViewedPlaylist == null) return;
            var songs = Audio.CurrentViewedPlaylist.Songs.ToList();
            if (!songs.Any()) return;

            var randomIndex = _rng.Next(songs.Count);
            var firstSong = songs.ElementAtOrDefault(randomIndex);
            if (firstSong == null) return;

            Audio.PlayAlbum(firstSong);
            Audio.SetShuffle(ShuffleState.Shuffle);
            Frame.Navigate(typeof(NowPlaying));
            MainWindow.UpdateNavigationViewSelection(typeof(NowPlaying));
        }
    }
}
