using AspNetUserMaintenanceAzureSiteExtension.Models;
using System.Collections.Generic;
using System.Data.SqlClient;
s
namespace AspNetUserMaintenanceAzureSiteExtension.Services
{
    public class UserRepository
    {
        private IAzureConfiguration AzureConfiguration { get; set; }

        public UserRepository(IAzureConfiguration azureConfiguration)
        {
            this.AzureConfiguration = azureConfiguration;
        }

        public IEnumerable<AspNetUserViewModel> ListAspNetUsers(AspNetUserSearchCriteria searchCriteria)
        {
            var result = new List<AspNetUserViewModel>();

            using (var connection = new SqlConnection(this.AzureConfiguration.GetConnectionString("AspNetUserMaintenanceAzureSiteExtensionSqlConnectionString")))
            {
                var command = new SqlCommand(
@"
select top 100
     u.UserName
    ,u.Email
    ,r.Name Role

from
    dbo.AspNetUsers u
    left join dbo.AspNetUserRoles ur on (u.Id = ur.UserId)
    left join dbo.AspNetRoles r on (ur.RoleId = r.Id)

where
    (@UserName = '' OR u.UserName like '%@UserName%')
    and (@Email = '' OR u.Email like '%@Email%')
    and (@Role = '' or r.Name like '%@Role%')

order by 1
", connection);
                
                command.Parameters.AddWithValue("UserName", searchCriteria.UserName ?? "");
                command.Parameters.AddWithValue("Email", searchCriteria.Email ?? "");
                command.Parameters.AddWithValue("Role", searchCriteria.Role ?? "");

                connection.Open();
                var rdr = command.ExecuteReader();
                
                while(rdr.NextResult())
                {
                    var u = new AspNetUserViewModel();
                    u.UserName = (string)rdr["UserName"];
                    u.Email = (string)rdr["Email"];
                    u.Role = (string)rdr["Role"];

                    result.Add(u);
                }
            }
            
            return result;
        }
    }
}