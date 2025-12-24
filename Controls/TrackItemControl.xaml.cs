using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Musium.Models;
using Musium.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Musium.Controls;

public sealed partial class TrackItemControl : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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

    public static readonly DependencyProperty SongProperty = DependencyProperty.Register(
        "Song",
        typeof(Song),
        typeof(TrackItemControl),
        new PropertyMetadata(null, OnSongChanged));
    public Song Song
    {
        get => (Song)GetValue(SongProperty);
        set => SetValue(SongProperty, value);
    }

    public static readonly DependencyProperty TrackViewProperty = DependencyProperty.Register(
        "TrackView",
        typeof(bool),
        typeof(TrackItemControl),
        new PropertyMetadata(null));
    public bool TrackView
    {
        get => (bool)GetValue(TrackViewProperty);
        set => SetValue(TrackViewProperty, value);
    }

    public event RoutedEventHandler Clicked;
    private static async void OnSongChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as TrackItemControl;
        if (control != null && control.Song?.Album?.CoverArtData is byte[] imageData)
        {
            var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageData.AsBuffer());
            stream.Seek(0);

            var bitmapImage = new BitmapImage();
            bitmapImage.DecodePixelWidth = 48;
            await bitmapImage.SetSourceAsync(stream);

            control.DisplayedCoverArt = bitmapImage;
        }
        else if (control != null)
        {
            control.DisplayedCoverArt = null;
        }
    }
    public TrackItemControl()
    {
        InitializeComponent();
    }
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Clicked?.Invoke(this, e);
    }
    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        Song.Favorited = !Song.Favorited;
    }

    private void NextQueue_Click(object sender, RoutedEventArgs e)
    {
        QueueManagerService.Instance.InsertStartOfQueue(Song);
    }
    private void LastQueue_Click(object sender, RoutedEventArgs e)
    {
        QueueManagerService.Instance.InsertEndOfQueue(Song);
    }

    private void MenuFlyoutItemFav_Click(object sender, RoutedEventArgs e)
    {
        Song.Favorited = !Song.Favorited;
    }
}
