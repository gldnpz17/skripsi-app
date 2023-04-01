using System.Collections.Concurrent;

namespace SkripsiAppBackend.Services.ObjectCachingService
{
    public class InMemoryObjectCachingService<TObject> : IObjectCachingService<TObject>
    {
        private readonly TimeSpan lifespan;
        private readonly ConcurrentDictionary<string, TObject> keyData = new();
        private readonly ConcurrentDictionary<string, DateTime> expiryTime = new();

        public InMemoryObjectCachingService(TimeSpan lifespan)
        {
            this.lifespan = lifespan;
        }

        public void Set(string key, TObject data)
        {
            keyData[key] = data;
            expiryTime[key] = DateTime.Now + lifespan;
        }

        public bool Exists(string key)
        {
            return keyData.ContainsKey(key);
        }

        public TObject Get(string key)
        {
            return keyData[key];
        }

        public void Delete(string key)
        {
            keyData.Remove(key, out _);
            expiryTime.Remove(key, out _);
        }

        public async Task<TObject> GetCache(string key, Func<Task<TObject>> getData)
        {
            if (!Exists(key))
            {
                await RefreshData();
            }

            if (expiryTime.ContainsKey(key) && DateTime.Now > expiryTime[key])
            {
                _ = RefreshData();
            }

            return Get(key);

            async Task RefreshData()
            {
                var data = await getData();
                Set(key, data);
            }
        }
    }

    static class InMemoryObjectCachingServiceExtensions
    {
        public static void AddInMemoryObjectCaching(this IServiceCollection services, List<Type> types)
        {
            foreach (var type in types)
            {
                var serviceType = typeof(IObjectCachingService<>).MakeGenericType(type);

                services.AddSingleton(serviceType, (service) => GetCachingService(type));
            }

            object GetCachingService(Type type)
            {
                var cachingServiceConstructor = typeof(InMemoryObjectCachingService<>)
                    .MakeGenericType(type)
                    .GetConstructor(new Type[] { typeof(TimeSpan) });

                if (cachingServiceConstructor == null)
                {
                    throw new Exception("Can't find suitable InMemoryObjectCachingService constructor.");
                }

                var cachingService = cachingServiceConstructor.Invoke(new object[] { TimeSpan.FromSeconds(5) });

                return cachingService;
            }
        }

        public static void AddInMemoryObjectCaching(this IServiceCollection services, params Type[] types)
        {
            AddInMemoryObjectCaching(services, types.ToList());
        }
    }
}
