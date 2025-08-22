using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YachtCRM.Domain
{
public class YachtModel
{
    [Key]
    public int ModelID { get; set; }


        [Required, MaxLength(120)]
        public string ModelName { get; set; } = default!;

        [MaxLength(60)]
        public string YachtType { get; set; } = "Motor Yacht";

        [Column(TypeName = "decimal(7,2)")]
        public decimal Length { get; set; }
        [Column(TypeName = "decimal(7,2)")]
        public decimal Beam { get; set; }
        [Column(TypeName = "decimal(7,2)")]
        public decimal Draft { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        public string? Description { get; set; }

        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
