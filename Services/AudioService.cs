using ABI.Microsoft.UI.Xaml;
using DiscordRPC;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualBasic;
using Musium.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TagLib;
using TagLib.Riff;
using Windows.Devices.Radios;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.WebUI;

namespace Musium.Services
{
    public enum RepeatState
    {
        Off,
        Repeat,
        RepeatOne
    }
    public enum ShuffleState
    {
        Off,
        Shuffle
    }
    public record class LyricResult(
        int? code = null,
        int? id = null,
        string? trackName = null,
        string? artistName = null,
        string? albumName = null,
        float? duration = null,
        bool? instrumental = null,
        string? plainLyrics = null,
        string? syncedLyrics = null
    );
    public class AudioService : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private DispatcherQueue dispatcherQueue;

        private MediaPlayer _mediaPlayer;
        public event EventHandler<TimeSpan> PositionChanged;
        private SystemMediaTransportControls _systemMediaTransportControls;

        private readonly Random _rng = new Random();
        public DiscordRpcClient DiscordClient;

        public bool RPCEnabled = true;

        private RepeatState _currentRepeatState = RepeatState.Off;
        public RepeatState CurrentRepeatState
        {
            get => _currentRepeatState;
            private set
            {
                _currentRepeatState = value;
                OnPropertyChanged();
            }
        }

        private ShuffleState _currentShuffleState = ShuffleState.Off;
        public ShuffleState CurrentShuffleState
        {
            get => _currentShuffleState;
            private set
            {
                _currentShuffleState = value;
                OnPropertyChanged();
            }
        }
        public void CycleRepeat()
        {
            switch (CurrentRepeatState)
            {
                case RepeatState.Off:
                    CurrentRepeatState = RepeatState.Repeat;
                    break;
                case RepeatState.Repeat:
                    CurrentRepeatState = RepeatState.RepeatOne;
                    break;
                case RepeatState.RepeatOne:
                    CurrentRepeatState = RepeatState.Off;
                    break;
                default:
                    CurrentRepeatState = RepeatState.Repeat;
                    break;
            }
        }
        private AudioService()
        {
            _mediaPlayer = new MediaPlayer();

            _mediaPlayer.PlaybackSession.PositionChanged += OnPlaybackSessionChanged;
            _mediaPlayer.CurrentStateChanged += OnCurrentStateChanged;
            _mediaPlayer.MediaEnded += OnMediaEnded;

            DiscordClient = new DiscordRpcClient("1453167190663495722"); // hard coded :( will "probably" change in the future

            DiscordClient.OnReady += (sender, e) =>
            {
                Debug.WriteLine("Connected to discord with user {0}", e.User.Username);
                Debug.WriteLine("Avatar: {0}", e.User.GetAvatarURL(User.AvatarFormat.WebP));
            };

            DiscordClient.Initialize();

            SetDiscordRPC();
        }
        public void Dispose()
        {
            DiscordClient.Dispose();
        }

        public void SetDiscordRPC()
        {
            if (!RPCEnabled)
            {
                DiscordClient.ClearPresence();
                return;
            }
            if (CurrentSongPlaying == null)
            {
                DiscordClient.ClearPresence();
            } else
            {
                var ts = new Timestamps() 
                { 
                    Start = DateTime.UtcNow.AddSeconds(-_mediaPlayer.Position.TotalSeconds), 
                    End = DateTime.UtcNow.AddSeconds(CurrentSongPlaying.Duration.TotalSeconds - _mediaPlayer.Position.TotalSeconds) 
                };
                //var a = new Assets()
                //{
                //    LargeImageKey = "logo",
                //    LargeImageText = "",
                //    LargeImageUrl = "",
                //    SmallImageKey = "0",
                //    SmallImageText = "0",
                //    SmallImageUrl = "0"
                //};
                DiscordClient.SetPresence(new RichPresence()
                {
                    Details = CurrentSongPlaying.Title,
                    State = CurrentSongPlaying.ArtistName,
                    Timestamps = ts,
                    StatusDisplay = StatusDisplayType.State,
                    Type = ActivityType.Listening,
                    //Assets = a // for some goddamn reason the library hates me and will NOT let me have no SmallImage. so for now i just wont have it
                });
            }
        }
                

        private List<Artist> Database = [];
        public ObservableCollection<Playlist> Playlists = [];
        private List<Song> _fullCurrentSongList = [];

        public void PlaySongList(List<Song> inputSongList, Song startingSong)
        {
            bool shuffled = CurrentShuffleState == ShuffleState.Shuffle;
            var songList = new List<Song>(inputSongList);

            _fullCurrentSongList = inputSongList;

            if (shuffled) shuffleSongList(songList);
            songList.Remove(startingSong);
            QueueManagerService.Instance.ReplaceQueueWithList(songList);
            PlaySong(startingSong);
        }
        private void shuffleSongList(List<Song> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int k = _rng.Next(i + 1);
                var temp = list[i];
                list[i] = list[k];
                list[k] = temp;
            }
        }
        
        public void ToggleShuffle()
        {
            CurrentShuffleState = CurrentShuffleState == ShuffleState.Off ? ShuffleState.Shuffle : ShuffleState.Off;
            ShuffleLogic();
        }

        public void SetShuffle(ShuffleState newState)
        {
            CurrentShuffleState = newState; 
            ShuffleLogic();
        }
        

        private void ShuffleLogic()
        {
            var songList = new List<Song>(_fullCurrentSongList);
            if (CurrentShuffleState == ShuffleState.Shuffle)
            {
                shuffleSongList(songList);
                songList.Remove(CurrentSongPlaying);
                QueueManagerService.Instance.ReplaceQueueWithList(songList);
            }
            else
            {
                QueueManagerService.Instance.ReplaceQueueWithUnshuffledList(songList, CurrentSongPlaying);
            }
        }

        private Artist? GetArtist(string name)
        {
            foreach (Artist artist in Database)
            {
                if (artist.Name == name)
                {
                    return artist;
                }
            }
            return null;
        }
        public async Task<Song?> GetSongFromPathAsync(Uri path)
        {
            return await Task.Run(() =>
            {
                List<Song> songs = new List<Song>();
                foreach (Artist artist in Database)
                {
                    foreach (Album album in artist.Albums)
                    {
                        foreach (Song song in album.Songs)
                        {
                            if (song.FilePath == path.LocalPath) return song;
                        }
                    }
                }
                return null;
            });

        }

        public async Task<List<Song>> GetAllTracksAsync()
        {
            return await Task.Run(() =>
            {
                List<Song> songs = new List<Song>();
                foreach (Artist artist in Database)
                {
                    foreach (Album album in artist.Albums)
                    {
                        foreach (Song song in album.Songs)
                        {
                            songs.Add(song);
                        }
                    }
                }
                return songs;
            });
        }

        public async Task<List<Album>> GetAllAlbumsAsync()
        {
            return await Task.Run(() =>
            {
                List<Album> albums = new List<Album>();
                foreach (Artist artist in Database)
                {
                    foreach (Album album in artist.Albums)
                    {
                        albums.Add(album);
                    }
                }
                return albums;
            });
        }
        public void SetDispatcherQueue(DispatcherQueue newdq)
        {
            dispatcherQueue = newdq;
        }
        
        private void UpdateSystemTimeline()
        {
            var timelineProperties = new SystemMediaTransportControlsTimelineProperties();

            timelineProperties.StartTime = TimeSpan.FromSeconds(0);
            timelineProperties.MinSeekTime = TimeSpan.FromSeconds(0);
            timelineProperties.Position = _mediaPlayer.PlaybackSession.Position;
            timelineProperties.MaxSeekTime = _mediaPlayer.PlaybackSession.NaturalDuration;
            timelineProperties.EndTime = _mediaPlayer.PlaybackSession.NaturalDuration;

            _systemMediaTransportControls.UpdateTimelineProperties(timelineProperties);
        }
        private void OnPlaybackSessionChanged(MediaPlaybackSession sender, object args)
        {
            PositionChanged?.Invoke(this, sender.Position);
            UpdateSystemTimeline();
            SetDiscordRPC();
        }
        private void OnCurrentStateChanged(MediaPlayer sender, object args)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                CurrentState = sender.CurrentState;
            });
            switch (sender.CurrentState)
            {
                case MediaPlayerState.Playing:
                    _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case MediaPlayerState.Paused:
                    _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case MediaPlayerState.Stopped:
                    _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    break;
                case MediaPlayerState.Closed:
                    _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                    break;
                default:
                    break;
            }
            UpdateSystemTimeline();
            SetDiscordRPC();
        }
        private void OnMediaEnded(MediaPlayer sender, object args)
        {
            NextSong();
        }

        public void TogglePlayback()
        {
            switch (CurrentState)
            {
                case MediaPlayerState.Closed:
                    break;
                case MediaPlayerState.Opening:
                    break;
                case MediaPlayerState.Buffering:
                    Pause();
                    break;
                case MediaPlayerState.Playing:
                    Pause();
                    break;
                case MediaPlayerState.Paused:
                    Resume();
                    break;
                case MediaPlayerState.Stopped:
                    break;
                default:
                    break;
            }
        }

        public void NextSong()
        {
            var currentSong = CurrentSongPlaying;

            if (CurrentRepeatState == RepeatState.RepeatOne && currentSong != null)
            {
                PlaySong(currentSong);
                return;
            }

            var songToPlay = QueueManagerService.Instance.FirstInQueue();
            if (songToPlay != null) // there is a song to play next
            {
                PlaySong(songToPlay);

                dispatcherQueue.TryEnqueue(() =>
                {
                    if (currentSong != null)
                    {
                        QueueManagerService.Instance.InsertEndOfHistory(currentSong);
                        if (QueueManagerService.Instance.History.Count > 100) // capped history at 100, dunno if people actually use it past 100 songs so make PR/issue if u do
                        {
                            QueueManagerService.Instance.RemoveFirstInHistory();
                        }
                    }
                    QueueManagerService.Instance.RemoveFromQueue(songToPlay);
                });
            }
            else if (CurrentRepeatState == RepeatState.Repeat) // there is no song to play next and repeat is enabled
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    var list = _fullCurrentSongList;
                    if (CurrentShuffleState == ShuffleState.Shuffle) shuffleSongList(list);
                    QueueManagerService.Instance.ReplaceQueueWithList(list);

                    var firstSongInNewQueue = QueueManagerService.Instance.FirstInQueue();
                    if (firstSongInNewQueue != null)
                    {
                        PlaySong(firstSongInNewQueue);
                        QueueManagerService.Instance.RemoveFromQueue(firstSongInNewQueue);
                    }
                });
            }
        }
        public void PreviousSong()
        {
            if (_mediaPlayer.PlaybackSession.Position.TotalMilliseconds > 3000 || QueueManagerService.Instance.History.Count <= 0) // rewind goes to previous song if done in under 3 seconds
            {
                ScrubTo(0);
                return;
            }

            var currentSong = CurrentSongPlaying;

            var songToPlay = QueueManagerService.Instance.LastInHistory();
            if (songToPlay != null)
            {
                PlaySong(songToPlay);

                dispatcherQueue.TryEnqueue(() =>
                {
                    QueueManagerService.Instance.RemoveFromHistory(songToPlay);
                    if (currentSong != null)
                    {
                        QueueManagerService.Instance.InsertStartOfQueue(currentSong);
                    }
                });
            }
        }

        private static AudioService _instance;
        public static AudioService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AudioService();
                }
                return _instance;
            }
        }

        private Song? _currentSongPlaying;
        public Song? CurrentSongPlaying
        {
            get => _currentSongPlaying;
            private set
            {
                _currentSongPlaying = value;
                OnPropertyChanged();                
            }
        }

        private MediaPlayerState _currentState;
        public MediaPlayerState CurrentState
        {
            get => _currentState;
            private set
            {
                _currentState = value;
                OnPropertyChanged();
            }
        }

        private Album? _currentViewedAlbum;
        public Album? CurrentViewedAlbum
        {
            get => _currentViewedAlbum;
            set
            {
                if (_currentViewedAlbum != value)
                {
                    _currentViewedAlbum = value;
                    OnPropertyChanged();
                }
            }
        }

        private Playlist? _currentViewedPlaylist;
        public Playlist? CurrentViewedPlaylist
        {
            get => _currentViewedPlaylist;
            set
            {
                if (_currentViewedPlaylist != value)
                {
                    _currentViewedPlaylist = value;
                    OnPropertyChanged();
                }
            }
        }

        public void Pause()
        {
            _mediaPlayer?.Pause();
        }
        public void Resume()
        {
            _mediaPlayer?.Play();
        }
        private MediaPlayerElement _element;
        public void SetMediaPlayer(MediaPlayerElement element)
        {
            _element = element;
            element.SetMediaPlayer(_mediaPlayer);

            _systemMediaTransportControls = _mediaPlayer.SystemMediaTransportControls;
            _mediaPlayer.CommandManager.IsEnabled = false;
            _systemMediaTransportControls.IsPlayEnabled = true;
            _systemMediaTransportControls.IsPauseEnabled = true;
            _systemMediaTransportControls.IsPreviousEnabled = true;
            _systemMediaTransportControls.IsNextEnabled = true;
            _systemMediaTransportControls.IsRewindEnabled = true;
            _systemMediaTransportControls.IsFastForwardEnabled = true;
            _systemMediaTransportControls.IsEnabled = true;


            _systemMediaTransportControls.ButtonPressed += smtc_ButtonPressed;
        }
        private void smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Next:
                    NextSong();
                    break;

                case SystemMediaTransportControlsButton.Previous:
                    PreviousSong();
                    break;

                case SystemMediaTransportControlsButton.Play:
                    Resume();
                    break;

                case SystemMediaTransportControlsButton.Pause:
                    Pause();
                    break;
            }
        }

        private async Task<MediaSource> CreateMediaSourceFromMemoryAsync(string filePath)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
            IBuffer buffer = await FileIO.ReadBufferAsync(file);

            var memoryStream = new InMemoryRandomAccessStream();
            await memoryStream.WriteAsync(buffer);

            memoryStream.Seek(0);

            var tagFile = TagLib.File.Create(filePath);
            var mimeType = tagFile.MimeType.ToString();
            return MediaSource.CreateFromStream(memoryStream, mimeType);
        }
        public async void PlaySong(Song song)
        {
            var source = await CreateMediaSourceFromMemoryAsync(song.FilePath);
            var playbackItem = new MediaPlaybackItem(source);

            SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Title = song.Title;
            updater.MusicProperties.Artist = song.ArtistName;

            using (var memoryStream = new MemoryStream(song.Album.CoverArtData ?? []))
            {
                var randomAccessStream = new InMemoryRandomAccessStream();

                await RandomAccessStream.CopyAsync(
                    memoryStream.AsInputStream(),
                    randomAccessStream.GetOutputStreamAt(0) 
                );

                randomAccessStream.Seek(0);
                updater.Thumbnail = RandomAccessStreamReference.CreateFromStream(randomAccessStream);
            }
            updater.Update();

            SetDiscordRPC();

            _mediaPlayer.Source = playbackItem;
            _mediaPlayer.Play();

            CurrentSongPlaying = song;
        }
        public void ScrubTo(int seconds)
        {
            _mediaPlayer.Position = new TimeSpan(0, 0, seconds);
        }

        private static readonly List<string> audioExtensions = [
            ".mp3",
            ".m4a",
            ".ogg",
            ".opus",
            ".wma"
        ];
        private static readonly List<string> losslessAudioExtensions = [
            ".flac",
            ".alac",
            ".wav",
            ".aiff",
            ".dsd"
        ];
        public static List<string> GetAllAudioExtensions()
        {
            List<string> allExtensions = [.. audioExtensions];
            allExtensions.AddRange(losslessAudioExtensions);
            return allExtensions;
        }
        private static readonly List<string> playlistExtensions = [
            ".xspf",
            //".m3u" // eventually
        ];
        public async Task<Song?> AddSongFromFile(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                throw new FileNotFoundException($"path '{path}' is invalid");
            }

            var extension = System.IO.Path.GetExtension(path).ToLower();
            if (!GetAllAudioExtensions().Contains(extension))
            {
                return null;
            }

            try
            {
                var tfile = TagLib.File.Create(path);

                if (!tfile.Properties.MediaTypes.Equals(MediaTypes.Audio))
                {
                    return null;
                }
                var artistName = !string.IsNullOrWhiteSpace(tfile.Tag.FirstAlbumArtist) ? tfile.Tag.FirstAlbumArtist?.Trim() : tfile.Tag.FirstPerformer?.Trim();
                var artist = GetArtistOrCreate(artistName ?? "Unknown Artist");
                var album = GetAlbumOrCreate(artist, tfile.Tag.Album);

                var song = ExtractSongData(tfile, new(path), album);

                return song;
            }
            catch (TagLib.CorruptFileException ex)
            {
                Console.WriteLine($"error with file {path}: {ex.Message}");
                return null;
            }
        }
        private Artist GetArtistOrCreate(string artistName)
        {
            var artist = GetArtist(artistName);
            if (artist == null)
            {
                artist = new Artist
                {
                    Name = artistName,
                    Albums = new ObservableCollection<Album>()
                };
                Database.Add(artist);
            }
            return artist;
        }

        private Album GetAlbumOrCreate(Artist artist, string albumName)
        {
            var album = artist.GetAlbum(albumName);
            if (album == null)
            {
                album = new Album
                {
                    Title = albumName,
                    Artist = artist,
                    Songs = new ObservableCollection<Song>()
                };
                artist.Albums.Add(album);
            }
            return album;
        }

        private Playlist ExtractPlaylistData(Uri path)
        {
            return null;
        }

        private Song ExtractSongData(TagLib.File tfile, Uri path, Album album)
        {
            var song = new Song
            {
                Title = tfile.Tag.Title,
                Album = album,
                ArtistName = tfile.Tag.FirstPerformer,
                FilePath = path.LocalPath,
                Genre = tfile.Tag.FirstGenre,
                Duration = tfile.Properties.Duration,
                TrackNumber = (int)tfile.Tag.Track,
                Lossless = IsLossless(path.LocalPath)
            };
            song.Favorited = song.RetrieveFavorited();
            var lyrics = tfile.Tag.Lyrics;
            if (!string.IsNullOrWhiteSpace(lyrics))
            {
                song.Lyrics = lyrics;
            } 
            else
            {
                var parent = Path.GetDirectoryName(path.LocalPath);
                var lrcPath = Path.Combine(parent, $"{Path.GetFileNameWithoutExtension(path.LocalPath)}.lrc");
                if (System.IO.File.Exists(lrcPath))
                {
                    song.Lyrics = System.IO.File.ReadAllText(lrcPath);
                }
            }

            album.Songs.Add(song);
            album.Songs.OrderBy(song => song.TrackNumber);

            var pic = tfile.Tag.Pictures.ElementAtOrDefault(0);
            if (pic != null)
            {
                album.CoverArtData = pic.Data.ToArray();
            } 
            else
            {
                var parent = Path.GetDirectoryName(path.LocalPath);
                var jpgPath = Path.Combine(parent, "cover.jpg");
                var pngPath = Path.Combine(parent, "cover.png");
                if (System.IO.File.Exists(pngPath))
                {
                    album.CoverArtData = System.IO.File.ReadAllBytes(pngPath);
                }
                else if (System.IO.File.Exists(jpgPath))
                {
                    album.CoverArtData = System.IO.File.ReadAllBytes(jpgPath);
                }
            }
            Task.Run(async () =>
            {
                if (album.CoverArtData is byte[] imageData)
                {
                    byte[] resizedImageData = await ResizeImageAsync(album.CoverArtData, 320);
                    album.CoverArtData = resizedImageData;
                }
            });
            return song;
        }
        private bool IsLossless(string path)
        {
            var extension = Path.GetExtension(path).ToLower();
            if (!audioExtensions.Contains(extension))
            {
                return false;
            }
            return losslessAudioExtensions.Contains(extension);
        }

        private bool _currentlyScanning = false;
        public async void SetLibrary(string targetDirectory)
        {
            if (_currentlyScanning) return;
            _currentlyScanning = true;

            Database.Clear();

            var playlistsToAdd = new List<Playlist>();

            await Task.Run(async () =>
            {
                await ScanDirectoryIntoLibrary(targetDirectory);

                foreach (string fileName in _playlistsToAdd)
                {
                    Playlist? playlist = null;
                    if (Path.GetExtension(fileName).ToLower() == ".xspf")
                        playlist = await Playlist.GetPlaylistFromXSPFFile(new Uri(fileName));
                    if (playlist != null) playlistsToAdd.Add(playlist);
                }
            });

            if (dispatcherQueue != null)
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    foreach (var pl in playlistsToAdd) Playlists.Add(pl);
                    _currentlyScanning = false;
                });
            }
            else
            {
                foreach (var pl in playlistsToAdd) Playlists.Add(pl);
                _currentlyScanning = false;
            }
        }
        private List<string> _playlistsToAdd = [];
        public async Task ScanDirectoryIntoLibrary(string targetDirectory)
        {
            if (!Directory.Exists(targetDirectory)) return;
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                if (playlistExtensions.Contains(Path.GetExtension(fileName))) _playlistsToAdd.Add(fileName); else await AddSongFromFile(fileName);

            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                await ScanDirectoryIntoLibrary(subdirectory);
        }

        public void PlayAlbum(Song startingSong)
        {
            PlaySongList([.. startingSong.Album.Songs], startingSong);
            if (CurrentShuffleState == ShuffleState.Shuffle) return;
            QueueManagerService.Instance.ReplaceQueueWithUnshuffledList(_fullCurrentSongList, CurrentSongPlaying);
        }
        public void PlayPlaylist(Song startingSong, Playlist playlist)
        {
            PlaySongList([.. playlist.Songs], startingSong);
            if (CurrentShuffleState == ShuffleState.Shuffle) return;
            QueueManagerService.Instance.ReplaceQueueWithUnshuffledList(_fullCurrentSongList, CurrentSongPlaying);
        }

        public async Task PlayTrackAsync(Song startingSong, bool favoritesOnly = false)
        {
            var tracks = await GetAllTracksAsync();
            if (favoritesOnly)
            {
                tracks = tracks.Where(song => song.Favorited).ToList();
            }
            PlaySongList(tracks, startingSong);
            if (CurrentShuffleState == ShuffleState.Shuffle) return;
            QueueManagerService.Instance.ReplaceQueueWithUnshuffledList(_fullCurrentSongList, CurrentSongPlaying);
        }

        // Image magic below
        public static async Task<byte[]> ResizeImageAsync(byte[] imageData, uint newWidth)
        {
            var inputStream = new InMemoryRandomAccessStream();
            await inputStream.WriteAsync(imageData.AsBuffer());
            inputStream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(inputStream);

            var transform = new BitmapTransform()
            {
                ScaledWidth = newWidth,
                ScaledHeight = (uint)((decoder.PixelHeight / (double)decoder.PixelWidth) * newWidth),
                InterpolationMode = BitmapInterpolationMode.Fant
            };

            var pixelData = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Rgba8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage
            );

            var outputStream = new InMemoryRandomAccessStream();

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outputStream);

            encoder.SetPixelData(
                BitmapPixelFormat.Rgba8,
                BitmapAlphaMode.Straight,
                transform.ScaledWidth,
                transform.ScaledHeight,
                decoder.DpiX,
                decoder.DpiY,
                pixelData.DetachPixelData()
            );

            await encoder.FlushAsync();

            var reader = new DataReader(outputStream.GetInputStreamAt(0));
            var bytes = new byte[outputStream.Size];
            await reader.LoadAsync((uint)outputStream.Size);
            reader.ReadBytes(bytes);

            return bytes;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (dispatcherQueue != null && !dispatcherQueue.HasThreadAccess)
            {
                dispatcherQueue.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
