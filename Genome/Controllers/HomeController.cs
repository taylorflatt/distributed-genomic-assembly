using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Genome.CustomFilters;
using Microsoft.AspNet.Identity.EntityFramework;
using Genome.Models;
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.Owin;

namespace Genome.Controllers
{
    //[AuthorizedLogin(Roles = Helpers.CustomRoles.Administrator + "," + Helpers.CustomRoles.Verified)]
    public class HomeController : Controller
    {
        private GenomeAssemblyDbContext db = new GenomeAssemblyDbContext();

        public ActionResult Index()
        {
            string username = HttpContext.User.Identity.GetUserName();

            var temp = from u in db.Users
                       where u.UserName.Equals(username)
                       select u;

            // This should only ever be iterated through once.
            foreach(var user in temp)
            {
                if(user.ClusterAccountVerified)
                    ViewBag.ShowCreateButton = "Show";
            }

            return View();
        }
        
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}