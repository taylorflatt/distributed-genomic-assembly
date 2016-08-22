using System;
using System.Net;
using System.Web.Mvc;
using Genome.Models;
using Genome.Helpers;
using Renci.SshNet;
using System.Collections.Generic;
using Microsoft.AspNet.Identity;
using System.Linq;

namespace Genome.Controllers
{
    public class GenomeAssemblyController : Controller
    {
        private GenomeAssemblyDbContext db = new GenomeAssemblyDbContext();

        // TODO: Figure out the routing issue with the permission checks. For now, they are commented out.
        [Authorize(Roles = "Admin, Verified")]
        public ActionResult Create()
        {
            // TODO: Need policy-based authorization workaround for this...aka open up the parameter in the account model
            // and extending the authorize attribute to include it as a parameter to check all in one sweep. If time permits...
            using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
            {
                string username = HttpContext.User.Identity.GetUserName();

                var temp = from u in db.Users
                           where u.UserName.Equals(username)
                           select u.ClusterAccountVerified;

                // This should only ever be iterated through once.
                foreach (var acctStat in temp)
                {
                    if (!acctStat)
                        return RedirectToAction("CreateJobErrorCluster", "Error");
                }

                return View(new GenomeModel());
            }
        }

        /// <summary>
        /// Post method of actually creating the job per the user's description. The creation of the scripts and initiating contact with BigDog is done here.
        /// </summary>
        /// <param name="genomeModel">The model that contains the job data.</param>
        /// <returns>Returns the view for the view for creating a job.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(GenomeModel genomeModel)
        {
            if (ModelState.IsValid)
            {
                // TODO: Consider the JELLYFISH SIZE. Rec size is est_genome_size  * estimated coverage. (This should be done in Javascript).
                // Should that be a field that we include in the model so we can calculate a safe size or just
                // have them do it? I think just have them do it with a tooltip and make it a required field.
                try
                {
                    #region Set General Information

                    HelperMethods.SetDefaultMasurcaValues(genomeModel);

                    genomeModel.OverallCurrentStep = 1;
                    genomeModel.OverallStatus = StepDescriptions.INITIAL_STEP;
                    genomeModel.NumberOfAssemblers = HelperMethods.NumberOfAssemblers(genomeModel.UseMasurca, genomeModel.UseSGA, genomeModel.UseWGS);

                    // TODO: Potentially add the model to the DB now with junk data to reserve the UUID so we don't have to use a SEED value at all. This 
                    // reduces the potential of something going wrong and two people ending up with a race condition or something.

                    genomeModel.CreatedBy = User.Identity.Name;

                    // TODO: Need to look into this to see about a translation to display to a user rather than the UK time it gives.
                    genomeModel.CreatedDate = DateTime.UtcNow;

                    #endregion

                    #region Create Scripts

                    List <string> dataSources = HelperMethods.ParseUrlString(genomeModel.DataSource);

                    Random rand = new Random();
                    int seed = rand.Next(198, 1248712);

                    JobBuilder builder = new JobBuilder(genomeModel, dataSources, seed);
                    builder.GenerateConfigs();

                    genomeModel.Seed = builder.seed; // Set seed value here so we know it is 100% definitely set.
                    #endregion

                    #region Connect To BigDog and Test Data Connection

                    string badUrl = HelperMethods.TestJobUrls(genomeModel.SSHUser, genomeModel.SSHPass, genomeModel.DataSource);

                    if (string.IsNullOrEmpty(badUrl) && string.IsNullOrEmpty(ErrorHandling.error))
                    {
                        /// We can instead pass in the SEED variable which will be used to reference the method in Locations to grab the correct file(s).
                        /// This is the ideal solution which will be implemented only once we know that the system works with the direct URL.
                        builder.CreateJob();

                        // No error so proceed.
                        if (ErrorHandling.NoError())
                        {
                            db.GenomeModels.Add(genomeModel);
                            db.SaveChanges();

                            return RedirectToAction("Details", new { id = genomeModel.uuid });
                        }

                        // Redisplay the data and display the error.
                        else
                        {
                            genomeModel.JobError = "We encountered an error while trying to submit your job. The following is the error we encountered: " +  ErrorHandling.error;

                            return View(genomeModel);
                        }
                    }

                    // Redisplay the data and display the error.
                    else
                    {
                        genomeModel.JobError = "There was an error with the URLs provided. We weren't able to locate or download at least"
                            + " one of your files. The file we had a problem with was: " + badUrl + ". Please make sure you typed the URL correctly"
                            + " and it is accessible. If this is in error, please contact an administrator promptly with the details.";

                        if (!string.IsNullOrEmpty(ErrorHandling.error))
                            genomeModel.JobError = string.Concat(genomeModel.JobError, " The following is additional error information that we encountered: " + ErrorHandling.error);

                        return View(genomeModel);
                    }

                    #endregion
                }

                catch (Exception e)
                {
                    genomeModel.JobError = "There has been an uncaught error. " + e;
                }
            }

            return View(genomeModel);
        }

        // GET: GenomeAssembly/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            GenomeModel genomeModel = db.GenomeModels.Find(id);

            if (genomeModel == null)
                return HttpNotFound();

            string currentUser = User.Identity.Name;

            // Only let the user that created the job view that job.
            if (genomeModel.CreatedBy.Equals(currentUser))
            {
                if (!string.IsNullOrEmpty(genomeModel.DownloadLink))
                {
                    // We need to display the download link for the user which is set in CheckJobStatus.UploadData() and stored in the model 
                    // under DownloadLink.
                }

                return View(genomeModel);
            }

            else
                return RedirectToAction("DetailsPermissionError", "Error");
        }

        /// <summary>
        /// Post method for job details. We perform the tasks of actually cancelling/updating jobs.
        /// </summary>
        /// <param name="id">ID of a particular job.</param>
        /// <param name="command">The task we wish to accomplish (update/cancel).</param>
        /// <returns>Returns the particular job's detail page with the results of the task.</returns>
        [HttpPost]
        public ActionResult Details(int id, string command)
        {
            GenomeModel genomeModel = db.GenomeModels.Find(id);

            if (command == "Update Status" && string.IsNullOrEmpty(genomeModel.DownloadLink))
                JobMaintenance.UpdateStatus(genomeModel);

            if (command == "Cancel Job")
            {
                // Add a method in JobMaintenance that reflects cancelling a job. Best place for it.
            }

            db.SaveChanges();

            return View(genomeModel);
        }

        /// <summary>
        /// Disposal method.
        /// </summary>
        /// <param name="disposing">Determines whether disposal needs to occur.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}