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
        private static XNamespace ns = "http://xspf.org/ns/0/";
        public static async Task<Playlist?> GetPlaylistFromXSPFFile(Uri uri)
        {
            Playlist playlist = new();
            playlist.Songs = new();
            if (!Path.Exists(uri.LocalPath)) return null;
            Debug.WriteLine("path exists");

            XElement elem = XElement.Load(@$"{uri.LocalPath}");
            if (elem.GetDefaultNamespace() != ns) return null;
            Debug.WriteLine("correct name space");

            var trackList = elem.Element(ns + "trackList");
            if (trackList == null) return null;
            Debug.WriteLine("trackList elem exists");

            var tracks = trackList.Elements("track");
            foreach (XElement trackElem in tracks)
            {
                if (trackElem.Element("location") is XElement locationElem)
                {
                    string location = locationElem.Value;
                    var song = await Audio.GetSongFromPathAsync(new(location));
                    if (song != null) playlist.Songs.Add(song);
                }
            }

            return playlist;
        }
    }
}
