using System.Web.Mvc;
using System.Web;
using Genome.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity;

namespace Genome.CustomFilters
{
    public class AuthorizedLoginAttribute : AuthorizeAttribute
    {
        public AuthorizedLoginAttribute()
        {
            View = "AuthorizeFailed";
        }

        public string View { get; set; }

        /// <summary>
        /// Check for Authorization
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);
            IsUserAuthorized(filterContext);
        }

        /// <summary>
        /// Method to check if the user is Authorized or not
        /// if yes continue to perform the action else redirect to an error page
        /// </summary>
        /// <param name="filterContext"></param>
        private void IsUserAuthorized(AuthorizationContext filterContext)
        {
            // If the Result returns null then the user is Authorized 
            if (filterContext.Result == null)
                return;

            //Get the current user.
            ApplicationUserManager UserManager = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();

            //Get the current user's role(s).
            var roles = UserManager.GetRoles(HttpContext.Current.User.Identity.GetUserId());

            //If the user is logged in then Navigate to Auth Failed View depending on their role.
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                //If they are unverified (newly registered), redirect them to a custom page.
                if (roles.Contains("Unverified"))
                {
                    var context = filterContext.HttpContext;
                    string redirectTo = "~/Account/UnverifiedUser";

                    if (!string.IsNullOrEmpty(context.Request.RawUrl))
                    {
                        redirectTo = string.Format("~/Account/UnverifiedUser",
                            HttpUtility.UrlEncode(context.Request.RawUrl));
                    }
                }

                //If they are verified, redirect them to a custom page.
                else
                {
                    var vr = new ViewResult();
                    vr.ViewName = View;

                    ViewDataDictionary dict = new ViewDataDictionary();
                    dict.Add("Message", "Sorry you are not Authorized to Perform this Action");

                    vr.ViewData = dict;

                    var result = vr;

                    filterContext.Result = result;
                }
            }

            //If the user is not logged in (and therefore not authorized), redirect them to the login page.
            else
            {
                var context = filterContext.HttpContext;
                string redirectTo = "~/Account/Login";

                if (!string.IsNullOrEmpty(context.Request.RawUrl))
                {
                    redirectTo = string.Format("~/Account/Login?ReturnUrl={0}",
                        HttpUtility.UrlEncode(context.Request.RawUrl));
                }
            }
        }
    }
}