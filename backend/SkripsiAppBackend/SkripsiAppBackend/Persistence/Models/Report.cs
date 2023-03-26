namespace SkripsiAppBackend.Persistence.Models
{
    public class Report
    {
        public int Id { get; set; }
        public bool Deleted { get; set; }
        public string OrganizationName { get; set; }
        public string ProjectId { get; set; }
        public string TeamId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public long Expenditure { get; set; }
    }
}
