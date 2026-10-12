// G4.3: Copy nguyên xi từ Players/FFMPEG/BoundedConcurrentQueue.cs (WinForms).
// Không có System.Drawing reference — không cần thay đổi gì.
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Kztek.Cameras.Players.FFMPEG
{
    public class BoundedConcurrentQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly int _capacity;
        private readonly object _lock = new object();

        public BoundedConcurrentQueue(int capacity)
        {
            _capacity = capacity;
        }

        public void Enqueue(T item, Action<T>? onDrop = null)
        {
            //lock (_lock)
            {
                if (_queue.Count >= _capacity)
                {
                    _queue.TryDequeue(out T removed);
                    if (removed != null && onDrop != null)
                        onDrop?.Invoke(removed);
                }
                _queue.Enqueue(item);
            }
        }

        public bool TryDequeue(out T item)
        {
            //lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    _queue.TryDequeue(out item);
                    return item != null;
                }

                item = default!;
                return false;
            }
        }

        public T[] ToArray(int max = 100)
        {
            var arr = _queue.ToArray(); // snapshot an toàn

            if (arr.Length <= max)
                return arr;

            return arr.TakeLast(max).ToArray(); // lấy N phần tử mới nhất
        }

        public int Count
        {
            get
            {
                lock (_lock)
                { return _queue.Count; }
            }
        }
        public void Clear()
        {
            lock (_lock)
            {
                while (_queue.TryDequeue(out _)) ;
            }
        }
    }
}
