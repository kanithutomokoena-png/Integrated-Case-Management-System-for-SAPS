using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAPS_ICMS.Models
{
    public class CaseEditRequest
    {
            [Key]
            public int EditRequestId { get; set; }

            [Required]
            public int CaseId { get; set; }

            [ForeignKey("CaseId")]
            public virtual Case? Case { get; set; }

            [Required]
            [MaxLength(450)]
            public string RequestedByUserId { get; set; }

            [MaxLength(100)]
            public string? RequestedByUserName { get; set; }

            [Required]
            public DateTime RequestedDate { get; set; }

            [Required]
            [MaxLength(1000)]
            public string RequestedChanges { get; set; }

            [MaxLength(1000)]
            public string? Justification { get; set; }

            [Required]
            public EditRequestStatus Status { get; set; }

            public DateTime? ReviewedDate { get; set; }

            [MaxLength(450)]
            public string? ReviewedByUserId { get; set; }

            [MaxLength(100)]
            public string? ReviewedByUserName { get; set; }

            [MaxLength(500)]
            public string? AdminNotes { get; set; }
            public bool EditCompleted { get; set; } = false;

            public DateTime? EditCompletedDate { get; set; }
    }

        public enum EditRequestStatus
        {
            Pending = 0,
            Approved = 1,
            Rejected = 2
        }
}
