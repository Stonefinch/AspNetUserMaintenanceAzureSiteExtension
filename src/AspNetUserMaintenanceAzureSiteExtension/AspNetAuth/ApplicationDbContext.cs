using Microsoft.AspNet.Identity.EntityFramework;

namespace AspNetUserMaintenanceAzureSiteExtension.AspNetAuth
{
    /// <summary>
    /// Class created for parity with default visual studio asp.net project template.
    /// minor difference in how the connection string is defined.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(string connectionString)
            : base(connectionString, throwIfV1Schema: false)
        {
            // no-op
        }

        public static ApplicationDbContext Create(string connectionString)
        {
            return new ApplicationDbContext(connectionString);
        }
    }
}