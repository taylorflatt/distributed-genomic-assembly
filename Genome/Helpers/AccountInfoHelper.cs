using System;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Genome.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Genome.CustomFilters;
using System.Threading.Tasks;
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
                // Select all the users in the database.
                var roles = context.Users
                    .Select(u => new { Username = u.UserName, Role = u.Roles }).ToList();

                // Populate the username list.
                foreach (var user in roles)
                {
                    user.Role.Equals("Admin");
                    adminCount++;
                }

                return adminCount;
            }
        }
    }
}