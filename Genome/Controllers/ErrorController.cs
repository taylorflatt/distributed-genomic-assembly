using System.Web.Mvc;

namespace Genome.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Error
        public ActionResult Error()
        {
            return View();
        }

        /// <summary>
        /// Shows users a permissions error page.
        /// </summary>
        /// <returns>Returns the error view.</returns>
        public ActionResult DetailsPermissionError()
        {
            ViewBag.Error = "You do not have sufficient permissions to view the details of this job. If you believe that this is in error, you should contact an administrator by selecting the contact link above.";

            return View("Error");
        }

        /// <summary>
        /// Shows users a cluster error page after attempting to view the "Create A Job" page without the cluster verification process done.
        /// </summary>
        /// <returns>Returns the error view.</returns>
        public ActionResult CreateJobErrorCluster()
        {
            ViewBag.Error = "You are unable to access the \"Create A Job\" page until you have fulfilled all requirements to submit a job. Please verify your account first.";

            return View("Error");
        }
    }
}
