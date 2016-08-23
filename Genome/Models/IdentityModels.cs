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
        public bool ClusterAccountVerified { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class GenomeAssemblyDbContext : IdentityDbContext<ApplicationUser>
    {
        public GenomeAssemblyDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static GenomeAssemblyDbContext Create()
        {
            return new GenomeAssemblyDbContext();
        }

        public DbSet<GenomeModel> GenomeModels { get; set; }

        /// These need to be added if I end up going with a relational model.
        //public DbSet<Assemblers> Assemblers { get; set; }
        //public DbSet<Masurca> Masurca { get; set; }

        //public System.Data.Entity.DbSet<Genome.Models.ApplicationUser> ApplicationUsers { get; set; }
    }
}