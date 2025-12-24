using Musium.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Musium.Services
{
    internal class QueueManagerService : ObservableObject
    {
        private static readonly Lazy<QueueManagerService> _instance = new Lazy<QueueManagerService>(() => new QueueManagerService());
        public static QueueManagerService Instance => _instance.Value;
        private QueueManagerService() { }

        public ObservableCollection<Song> Queue = [];
        public List<Song> History = [];

        public void ReplaceQueueWithList(List<Song> list)
        {
            Queue.Clear();
            foreach (Song song in list)
            {
                Queue.Add(song);
            }
        }
        public void ReplaceQueueWithUnshuffledList(List<Song> list, Song song)
        {
            int index = list.FindIndex(s => s == song);

            if (index == -1) return;

            int startIndex = index + 1;
            if (startIndex <= list.Count)
            {
                int count = list.Count - startIndex;
                ReplaceQueueWithList(list.GetRange(startIndex, count));
                if (song != null) Queue.Remove(song);
            }
        }

        public void InsertStartOfQueue(Song song)
        {
            Queue.Insert(0, song);
        }
        public void InsertEndOfQueue(Song song)
        {
            Queue.Add(song);
        }
        public void RemoveFromQueue(Song song)
        {
            Queue.Remove(song);
        }
        public void RemoveFirstInQueue()
        {
            if (Queue.Count > 0)
            {
                Queue.RemoveAt(0);
            }
        }
        public Song? FirstInQueue()
        {
            return Queue.FirstOrDefault<Song>();
        }
        public Song? LastInQueue()
        {
            return Queue.LastOrDefault<Song>();
        }

        public void InsertStartOfHistory(Song song)
        {
            History.Insert(0, song);
        }
        public void InsertEndOfHistory(Song song)
        {
            History.Add(song);
        }
        public void RemoveFromHistory(Song song)
        {
            History.Remove(song);
        }
        public void RemoveFirstInHistory()
        {
            if (History.Count > 0)
            {
                History.RemoveAt(0);
            }
        }
        public Song? FirstInHistory()
        {
            return History.FirstOrDefault<Song>();
        }
        public Song? LastInHistory()
        {
            return History.LastOrDefault<Song>();
        }
    }
}
