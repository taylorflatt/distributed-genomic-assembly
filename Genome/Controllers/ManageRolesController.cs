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
using Genome.Helpers;
using System.Collections;

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

        // Contains all of the commands that are run when pressing the buttons in the view.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(string UserName, string RoleName, string command)
        {
            if (command == "Add User To Role")
                AddUserToRole(UserName, RoleName);

            else if (command == "Get User Role")
                GetAllUserRoles(UserName);

            else if (command == "Remove User from Role")
                DeleteRoleForUser(UserName);

            else if (command == "Delete User")
                await DeleteUser(UserName);

            else if (command == "Get Users List")
                GetUsersList();

            // Populate the roles for a dropdown.
            var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;

            return View("Index");
        }

        private void GetUsersList()
        {
            try
            {
                using (var context = new IdentityDbContext())
                {
                    // Create a tuple to hold the user's username and role name.
                    List<Tuple<string, string>> userList = new List<Tuple<string, string>>();

                    // Select all the users in the database.
                    var temp = context.Users
                        .Select(u => new { Username = u.UserName }).ToList();

                    // Populate the username list.
                    foreach (var user in temp)
                    {
                        userList.Add(Tuple.Create(user.Username, GetUserRole(user.Username)));
                    }

                    // Send the list to the view.
                    ViewBag.UserList = userList.ToList();
                }
            }

            catch(Exception e)
            {
                ViewBag.GetUsersError = e.Message; 
            }
        }

        private async Task DeleteUser(string UserName)
        {
            try
            {
                // If we are trying to remove an admin and there is only a single admin left, we CANNOT remove that admin. We don't want to get locked out.
                if (GetUserRole(UserName).Equals("Admin") && AccountInfoHelper.NumberAdminsLeft() == 1)
                    ViewBag.DeleteError = "Cannot delete this user because they are the last admin. That would result in being locked out.";

                else
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

        private string GetUserRole(string UserName)
        {
            if (!string.IsNullOrWhiteSpace(UserName))
            {
                ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

                if (user != null)
                {
                    if (UserManager.GetRoles(user.Id).Count <= 0)
                        throw new Exception("This user doesn't have any roles assigned to them.");

                    else
                        return (string)UserManager.GetRoles(user.Id).ElementAt(0);
                }

                else
                    throw new Exception("Cannot find " + UserName + " in the database.");
            }

            else
                throw new Exception("Cannot find " + UserName + " in the database.");
        }

        private void GetAllUserRoles(string UserName)
        {
            //Make sure valid information is entered/selected.
            RoleChangeValidation(UserName);

            if (!string.IsNullOrWhiteSpace(UserName))
            {
                ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

                if (user != null)
                {
                    if (UserManager.GetRoles(user.Id).Count <= 0)
                        ViewBag.RolesForThisUser = "This user currently has no roles assigned.";

                    else
                    {
                        ViewBag.RolesForThisUser = (string)UserManager.GetRoles(user.Id).ElementAt(0);
                        ViewBag.ResultMessage = "User roles successfully retrieved.";
                    }
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

        private void DeleteRoleForUser(string UserName)
        {
            ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

            if(user == null)
                ViewBag.UserSelectError = "That user is not in the database.";

            else
            {
                // Don't let them be removed from the admin role to prevent lockout.
                if (UserManager.GetRoles(user.Id).Contains("Admin") && AccountInfoHelper.NumberAdminsLeft() == 1)
                    ViewBag.AdminDeleteError = "This user is an admin and the only admin remaining. The user must be deleted instead.";

                else
                {
                    // Get all the user roles (will only be 1).
                    var oldRole = (string)UserManager.GetRoles(user.Id).ElementAt(0);

                    // Get the list of roles.
                    Dictionary<int, string> roleList = CustomRoles.Roles();

                    // Get the key associated with the role name.
                    var key = roleList.Keys.Single(k => roleList[k] == oldRole);

                    // If they are already the lowest user role, then we cannot remove them from that role.
                    if(key == roleList.Count - 1)
                    {
                        ViewBag.ResultMessageError = "Role was unsuccessfully removed from " + oldRole + " because that is the lowest role possible. If you like, you may delete them instead. ";
                    }

                    else
                    {
                        // Remove the user from their previous role.
                        UserManager.RemoveFromRole(user.Id, oldRole);

                        // Now we add the user to the role right below their old role.
                        AddUserToRole(UserName, roleList[key + 1]);

                        ViewBag.ResultMessage = "Role removed from this user successfully! User has been added to the " + roleList[key + 1] + " role.";
                    }
                }
            }

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