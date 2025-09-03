namespace YachtCRM.Domain
{
    public class CustomerFeedback
    {
        public int CustomerFeedbackID { get; set; }
        public int CustomerID { get; set; }
        public int? ProjectID { get; set; }  // optional, tie to a delivery

        // 0..10 (NPS-style)
        public int Score { get; set; }
        public string? Comments { get; set; }
        public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;

        public Customer Customer { get; set; } = null!;
        public Project? Project { get; set; }
    }
}
