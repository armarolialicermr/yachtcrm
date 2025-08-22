namespace YachtCRM.Domain
{
    public class ServiceTask
    {
        public int ServiceTaskID { get; set; }
        public int CustomerID { get; set; }
        public int? ProjectID { get; set; }

        public string Category { get; set; } = "Maintenance";
        public string Title { get; set; } = default!;
        public string? Notes { get; set; }

        public DateTime OpenedOn { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedOn { get; set; }

        public decimal? Cost { get; set; }

        public Customer Customer { get; set; } = default!;
        public Project? Project { get; set; }
    }
}
