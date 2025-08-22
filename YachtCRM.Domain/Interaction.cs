using System.ComponentModel.DataAnnotations;

namespace YachtCRM.Domain
{
    public class Interaction
    {
        public int InteractionID { get; set; }
        public int CustomerID { get; set; }
        public int? ProjectID { get; set; }

        [MaxLength(40)]
        public string Type { get; set; } = "Meeting"; // Call/Email/Meeting

        public string? Notes { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

        public Customer Customer { get; set; } = default!;
        public Project? Project { get; set; }
    }
}
