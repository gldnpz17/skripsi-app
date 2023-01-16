namespace SkripsiAppBackend.Services
{
    public interface IKeyValueService
    {
        public void Set(string key, string value);
        public string Get(string key);
        public void Delete(string key);
    }
}
