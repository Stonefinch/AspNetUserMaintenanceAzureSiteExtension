using AspNetUserMaintenanceAzureSiteExtension.AspNetAuth;
using AspNetUserMaintenanceAzureSiteExtension.Data;
using AspNetUserMaintenanceAzureSiteExtension.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace AspNetUserMaintenanceAzureSiteExtension.Services
{
    public interface IUserRepository
    {
        IEnumerable<AspNetUserViewModel> ListAspNetUsers(AspNetUserSearchCriteria searchCriteria);

        IEnumerable<string> CreateUser(string userName, string email, string password, string roles);

        IEnumerable<string> SyncUserRoles(string userName, string roles);

        IEnumerable<string> CreateRole(string roleName);
    }

    public class UserRepository : IUserRepository
    {
        private const string ASPNETAUTHCONNECTIONSTRING = "ASPNETAUTHCONNECTIONSTRING";

        private IAzureConfiguration AzureConfiguration { get; set; }

        private ApplicationUserManager ApplicationUserManager { get; set; }

        private ApplicationRoleManager ApplicationRoleManager { get; set; }

        public UserRepository(IAzureConfiguration azureConfiguration)
        {
            this.AzureConfiguration = azureConfiguration;

            var connectionString = this.AzureConfiguration.GetConnectionString(ASPNETAUTHCONNECTIONSTRING);

            // note: the method to create this ApplicationUserManager is taken from the default visual studio asp.net project template with individual user account auth.
            // this ApplicationUserManager will be consumed by this repository similarly to how the AccountController in the default template consumes it.
            var appContext = new ApplicationDbContext(connectionString);
            this.ApplicationUserManager = new ApplicationUserManager(new UserStore<ApplicationUser>(appContext));
            this.ApplicationRoleManager = new ApplicationRoleManager(new RoleStore<IdentityRole>(appContext));
        }

        public IEnumerable<AspNetUserViewModel> ListAspNetUsers(AspNetUserSearchCriteria searchCriteria)
        {
            // entity framework is needed due to the ApplicationUserManager, so we'll use it here as well
            List<AspNetUserViewModel> result = null;

            using (var ctx = this.CreateContext())
            {
                var command =
@"select top 100
     u.UserName
    ,u.Email
    ,r.Name Role

from
    dbo.AspNetUsers u
    -- note: join may duplicate users in result set
    left join dbo.AspNetUserRoles ur on (u.Id = ur.UserId)
    left join dbo.AspNetRoles r on (ur.RoleId = r.Id)

where
    (@UserName is null or @UserName = '' or u.UserName like '%' + @UserName + '%')
    and (@Email is null or @Email = '' or u.Email like '%' + @Email + '%')
    and (@Role is null or @Role = '' or r.Name like '%' + @Role + '%')

order by 1
";

                result = ctx.Database
                    .SqlQuery<AspNetUserViewModel>(
                        command,
                        new SqlParameter("@UserName", searchCriteria.UserName ?? ""),
                        new SqlParameter("@Email", searchCriteria.Email ?? ""),
                        new SqlParameter("@Role", searchCriteria.Role ?? ""))
                    .ToList();
            }

            // query joins users to roles. Could be duplicates. Consolidate.
            result = result
                .GroupBy(x => x.UserName)
                .Select(x => new AspNetUserViewModel()
                {
                    UserName = x.Key,
                    Email = x.Min(g => g.Email),
                    Role = string.Join(", ", x.Select(g => g.Role))
                })
                .ToList();

            return result;
        }

        public IEnumerable<string> CreateUser(string userName, string email, string password, string roles)
        {
            if (String.IsNullOrWhiteSpace(userName))
                return new[] { $"User [{userName}] not created. username not provided." };

            if (String.IsNullOrWhiteSpace(email))
                return new[] { $"User [{userName}] not created. email not provided." };

            if (String.IsNullOrWhiteSpace(email))
                return new[] { $"User [{userName}] not created. password not provided." };
            
            roles = roles ?? "";
            
            var dbUser = this.ApplicationUserManager.FindByNameAsync(userName).Result;

            if (dbUser != null)
                return new[] { $"User [{userName}] not created. User already exists." };
            
            var user = new ApplicationUser() { UserName = userName, Email = email };
            var ir = this.ApplicationUserManager.CreateAsync(user, password).Result;

            if (!ir.Succeeded)
            {
                return ir.Errors;
            }

            return this.SyncUserRoles(userName, roles);
        }

        public IEnumerable<string> SyncUserRoles(string userName, string roles)
        {
            if (String.IsNullOrWhiteSpace(userName))
                return new[] { "username not provided. No Roles changed." };

            roles = roles ?? "";

            var dbUser = this.ApplicationUserManager.FindByNameAsync(userName).Result;

            if (dbUser == null)
                return new[] { $"Could not find user with username: {userName}. No Roles changed." };

            var result = new List<string>();

            var currentDbRoles = this.ApplicationUserManager.GetRolesAsync(dbUser.Id).Result;

            var desiredRoles = roles.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            // remove from roles in db but no longer desired
            var removeFromRoles = currentDbRoles.Except(desiredRoles).ToList();

            foreach (var roleToRemove in removeFromRoles)
            {
                var rr = this.ApplicationUserManager.RemoveFromRoleAsync(dbUser.Id, roleToRemove).Result;

                if (!rr.Succeeded)
                {
                    result.Add($"Could not remove user [{dbUser.UserName}] from role [{roleToRemove}].");
                    result.AddRange(rr.Errors);
                }
            }

            // add user to roles not current in
            var addToRoles = desiredRoles.Except(currentDbRoles).ToList();

            foreach (var roleToAdd in addToRoles)
            {
                // create role if it doesn't yet exist
                var exists = this.ApplicationRoleManager.RoleExistsAsync(roleToAdd).Result;

                if (!exists)
                {
                    var ir = this.ApplicationRoleManager.CreateAsync(new IdentityRole(roleToAdd)).Result;

                    if (!ir.Succeeded)
                    {
                        result.Add($"Could not create role: {roleToAdd}. User [{dbUser.UserName}] not added to role.");
                        result.AddRange(ir.Errors);
                        continue;
                    }
                }

                var rr = this.ApplicationUserManager.AddToRoleAsync(dbUser.Id, roleToAdd).Result;

                if (!rr.Succeeded)
                {
                    result.Add($"Could not add user [{dbUser.UserName}] to role [{roleToAdd}].");
                    result.AddRange(rr.Errors);
                }
            }

            return result;
        }

        public IEnumerable<string> CreateRole(string roleName)
        {
            if (String.IsNullOrWhiteSpace(roleName))
                return new[] { "roleName not provided." };

            var ir = this.ApplicationRoleManager.CreateAsync(new IdentityRole(roleName)).Result;

            return ir.Errors;
        }

        private UserContext CreateContext()
        {
            return new UserContext(this.AzureConfiguration.GetConnectionString(ASPNETAUTHCONNECTIONSTRING));
        }
    }
}