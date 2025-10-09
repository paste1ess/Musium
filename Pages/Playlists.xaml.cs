using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Musium.Models;
using Musium.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Musium.Pages
{
    public sealed partial class Playlists : Page
    {
        public readonly AudioService Audio = AudioService.Instance;
        public Playlists()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public ObservableCollection<Playlist> AllPlaylists = new();
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var playlist = await Playlist.GetPlaylistFromXSPFFile(new("file://C:\\Users\\jamied\\Documents\\testPlaylist.xspf"));
            if (playlist == null) return;
            foreach (Song song in playlist.Songs)
            {
                Debug.WriteLine(song.FilePath);
            }
            AllPlaylists.Add(playlist);
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedPlaylist = e.ClickedItem as Playlist;

            if (clickedPlaylist != null)
            {
                Audio.CurrentViewedPlaylist = clickedPlaylist;
                Frame.Navigate(typeof(InnerPlaylist));
                MainWindow.UpdateNavigationViewSelection(typeof(InnerPlaylist));
            }
        }
    }
}
