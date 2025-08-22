using System.ComponentModel.DataAnnotations;

namespace YachtCRM.Domain
{
    public class ProjectMilestone
    {
        public int ProjectMilestoneID { get; set; }
        public int ProjectID { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = default!;

        public DateTime PlannedDate { get; set; }
        public DateTime? ActualDate { get; set; }
        public bool IsComplete { get; set; }

        public Project Project { get; set; } = default!;
    }
}
