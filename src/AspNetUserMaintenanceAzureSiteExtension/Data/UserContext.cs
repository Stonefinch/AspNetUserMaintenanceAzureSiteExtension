using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace AspNetUserMaintenanceAzureSiteExtension.Data
{
    public class UserContext : DbContext
    {
        public UserContext(string connectionString)
            : base(connectionString)
        {
            // Do not attempt to create database
            Database.SetInitializer<UserContext>(null);
        }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // table names are not plural in DB, remove the convention
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            
            base.OnModelCreating(modelBuilder);
        }
    }
}