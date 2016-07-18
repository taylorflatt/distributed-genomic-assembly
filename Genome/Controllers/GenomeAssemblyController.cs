using System;
using System.Net;
using System.Web.Mvc;
using Genome.Models;
using Genome.Helpers;
using Renci.SshNet;
using System.Collections.Generic;
using Genome.CustomFilters;

namespace Genome.Controllers
{
    public class GenomeAssemblyController : Controller
    {
        private GenomeAssemblyDbContext db = new GenomeAssemblyDbContext();

        [AuthorizedLogin(Roles = CustomRoles.Administrator)]
        [AuthorizedLogin(Roles = CustomRoles.Verified)]
        public ActionResult Create()
        {
            // Return the VerifyAccount view if their account is not verified...otherwise return this view.
            return View(new GenomeModel());
        }

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
                    HelperMethods.SetDefaultMasurcaValues(genomeModel);

                    genomeModel.NumAssemblers = HelperMethods.NumberOfAssemblers(genomeModel);

                    genomeModel.OverallCurrentStep = 1;
                    genomeModel.OverallStatus = StepDescriptions.INITIAL_STEP;

                    genomeModel.CreatedBy = User.Identity.Name;
                    genomeModel.CreatedDate = DateTime.UtcNow;

                    ConfigBuilder builder = new ConfigBuilder();

                    List <string> dataSources = HelperMethods.ParseUrlString(genomeModel.DataSource);

                    int seed;

                    string initURL = builder.BuildInitConfig(dataSources, out seed);
                    string masurcaURL = builder.BuildMasurcaConfig(genomeModel, dataSources, seed);

                    string error = "";
                    string badUrl = "";

                    /// TODO: Start the download of their data on BigDog to see if we can get each URL. If we cannot, then we just terminate 
                    /// and tell them the error we receieved.
                    SSHConfig ssh = new SSHConfig(Locations.BD_IP, genomeModel, "", out error);

                    ssh.TestJobUrls(out error, out badUrl);

                    if(string.IsNullOrEmpty(badUrl) && string.IsNullOrEmpty(error))
                    {
                        /// We can instead pass in the SEED variable which will be used to reference the method in Locations to grab the correct file(s).
                        /// This is the ideal solution which will be implemented only once we know that the system works with the direct URL.
                        ssh.CreateJob(initURL, masurcaURL, out error);

                        //ssh.CreateConnection(out error);

                        // No error so proceed.
                        if (string.IsNullOrEmpty(error))
                        {
                            //db.GenomeModels.Add(genomeModel);
                            //db.SaveChanges();
                            return RedirectToAction("Details", new { id = genomeModel.uuid });
                        }

                        // Redisplay the data and display the error.
                        else
                        {
                            ViewBag.ConnectionError = "There was an error with the connection to BigDog. The following is the error we encountered: ";
                            ViewBag.ConnectionErrorDetails = error;

                            return View(genomeModel);
                        }
                    }

                    // Redisplay the data and display the error.
                    else
                    {
                        ViewBag.ConnectionError = "There was an error with the URLs provided. We weren't able to locate or download at least one of your files. The file we had a problem with was: " + badUrl + ".";

                        if(!string.IsNullOrEmpty(error))
                            ViewBag.ConnectionErrorDetails = "The following is additional error information that we encountered: " + error;
                        

                        return View(genomeModel);
                    }
                }

                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
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

                    ViewBag.DataUploaded = "Your data has successfully uploaded and is ready for download. Using the credentials that you use to access this website, please open the link and enter those credentials to begin the data download.";
                }

                return View(genomeModel);
            }

            else
                return RedirectToAction("DetailsPermissionError", "Error");
        }

        [HttpPost]
        public ActionResult Details(int id, string command)
        {
            GenomeModel genomeModel = db.GenomeModels.Find(id);

            bool jobsToUpload = false;
            string error = "";

            if (command == "Update Status")
                CheckJobStatus.UpdateStatus(id, ref jobsToUpload, out error);

            if (command == "Cancel Job")
            {
                using (var client = new SshClient(Locations.BD_IP, Locations.BD_UPDATE_KEY_PATH))
                {
                    client.Connect();

                    LinuxCommands.CancelJob(client, genomeModel.SGEJobId, out error);

                    if(string.IsNullOrEmpty(error))
                        ViewBag.JobCancelSuccess = "Your job has been successfully cancelled. All progress will reflect its current position at the time it was cancelled.";

                    else
                        ViewBag.JobCancelFailure = "Your job has not been successfully cancelled with the following error: " + error;
                }
            }

            if(string.IsNullOrEmpty(error))
            {
                // If the job is ready to be uploaded (according to our updatestatuses method) and a download link hasn't been assigned (upload hasn't 
                // finished), then we will display to the user that their data is currently being uploaded.
                if(jobsToUpload && string.IsNullOrEmpty(genomeModel.DownloadLink))
                {
                    // Run background task that will upload the data to the web server FTP for download by the user.
                    ViewBag.DataUploading = "Your job has completed executing on BigDog and is currently being packaged and uploaded to our server so that you may access it. Please be patient as this could take some time depending on the size of the data. Once it has successfully uploaded, a link will be made available from which you will be able to download your data.";
                }

                return View("Details");
            }

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}