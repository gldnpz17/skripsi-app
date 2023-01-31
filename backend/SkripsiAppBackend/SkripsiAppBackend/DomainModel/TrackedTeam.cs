namespace SkripsiAppBackend.DomainModel
{
    public class TrackedTeam
    {
        public string TeamId { get; set; }
        public string ProjectId { get; set; }
        public string OrganizationName { get; set; }
        public bool IsUntracked { get; set; } = false;
    }
}
