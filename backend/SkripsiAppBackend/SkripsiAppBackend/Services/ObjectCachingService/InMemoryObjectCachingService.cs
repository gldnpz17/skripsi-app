namespace SkripsiAppBackend.Services.ObjectCachingService
{
    public class InMemoryObjectCachingService<TObject> : IObjectCachingService<TObject>
    {
        private readonly TimeSpan lifespan;
        private readonly Dictionary<string, TObject> keyData = new();
        private readonly Dictionary<string, Task> keyExpiryTasks = new();

        public InMemoryObjectCachingService(TimeSpan lifespan)
        {
            this.lifespan = lifespan;
        }

        public void Set(string key, TObject data)
        {
            keyData[key] = data;

            if (!keyExpiryTasks.ContainsKey(key))
            {
                keyExpiryTasks[key] = Task.Run(async () =>
                {
                    await Task.Delay(lifespan);
                    keyExpiryTasks.Remove(key);
                    Delete(key);
                });
            }
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
            keyData.Remove(key);
        }
    }

    static class InMemoryObjectCachingServiceExtensions
    {
        public static void AddInMemoryObjectCaching(this IServiceCollection services, List<Type> types)
        {
            foreach (var type in types)
            {
                var cachingServiceConstructor = typeof(InMemoryObjectCachingService<>)
                    .MakeGenericType(type)
                    .GetConstructor(new Type[] { typeof(TimeSpan) });

                if (cachingServiceConstructor == null)
                {
                    throw new Exception("Can't find suitable InMemoryObjectCachingService constructor.");
                }
                var cachingService = cachingServiceConstructor.Invoke(new object[] { TimeSpan.FromSeconds(5) });

                var serviceType = typeof(IObjectCachingService<>)
                    .MakeGenericType(type);

                services.AddSingleton(serviceType, cachingService);
            }
        }

        public static void AddInMemoryObjectCaching(this IServiceCollection services, params Type[] types)
        {
            AddInMemoryObjectCaching(services, types.ToList());
        }
    }
}
