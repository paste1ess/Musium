using Musium.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Musium.Models
{
    public class Playlist : ObservableObject
    {
        private static AudioService Audio { get; } = AudioService.Instance;
        private ObservableCollection<Song> _songs;
        public ObservableCollection<Song> Songs
        {
            get => _songs;
            set
            {
                _songs = value;
                OnPropertyChanged();
            }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }
        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }
        private byte[]? _imageData;
        public byte[]? ImageData
        {
            get => _imageData;
            set
            {
                _imageData = value;
                OnPropertyChanged();
            }
        }

        private static XNamespace ns = "http://xspf.org/ns/0/";
        public static async Task<Playlist?> GetPlaylistFromXSPFFile(Uri uri)
        {
            Playlist playlist = new();
            playlist.Songs = new();
            if (!Path.Exists(uri.LocalPath)) return null;

            XElement elem = XElement.Load(@$"{uri.LocalPath}");
            if (elem.GetDefaultNamespace() != ns) return null;

            var trackList = elem.Element(ns + "trackList");
            if (trackList == null) return null;

            var title = elem.Element(ns + "title");
            playlist.Title = title?.Value ?? Path.GetFileNameWithoutExtension(uri.LocalPath);

            var tracks = trackList.Elements(ns + "track");

            foreach (XElement trackElem in tracks)
            {
                if (trackElem.Element(ns + "location") is XElement locationElem)
                {
                    string location = locationElem.Value;

                    try
                    {
                        Uri baseUri = new Uri(Path.GetDirectoryName(uri.LocalPath) + Path.DirectorySeparatorChar);
                        Uri finalUri = new Uri(baseUri, location);

                        Debug.WriteLine(finalUri.LocalPath);

                        var song = await Audio.GetSongFromPathAsync(finalUri);
                        if (song != null) playlist.Songs.Add(song);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
            }

            return playlist;
        }
    }
}
