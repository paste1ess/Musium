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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;


namespace Musium.Controls;

public sealed partial class QueueListItemControl : UserControl, INotifyPropertyChanged
{
    public readonly AudioService Audio = AudioService.Instance;
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private BitmapImage? _displayedCoverArt;
    public BitmapImage? DisplayedCoverArt
    {
        get => _displayedCoverArt;
        private set
        {
            _displayedCoverArt = value;
            OnPropertyChanged();
        }
    }

    public static readonly DependencyProperty SongProperty = DependencyProperty.Register(
        "Song",
        typeof(Song),
        typeof(QueueListItemControl),
        new PropertyMetadata(null, OnSongChanged));

    public Song Song
    {
        get => (Song)GetValue(SongProperty);
        set => SetValue(SongProperty, value);
    }

    public QueueListItemControl()
    {
        InitializeComponent();
    }

    private static void OnSongChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is QueueListItemControl control)
        {
            _ = control.UpdateCoverArtAsync();
        }
    }

    private async Task UpdateCoverArtAsync()
    {
        if (Song?.Album?.CoverArtData is byte[] imageData)
        {
            try
            {
                var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(imageData.AsBuffer());
                stream.Seek(0);

                var bitmapImage = new BitmapImage();
                bitmapImage.DecodePixelWidth = 40;
                await bitmapImage.SetSourceAsync(stream);

                DisplayedCoverArt = bitmapImage;
            }
            catch (Exception ex)
            {
                DisplayedCoverArt = null;
            }
        }
        else
        {
            DisplayedCoverArt = null;
        }
    }

    private void RemoveQueue_Click(object sender, RoutedEventArgs e)
    {
        QueueManagerService.Instance.RemoveFromQueue(Song);
    }
}
