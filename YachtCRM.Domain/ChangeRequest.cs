namespace YachtCRM.Domain
{
    public class ChangeRequest
    {
        public int ChangeRequestID { get; set; }
        public int ProjectID { get; set; }
        public DateTime RequestedOn { get; set; } = DateTime.UtcNow;

        public string Title { get; set; } = default!;
        public string? Description { get; set; }

        public decimal? CostImpact { get; set; }
        public int? DaysImpact { get; set; }
        public bool Approved { get; set; }
        public DateTime? ApprovedOn { get; set; }

        public Project Project { get; set; } = default!;
    }
}
