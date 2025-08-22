using System.ComponentModel.DataAnnotations;

namespace YachtCRM.Domain
{
    public class CrmTask
    {
        public int CrmTaskID { get; set; }
        public int? CustomerID { get; set; }
        public int? ProjectID { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);
        public bool IsCompleted { get; set; }

        public Customer? Customer { get; set; }
        public Project? Project { get; set; }
    }
}
