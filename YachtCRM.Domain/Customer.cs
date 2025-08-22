using System.ComponentModel.DataAnnotations;

namespace YachtCRM.Domain
{
    public class Customer
    {
        public int CustomerID { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = default!;

        [EmailAddress] public string? Email { get; set; }
        [Phone] public string? Phone { get; set; }
        public string? Country { get; set; }
        public string? Notes { get; set; }

        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<CrmTask> Tasks { get; set; } = new List<CrmTask>();
        public ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
        public ICollection<ServiceTask> ServiceTasks { get; set; } = new List<ServiceTask>();
        public ICollection<CustomerBroker> CustomerBrokers { get; set; } = new List<CustomerBroker>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
