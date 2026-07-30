using System.ComponentModel.DataAnnotations;

namespace SAPS_ICMS.Models
{
    public class Evidence
    {
        [Key]
        public int EvidenceId { get; set; }
        public int CaseId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string ContentType { get; set; } = string.Empty;

        public byte[] FileData {  get; set; } = Array.Empty<byte>();
        public DateTime UploadDate { get; set; } = DateTime.Now;

        public string UploadedBy { get; set; }
        public Case? Case { get; set; }
    }
}
