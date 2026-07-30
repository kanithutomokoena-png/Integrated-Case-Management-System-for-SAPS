using Microsoft.AspNetCore.Authorization;

namespace SAPS_ICMS.Filters
{
    public class CustomAuthorizeAttribute: AuthorizeAttribute
    {
        public CustomAuthorizeAttribute(params string[] roles) {
        Roles = string.Join(",", roles);
        }
    }
}
