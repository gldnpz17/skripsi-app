using System.Collections.Concurrent;
using System.Text.Json;

namespace SkripsiAppBackend.Services.UniversalCachingService
{
    public struct Key
    {
        public List<object> Items { get; }
        public Key(params object[] items)
        {
            Items = items.ToList();
        }

        public override string ToString()
        {
            return String.Join(":", Items);
        }

        public bool Contains(Key shorterKey)
        {
            for (int i = 0; i < shorterKey.Items.Count; i++)
            {
                if (Items[i] != shorterKey.Items[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Key) return false;
            var otherKey = (Key)obj;

            return otherKey.GetHashCode() == this.GetHashCode();
        }

        public static bool operator ==(Key left, Key right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Key left, Key right)
        {
            return !(left == right);
        }

        public int HashCode
        {
            get
            {
                return GetHashCode();
            }
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            foreach (var item in Items)
            {
                hashCode.Add(item);
            }
            return hashCode.ToHashCode();
        }
    }

    public class InMemoryUniversalCachingService
    {
        private readonly TimeSpan lifespan;
        private readonly ConcurrentDictionary<Key, object> values;
        private readonly ConcurrentDictionary<Key, DateTime> expiryTimes;
        private readonly ConcurrentDictionary<Key, bool> fetchLock;

        public InMemoryUniversalCachingService(TimeSpan lifespan)
        {
            int numProcs = Environment.ProcessorCount;
            int concurrencyLevel = numProcs * 2;

            values = new ConcurrentDictionary<Key, object>(concurrencyLevel, numProcs);
            expiryTimes = new ConcurrentDictionary<Key, DateTime>(concurrencyLevel, numProcs);
            fetchLock = new ConcurrentDictionary<Key, bool>(concurrencyLevel, numProcs);
            this.lifespan = lifespan;
        }

        public async Task<TObject> UseCache<TObject>(Key key, Func<Task<TObject>> getData, List<Key>? invalidateKeys = null)
        {
            if (!values.ContainsKey(key) && !fetchLock.ContainsKey(key))
            {
                await refreshData();
            }

            if (fetchLock.ContainsKey(key))
            {
                // This is stupid. But it'll do.
                while (true)
                {
                    await Task.Delay(10);
                    if (!fetchLock.ContainsKey(key))
                    {
                        break;
                    }
                }
            }

            var expirationExists = expiryTimes.TryGetValue(key, out var expiration);

            if (expirationExists && DateTime.Now > expiration)
            {
                _ = refreshData();
            }

            values.TryGetValue(key, out var value);
            return (TObject)value;

            bool ObjectsAreEqual(object a, object b)
            {
                string strA = JsonSerializer.Serialize(a);
                string strB = JsonSerializer.Serialize(b);
                return a == b;
            }

            async Task refreshData()
            {
                Console.WriteLine($"Fetching data for key {key}");
                fetchLock.TryAdd(key, true);
                var newValue = await getData();
                values.TryGetValue(key, out var oldValue);

                values[key] = newValue;
                expiryTimes[key] = DateTime.Now + lifespan;

                fetchLock.TryRemove(key, out _);

                if (oldValue != null && newValue != null && !ObjectsAreEqual(newValue, oldValue) && invalidateKeys != null)
                {
                    invalidateKeys.ForEach(key => Invalidate(key));
                }
            }
        }

        public void Invalidate(Key key)
        {
            // Perhaps we should use some kind of indexing system. But this should do for now.
            // TODO: Seriously, we should fix this. This is an absolute dogshit implementation. What a disgrace.
            foreach (var entry in values)
            {
                if (entry.Key.Contains(key))
                {
                    values.Remove(key, out _);
                    expiryTimes.Remove(key, out _);
                }
            }
        }
    }
}
