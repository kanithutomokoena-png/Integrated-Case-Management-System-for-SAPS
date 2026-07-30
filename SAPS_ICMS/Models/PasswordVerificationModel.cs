using System.ComponentModel.DataAnnotations;

namespace SAPS_ICMS.Models
{
    public class PasswordVerificationModel
    {
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
