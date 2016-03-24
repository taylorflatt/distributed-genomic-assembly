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

namespace Genome.Controllers
{
    //[AuthorizedLogin(Roles = Helpers.CustomRoles.Administrator)]
    public class ManageRolesController : Controller
    {
        private GenomeAssemblyDbContext context = new GenomeAssemblyDbContext();
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

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManageUsersRoles(string UserName, string RoleName, string command)
        {
            if (command == "Add User To Role")
                AddUserToRole(UserName, RoleName);

            else if (command == "Get User Role")
                GetRoles(UserName);

            else if (command == "Remove User from Role")
                DeleteRoleForUser(UserName, RoleName);

            else if (command == "Delete User")
                await DeleteUser(UserName);

            // Populate the roles for a dropdown.
            var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;

            return View("Index");
        }

        private async Task DeleteUser(string UserName)
        {
            try
            {
                var user = await UserManager.FindByNameAsync(UserName);
                var logins = user.Logins;

                // Remove the logins if any.
                foreach (var login in logins.ToList())
                {
                    await _userManager.RemoveLoginAsync(login.UserId, new UserLoginInfo(login.LoginProvider, login.ProviderKey));
                }

                var rolesForUser = await UserManager.GetRolesAsync(user.Id);

                // Remove the user roles if any.
                if (rolesForUser.Count() > 0)
                {
                    foreach (var role in rolesForUser.ToList())
                    {
                        var result = await UserManager.RemoveFromRoleAsync(user.Id, role);
                    }
                }

                // Finally remove the user.
                await UserManager.DeleteAsync(user);

                ViewBag.ResultMessage = "User successfully removed.";
            }

            catch(Exception e)
            {
                ViewBag.DeleteError = "The user was unable to be removed from the system. Error Message: " + e.Message;
            }

        }

        private void AddUserToRole(string UserName, string RoleName)
        {
            //Make sure valid information is entered/selected.
            RoleChangeValidation(UserName, RoleName);

            //Admin > Verified > Unverified. The roles are mutually exclusive.
            ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

            if (user != null && !RoleName.Equals(""))
            {
                if(UserManager.GetRoles(user.Id).Contains("Admin"))
                {
                    ViewBag.UserSelectError = "An admin cannot change roles to prevent being locked out. They must be deleted!";
                    return;
                }

                if (UserManager.GetRoles(user.Id).Contains(RoleName))
                {
                    ViewBag.RoleSelectError = "The user already has that role assigned.";
                    return;
                }

                //else if (UserManager.GetRoles(user.Id).Contains("Admin"))
                //    UserManager.RemoveFromRoles(user.Id, "Admin");

                else if (UserManager.GetRoles(user.Id).Contains("Verified"))
                    UserManager.RemoveFromRoles(user.Id, "Verified");

                else if (UserManager.GetRoles(user.Id).Contains("Unverified"))
                    UserManager.RemoveFromRoles(user.Id, "Unverified");

                UserManager.AddToRole(user.Id, RoleName);

                ViewBag.ResultMessage = "User added to role successfully !";
            }

            else if (RoleName.Equals(""))
                ViewBag.RoleSelectError = "Please select a valid role.";

            else
                ViewBag.RoleSelectError = "That user is not in the database.";

            // prepopulat roles for the view dropdown
            var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;
        }

        private void GetRoles(string UserName)
        {
            //Make sure valid information is entered/selected.
            RoleChangeValidation(UserName);

            if (!string.IsNullOrWhiteSpace(UserName))
            {
                ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

                if (user != null)
                {
                    if(UserManager.GetRoles(user.Id).Count <= 0)
                        ViewBag.RolesForThisUser = "This user currently has no roles assigned.";

                    else
                        ViewBag.RolesForThisUser = (string) UserManager.GetRoles(user.Id).ElementAt(0);
                }

                else
                    ViewBag.RoleSelectError = "That user is not in the database.";

                // Populate the roles for a dropdown.
                var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();

                ViewBag.Roles = list;
            }

            else
                ViewBag.RoleSelectError = "That user is not in the database.";
        }

        private void DeleteRoleForUser(string UserName, string RoleName)
        {
            //Make sure valid information is entered/selected.
            RoleChangeValidation(UserName, RoleName);

            ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

            if(user == null)
                ViewBag.UserSelectError = "That user is not in the database.";

            else if(RoleName.Equals(""))
                ViewBag.RoleSelectError = "Please select a valid role.";

            else if (UserManager.IsInRole(user.Id, RoleName))
            {
                // Don't let them be removed from the admin role to prevent lockout.
                
                if (UserManager.GetRoles(user.Id).Contains("Admin"))
                    ViewBag.ResultMessage = "This user is an admin and cannot be removed from the role. The user must be deleted instead.";

                else
                {
                    UserManager.RemoveFromRole(user.Id, RoleName);
                    ViewBag.ResultMessage = "Role removed from this user successfully !";
                }
            }

            else
                ViewBag.ResultMessage = "This user doesn't belong to selected role.";

            // Populate the roles for a dropdown.
            var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;
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