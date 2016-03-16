using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Genome.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public int DawgTag { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class GenomeJobDbContext : IdentityDbContext<ApplicationUser>
    {
        public GenomeJobDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        //public static ApplicationDbContext Create()
        //{
        //    return new ApplicationDbContext();
        //}


        public static GenomeJobDbContext Create()
        {
            return new GenomeJobDbContext();
        }

        public System.Data.Entity.DbSet<Genome.Models.GenomeModel> GenomeModels { get; set; }

        //public System.Data.Entity.DbSet<Genome.Models.ApplicationUser> ApplicationUsers { get; set; }
    }
}