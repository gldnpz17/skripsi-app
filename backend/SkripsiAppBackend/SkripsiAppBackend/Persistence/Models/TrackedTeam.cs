using System.ComponentModel.DataAnnotations.Schema;

namespace SkripsiAppBackend.Persistence.Models
{
    public class TrackedTeam
    {
        public string TeamId { get; set; }
        public string ProjectId { get; set; }
        public string OrganizationName { get; set; }
        public DateTime? Deadline { get; set; }
        public int? CostPerEffort { get; set; }
        public bool Deleted { get; set; }
        public string EacFormula { get; set; }
        public string EtcFormula { get; set; }
    }
}
