using System;

namespace YachtCRM.Domain
{
    public class ServiceRequest
    {
        public int ServiceRequestID { get; set; }

        // Foreign keys
        public int ProjectID { get; set; }
        public Project? Project { get; set; }

        // Core fields
        public string Title { get; set; } = "";
        public string Type { get; set; } = "Maintenance";
        public string Status { get; set; } = "Open";

        // Dates
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedOn { get; set; }   // nullable so we can track open requests

        // Optional notes
        public string? Description { get; set; }
    }
}




