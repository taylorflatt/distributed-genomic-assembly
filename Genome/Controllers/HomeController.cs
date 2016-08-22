using System;
using System.Linq;
using System.Web.Mvc;
//using Genome.CustomFilters;
using Genome.Models;
using System.Data;

namespace Genome.Controllers
{
    //[AuthorizedLogin(Roles = Helpers.CustomRoles.Administrator + "," + Helpers.CustomRoles.Verified)]
    public class HomeController : Controller
    {
        private GenomeAssemblyDbContext db = new GenomeAssemblyDbContext();

        public ActionResult Index(HomeViewModel model)
        {
            string username = HttpContext.User.Identity.Name;

            try
            {
                var user = from u in db.Users
                           where u.UserName.Equals(username)
                           select u;

                model.ClusterAccountVerified = user.Single().ClusterAccountVerified;

                if (!ModelState.IsValid)
                    return View(model);
            }

            catch(InvalidOperationException e)
            {
                model.Error = "No user could be found when searching for a user to determine if their cluster account has been verified.";
            }

            return View(model);
        }
        
        public ActionResult About()
        {
            return View();
        }

        public ActionResult FAQ()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}