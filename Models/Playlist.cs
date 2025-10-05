using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Musium.Models
{
    public class Playlist : ObservableObject
    {
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

        static public Playlist GetPlaylistFromFile(string path)
        {
            Playlist playlist = new();
            XElement elem = XElement.Load(@$"{path}");
            var playlistElem = elem.Element("Playlist");
            Debug.WriteLine(elem.GetDefaultNamespace());

            return playlist;
        }
    }
}
