using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAPS_ICMS.Models
{
    public enum IncidentRole
    {
        Victim,
        Witness,
        Suspect,
        Other
    }
    public enum CaseStatus
    {
        Investigation = 0,
        UnderReview = 1,
        Closed = 2
    }
    public class Case
    {
        public int CaseId { get; set; }
        [MaxLength(50)]
        public string? CaseNumber { get; set; }
        public DateOnly Date { get; set; }
        public string? LocationTaken { get; set; }
        public string? OfficerTakingStatement { get; set; } = string.Empty;
        public string? Complainant { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? HomeAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Occupation { get; set; }
        public IncidentRole RelationshipToIncident { get; set; }
        public string? CaseInformation  { get; set; }
        public string? ComplainantSignature { get; set; }
        public DateOnly? ComplainantSignatureDate {  get; set; }
        public TimeSpan? ComplainantSignatureTime { get; set; }
        public string? OfficerSignature { get; set; }
        public DateOnly? OfficerSignatureDate { get; set; }
        public CaseStatus Status { get; set; } = CaseStatus.Investigation;
        [NotMapped]
        public List<IFormFile>? EvidenceFiles { get; set; }
        [NotMapped]
        public virtual List<Witness>? Witnesses { get; set; } = new List<Witness>();
        public virtual List<Evidence>? Evidences { get; set; } = new List<Evidence>();
    }
}
