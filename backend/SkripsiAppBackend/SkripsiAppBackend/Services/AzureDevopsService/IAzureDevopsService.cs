namespace SkripsiAppBackend.Services.AzureDevopsService
{
    public interface IAzureDevopsService
    {
        public struct Profile
        {
            public Guid sessionId { get; set; }
            public string DisplayName { get; set; }
            public string PublicAlias { get; set; }
            public string ProfileId { get; set; }
            
            public static Profile Empty
            {
                get
                {
                    return new Profile()
                    {
                        sessionId = Guid.Empty,
                    };
                }
            }

            public bool Equals(Profile profile)
            {
                return sessionId == profile.sessionId;
            }
        }

        public struct Organization
        {
            public string Name { get; set; }
        }

        public struct Project
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public struct Team
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public enum SprintTimeFrame
        {
            Past,
            Current,
            Future,
            Unknown
        }

        public struct Sprint
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public SprintTimeFrame TimeFrame { get; set; }
        }

        public enum WorkItemState
        {
            New,
            Done,
            Unknown
        }

        public struct WorkItem
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public WorkItemState State { get; set; }
            public double Priority { get; set; }
            public double Effort { get; set; }
            public double BusinessValue { get; set; }
        }

        public bool HasActiveProfile { get; }

        public Profile ReadSelfProfile();
        public Task<List<Organization>> ReadAllOrganizations();
        public Task<List<Project>> ReadProjectsByOrganization(string organizationName);
        public Task<Project> ReadProject(string organizationName, string projectId);
        public Task<List<Team>> ReadTeamsByProject(string organizationName, string projectId);
        public Task<Team> ReadTeam(string organizationName, string projectId, string teamId);
        public Task<List<Sprint>> ReadTeamSprints(string organizationName, string projectId, string teamId);
        public Task<List<WorkItem>> ReadSprintWorkItems(string organizationName, string projectId, string teamId, string sprintId);
        public Task<List<WorkItem>> ReadBacklogWorkItems(string organizationName, string projectId, string teamId);
        public Task<List<DayOfWeek>> ReadTeamWorkDays(string organizationName, string projectId, string teamId);
    }
}
