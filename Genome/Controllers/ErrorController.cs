using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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

        public ActionResult DetailsPermissionError()
        {
            ViewBag.Error = "You do not have sufficient permissions to view the details of this job. If you believe that this is in error, you should contact an administrator by selecting the contact link above.";

            return View("Error");
        }
    }
}
