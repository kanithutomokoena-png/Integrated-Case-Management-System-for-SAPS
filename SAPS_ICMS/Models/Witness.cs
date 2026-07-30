using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAPS_ICMS.Models
{
    public class Witness
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WitnessId { get; set; }
        public int CaseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Statement { get; set; }
        public string? PhoneNumber { get; set; }
        
        public virtual Case? Case { get; set; }
    }
}
