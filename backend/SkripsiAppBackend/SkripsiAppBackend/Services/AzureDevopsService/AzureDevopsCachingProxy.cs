using Microsoft.Extensions.ObjectPool;
using SkripsiAppBackend.Services.ObjectCachingService;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SkripsiAppBackend.Services.AzureDevopsService
{
    public struct Key
    {
        public List<object> Items { get; }
        public Key(params object[] items)
        {
            Items = items.ToList();
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

        public int HashCode { 
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

        public InMemoryUniversalCachingService(TimeSpan lifespan)
        {
            int numProcs = Environment.ProcessorCount;
            int concurrencyLevel = numProcs * 2;
            int initialCapacity = 101;

            values = new ConcurrentDictionary<Key, object>(concurrencyLevel, numProcs);
            expiryTimes = new ConcurrentDictionary<Key, DateTime>(concurrencyLevel, numProcs);
            this.lifespan = lifespan;
        }
        public async Task<TObject> UseCache<TObject>(Key key, Func<Task<TObject>> getData, List<Key>? invalidateKeys = null)
        {
            if (!values.ContainsKey(key))
            {
                await refreshData();
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
                var newValue = await getData();
                values.TryGetValue(key, out var oldValue);

                string strA = JsonSerializer.Serialize(newValue);
                string strB = JsonSerializer.Serialize(oldValue);
                var isEqual = strA == strB;

                values[key] = newValue;
                expiryTimes[key] = DateTime.Now + lifespan;

                if (oldValue != null && newValue != null && !isEqual && invalidateKeys != null)
                {
                    // Perhaps we should use some kind of indexing system. But this should do for now.
                    // TODO: Seriously, we should fix this. This is an absolute dogshit implementation. What a disgrace.
                    foreach (var entry in values)
                    {
                        if (invalidateKeys.Any(key => entry.Key.Contains(key)))
                        {
                            values.Remove(key, out _);
                            expiryTimes.Remove(key, out _);
                        }
                    }
                }
            }
        }
    }

    public class AzureDevopsCachingProxy : IAzureDevopsService
    {
        private readonly IAzureDevopsService service;
        private readonly InMemoryUniversalCachingService cache;

        public AzureDevopsCachingProxy(
            IAzureDevopsService service,
            InMemoryUniversalCachingService cache)
        {
            this.service = service;
            this.cache = cache;
        }

        public bool HasActiveProfile => service.HasActiveProfile;

        public Task<List<IAzureDevopsService.Organization>> ReadAllOrganizations()
        {
            return service.ReadAllOrganizations();
        }

        public Task<List<IAzureDevopsService.WorkItem>> ReadBacklogWorkItems(string organizationName, string projectId, string teamId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, teamId, "workItems", "backlog"),
                () => service.ReadBacklogWorkItems(organizationName, projectId, teamId),
                new List<Key> { new Key(organizationName, projectId, teamId, "workItems") }
            );
        }

        public Task<IAzureDevopsService.Project> ReadProject(string organizationName, string projectId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId),
                () => service.ReadProject(organizationName, projectId)
            );
        }

        public Task<List<IAzureDevopsService.Project>> ReadProjectsByOrganization(string organizationName)
        {
            return cache.UseCache(
                new Key(organizationName, "projects"),
                () => service.ReadProjectsByOrganization(organizationName)
            );
        }

        public IAzureDevopsService.Profile ReadSelfProfile()
        {
            return service.ReadSelfProfile();
        }

        public Task<List<IAzureDevopsService.WorkItem>> ReadSprintWorkItems(string organizationName, string projectId, string teamId, string sprintId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, teamId, "workItems", "sprints", sprintId),
                () => service.ReadSprintWorkItems(organizationName, projectId, teamId, sprintId),
                new List<Key> { new Key(organizationName, projectId, teamId, "workItems") }
            );
        }

        public Task<IAzureDevopsService.Team> ReadTeam(string organizationName, string projectId, string teamId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, teamId),
                () => service.ReadTeam(organizationName, projectId, teamId)
            );
        }

        public Task<List<IAzureDevopsService.Team>> ReadTeamsByProject(string organizationName, string projectId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, "teams"),
                () => service.ReadTeamsByProject(organizationName, projectId)
            );
        }

        public Task<List<IAzureDevopsService.Sprint>> ReadTeamSprints(string organizationName, string projectId, string teamId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, teamId, "sprints"),
                () => service.ReadTeamSprints(organizationName, projectId, teamId)
            );
        }

        public Task<List<DayOfWeek>> ReadTeamWorkDays(string organizationName, string projectId, string teamId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, teamId, "workDays"),
                () => service.ReadTeamWorkDays(organizationName, projectId, teamId)
            );
        }
    }
}
