using Microsoft.AspNetCore.Identity;
namespace SAPS_ICMS.Models
{
    public class Officer: IdentityUser
    {
        public string SecurityNumber { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Surname { get; set; } = default!;
        
    }
}
