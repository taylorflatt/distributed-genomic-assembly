using System;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Genome.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using Genome.Helpers;

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

        /// <summary>
        /// Runs all the commands corresponding to the buttons in the view.
        /// </summary>
        /// <param name="UserName">The selected username passed from the view.</param>
        /// <param name="RoleName">The selected rolename passed from the view.</param>
        /// <param name="command">The particular command we run on the information.</param>
        /// <returns>Returns to the view with the information from running the selected command.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(string UserName, string RoleName, string command)
        {
            if (command == "Add User To Role")
                AddUserToRole(UserName, RoleName);

            else if (command == "Get User Role")
                ViewBag.UserRole = GetUserRole(UserName);

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

        /// <summary>
        /// Gets a list of all users and stores them in a viewbag to be sent back to the view.
        /// </summary>
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

                    ViewBag.ResultMessage = "All user roles were successfully retrieved!";

                    // Send the list to the view.
                    ViewBag.UserList = userList.ToList();
                }
            }

            catch(Exception e)
            {
                ViewBag.GetUsersError = e.Message; 
            }
        }

        /// <summary>
        /// Completely removes a user from the system. This will maintain their content however. Note, the last admin will not be able to be removed.
        /// </summary>
        /// <param name="UserName">The username of the user to be deleted.</param>
        private async Task DeleteUser(string UserName)
        {
            try
            {
                // If we are trying to remove an admin and there is only a single admin left, we CANNOT remove that admin. We don't want to get locked out.
                if (GetUserRole(UserName).Equals("Admin") && AccountInfoHelper.NumberAdminsLeft() == 1)
                    ViewBag.ResultMessageError = "Cannot delete " + UserName + " because they are the last admin. This action would result in a lockout.";

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
                ViewBag.ResultMessageError = "The user was unable to be removed from the system. Error Message: " + e.Message;
            }
        }

        /// <summary>
        /// Add a user to a particular role. Note, this will remove their previous role.
        /// </summary>
        /// <param name="UserName">The user whose role is to be changed.</param>
        /// <param name="RoleName">The new role of the user.</param>
        /// Note: I made it so admins cannot be deleted. This was a design choice but can be easily changed by removing portions of code below. 
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
                    ViewBag.ResultMessageError = "An admin cannot change roles to prevent being locked out. " + UserName + " must be deleted!";
                    return;
                }

                if (UserManager.GetRoles(user.Id).Contains(RoleName))
                {
                    ViewBag.ResultMessageError = "The user already has that role assigned.";
                    return;
                }

                else if (UserManager.GetRoles(user.Id).Contains("Verified"))
                    UserManager.RemoveFromRoles(user.Id, "Verified");

                else if (UserManager.GetRoles(user.Id).Contains("Unverified"))
                    UserManager.RemoveFromRoles(user.Id, "Unverified");

                UserManager.AddToRole(user.Id, RoleName);

                ViewBag.ResultMessage = "User added to role successfully !";
            }

            else if (RoleName.Equals(""))
                ViewBag.ResultMessageError = "Please select a valid role.";

            else
                ViewBag.ResultMessageError = "Cannot find \"" + UserName + "\" in the database.";

            // prepopulate roles for the view dropdown. This is required or you will run into errors.
            var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;
        }

        /// <summary>
        /// Retrieves a user's current role.
        /// </summary>
        /// <param name="UserName">The user whose role we are retrieving.</param>
        /// <returns>Returns the user's role as a string.</returns>
        private string GetUserRole(string UserName)
        {
            if (!string.IsNullOrWhiteSpace(UserName))
            {
                UserName = UserName.Trim();

                ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

                if (user != null)
                {
                    if (UserManager.GetRoles(user.Id).Count <= 0)
                        return ViewBag.ResultMessageError = "This user doesn't have any roles assigned to them.";

                    else
                    {
                        ViewBag.ResultMessage = "The user's role was successfully retrieved!";
                        return (string)UserManager.GetRoles(user.Id).ElementAt(0);
                    }
                }

                else
                    return ViewBag.ResultMessageError = "Cannot find \"" + UserName + "\" in the database.";
            }

            else
                return ViewBag.ResultMessageError = "Cannot find \"" + UserName + "\" in the database.";
        }

        /// <summary>
        /// Delete a particular role for a user. This option will reduce their role rank by one. A user will never be without a role.
        /// </summary>
        /// <param name="UserName">The user whose rank will be reduced.</param>
        private void DeleteRoleForUser(string UserName)
        {
            ApplicationUser user = context.Users.FirstOrDefault(u => u.UserName.Equals(UserName, StringComparison.CurrentCultureIgnoreCase));

            if(user == null)
                ViewBag.ResultMessageError = "Cannot find \"" + UserName + "\" in the database.";

            else
            {
                // Don't let them be removed from the admin role to prevent lockout.
                if (UserManager.GetRoles(user.Id).Contains("Admin") && AccountInfoHelper.NumberAdminsLeft() == 1)
                    ViewBag.ResultMessageError = UserName + " is an admin and the only admin remaining. The user must be deleted instead.";

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
                        ViewBag.ResultMessageError = "Role was unsuccessfully reduced from " + oldRole + " because that is the lowest role possible. If you like, you may delete " + UserName + " instead. ";

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

            // Populate the roles for a dropdown. This is required else an error will occur.
            var list = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;
        }

        /// <summary>
        /// Validation method for verifying that the proper dropdowns have been selected when applicable.
        /// </summary>
        /// <param name="UserName">A string representing a username.</param>
        /// <param name="RoleName">An optional string representing a role name.</param>
        private void RoleChangeValidation(string UserName, string RoleName = "null")
        {
            //If they don't select a username for the user and leave it as the default, notify them to change it and return the view.
            if (UserName.Equals(""))
                ViewBag.ResultMessageError = "Please select a valid user.";

            //If they don't select a role for the user and leave it as the default, notify them to change it and return the view.
            if (RoleName.Equals(""))
                ViewBag.ResultMessageError = "Please select a proper role.";

            var temp = context.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();

            ViewBag.Roles = temp;
        }

        /// <summary>
        /// Disposal method.
        /// </summary>
        /// <param name="disposing">Determines whether disposal needs to occur.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                context.Dispose();

            base.Dispose(disposing);
        }
    }
}