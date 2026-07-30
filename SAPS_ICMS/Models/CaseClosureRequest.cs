using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAPS_ICMS.Models
{
    public class CaseClosureRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int CaseId { get; set; }

        [ForeignKey("CaseId")]
        public virtual Case? Case { get; set; }

        [MaxLength(450)] 
        public string RequestedByUserId { get; set; }

        [MaxLength(100)]
        public string? RequestedByUserName { get; set; }

        [Required]
        public DateTime RequestedDate { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [Required]
        public RequestStatus Status { get; set; }

        public DateTime? ReviewedDate { get; set; }

        [MaxLength(450)]
        public string? ReviewedByUserId { get; set; }

        [MaxLength(100)]
        public string? ReviewedByUserName { get; set; }

    }

    public enum RequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}

