using Microsoft.AspNet.Identity;

namespace AspNetUserMaintenanceAzureSiteExtension.AspNetAuth
{
    /// <summary>
    /// Class created for parity with default visual studio asp.net project template.
    /// </summary>
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
            // no-op
        }
    }
}