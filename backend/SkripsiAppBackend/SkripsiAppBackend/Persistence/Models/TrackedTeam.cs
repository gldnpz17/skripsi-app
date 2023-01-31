using System.ComponentModel.DataAnnotations.Schema;

namespace SkripsiAppBackend.Persistence.Models
{
    public class TrackedTeam
    {
        public string TeamId { get; set; }
        public string ProjectId { get; set; }
        public string OrganizationName { get; set; }
        public bool Deleted { get; set; }
    }
}
