using Microsoft.AspNetCore.Identity;

namespace Webshop.Models
{
    public class SiteUser : IdentityUser<int>
    {
        public SiteUser()
        {

        }
        public string Name { get; set; }
    }
}
