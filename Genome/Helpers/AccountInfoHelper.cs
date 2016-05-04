using System.Data;
using System.Linq;
using Genome.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;

namespace Genome.Helpers
{
    public class AccountInfoHelper
    {
        private GenomeAssemblyDbContext context = new GenomeAssemblyDbContext();
        private ApplicationUser userContext = new ApplicationUser();

        public static int NumberAdminsLeft()
        {
            int adminCount = 0;

            using (var context = new IdentityDbContext())
            {
                Dictionary<int, string> roleList = CustomRoles.Roles();

                // Select all the users in the database.
                var users = context.Users
                    .Select(u => new { Username = u.UserName, Role = u.Roles}).ToList();

                // Populate the username list.
                foreach (var user in users)
                {
                    if (user.Role.ElementAt(0).RoleId.Equals(roleList.Keys.Single(k => roleList[k] == "Admin").ToString()))
                        adminCount++;
                }

                return adminCount;
            }
        }
    }
}