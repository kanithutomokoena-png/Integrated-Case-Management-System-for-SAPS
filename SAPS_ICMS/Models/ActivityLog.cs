using System.ComponentModel.DataAnnotations;

namespace SAPS_ICMS.Models
{
    public class ActivityLog
    {
        [Key]
        public int LogId { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; }

        [MaxLength(100)]
        public string? EntityType { get; set; } // Case, Evidence, User, etc.

        public int? EntityId { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [MaxLength(50)]
        public string? Location { get; set; }
    }

    public static class ActivityActions
    {
        // Case actions
        public const string CaseCreated = "Case Created";
        public const string CaseUpdated = "Case Updated";
        public const string CaseViewed = "Case Viewed";
        public const string CaseClosed = "Case Closed";
        public const string CaseDeleted = "Case Deleted";

        // Evidence actions
        public const string EvidenceUploaded = "Evidence Uploaded";
        public const string EvidenceDownloaded = "Evidence Downloaded";
        public const string EvidenceDeleted = "Evidence Deleted";

        // User actions
        public const string UserLogin = "User Login";
        public const string UserLogout = "User Logout";
        public const string UserCreated = "User Created";
        public const string UserUpdated = "User Updated";

        // Archive actions
        public const string ArchiveAccessed = "Archive Accessed";
        public const string ArchivePasswordVerified = "Archive Password Verified";
        public const string ArchiveSearched = "Archive Searched";

        // Edit request actions
        public const string EditRequested = "Edit Requested";
        public const string EditApproved = "Edit Approved";
        public const string EditRejected = "Edit Rejected";
        public const string EditCompleted = "Edit Completed";

        // Closure request actions
        public const string ClosureRequested = "Closure Requested";
        public const string ClosureApproved = "Closure Approved";
        public const string ClosureRejected = "Closure Rejected";
    }
}
