using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YachtCRM.Domain
{
    public class Project
    {
        public int ProjectID { get; set; }
        public int CustomerID { get; set; }
        public int YachtModelID { get; set; }

        [Required, MaxLength(160)]
        public string Name { get; set; } = default!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public DateTime PlannedStart { get; set; }
        public DateTime PlannedEnd { get; set; }
        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }

        [MaxLength(40)]
        public string Status { get; set; } = "Planning";

        public Customer Customer { get; set; } = default!;
        public YachtModel YachtModel { get; set; } = default!;
        public int? YardID { get; set; }       // nullable for smooth migration
        public Yard? Yard { get; set; }
        public ICollection<CrmTask> Tasks { get; set; } = new List<CrmTask>();
        public ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
        public ICollection<ProjectMilestone> Milestones { get; set; } = new List<ProjectMilestone>();
        public ICollection<ChangeRequest> ChangeRequests { get; set; } = new List<ChangeRequest>();
        public ICollection<ServiceTask> ServiceTasks { get; set; } = new List<ServiceTask>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    }
}
