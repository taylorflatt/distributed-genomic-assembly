using System;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Genome.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace Genome.Controllers
{
    public class ManageRolesController : Controller
    {
        private GenomeJobDbContext context = new GenomeJobDbContext();
        private ApplicationUser userContext = new ApplicationUser();
        private ApplicationUserManager _userManager;

        //Need this: http://stackoverflow.com/questions/27750918/mvc5-account-controller-null-reference-exception
        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { _userManager = value; }

        }
        //
        // GET: /ManageRoles/
        public ActionResult Index()
        {
            var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;

            //var userList = context.Users.OrderBy(u => u.UserName).ToList().Select(rr => new SelectListItem { Value = rr.UserName.ToString(), Text = rr.UserName }).ToList();
            //ViewBag.UserNames = userList;

            return View();
        }

        [HttpPost]
        public ActionResult ManageUsersRoles(string command)
        {
            if(command == "Add User To Role")
            {
                //do shit
            }

            else if (command == "Get User Role")
            {
                //do shit
            }

            else if (command == "Remove User from Role")
            {
                //do shit
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddUserToRole(string UserName, string RoleName)
        {
            //Make sure valid information is entered/selected.
            RoleChangeValidation(RoleName, UserName);

            //Admin > Verified > Unverified. The roles are mutually exclusive.
            ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

            if (user != null)
            {
                if (UserManager.GetRoles(user.Id).Contains("Admin"))
                    UserManager.RemoveFromRoles(user.Id, "Admin");

                else if (UserManager.GetRoles(user.Id).Contains("Verified"))
                    UserManager.RemoveFromRoles(user.Id, "Verified");

                else if (UserManager.GetRoles(user.Id).Contains("Unverified"))
                    UserManager.RemoveFromRoles(user.Id, "Unverified");

                UserManager.AddToRole(user.Id, RoleName);

                ViewBag.ResultMessage = "User added to role successfully !";
            }
            
            else
                ViewBag.RoleSelectError = "That user is not in the database.";

            // prepopulat roles for the view dropdown
            var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;

            return View("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GetRoles(string UserName)
        {
            //Make sure valid information is entered/selected.
            RoleChangeValidation(UserName);

            if (!string.IsNullOrWhiteSpace(UserName))
            {
                ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

                if (user != null)
                    ViewBag.RolesForThisUser = UserManager.GetRoles(user.Id);

                else
                    ViewBag.RoleSelectError = "That user is not in the database.";

                // Populate the roles for a dropdown.
                var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
                ViewBag.Roles = list;
            }

            else
                ViewBag.RoleSelectError = "That user is not in the database.";

            return View("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRoleForUser(string UserName, string RoleName)
        {
            //Make sure valid information is entered/selected.
            RoleChangeValidation(UserName, RoleName);

            ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

            if(user == null)
                ViewBag.RoleSelectError = "That user is not in the database.";

            else if (UserManager.IsInRole(user.Id, RoleName))
            {
                UserManager.RemoveFromRole(user.Id, RoleName);
                ViewBag.ResultMessage = "Role removed from this user successfully !";
            }

            else
                ViewBag.ResultMessage = "This user doesn't belong to selected role.";

            // Populate the roles for a dropdown.
            var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;

            return View("Index");
        }

        private void RoleChangeValidation(string UserName, string RoleName = "null")
        {
            //If they don't select a username for the user and leave it as the default, notify them to change it and return the view.
            if (UserName.Equals(""))
                ViewBag.UserSelectError = "Please select a valid user.";

            //If they don't select a role for the user and leave it as the default, notify them to change it and return the view.
            if (RoleName.Equals(""))
                ViewBag.RoleSelectError = "Please select a proper role.";

            var temp = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();

            ViewBag.Roles = temp;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}