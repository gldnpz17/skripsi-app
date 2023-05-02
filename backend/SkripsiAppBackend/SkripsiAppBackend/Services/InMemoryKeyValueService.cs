using System.Collections.Concurrent;

namespace SkripsiAppBackend.Services
{
    public class InMemoryKeyValueService : IKeyValueService
    {
        private readonly ConcurrentDictionary<string, string> keyValuePairs = new();

        public void Delete(string key)
        {
            keyValuePairs.Remove(key, out _);
        }

        public string Get(string key)
        {
            return keyValuePairs[key];
        }

        public void Set(string key, string value)
        {
            keyValuePairs[key] = value;
        }
    }
}
