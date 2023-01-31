namespace SkripsiAppBackend.Services.ObjectCachingService
{
    public interface IObjectCachingService<TObject>
    {
        public void Set(string key, TObject data);
        public bool Exists(string key);
        public TObject Get(string key);
        public void Delete(string key);
        public async Task<TObject> GetCache(string key, Func<Task<TObject>> getData)
        {
            if (!Exists(key))
            {
                Set(key, await getData());
            }

            return Get(key);
        }
    }
}
