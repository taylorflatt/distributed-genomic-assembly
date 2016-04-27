﻿using System;
using System.Data.Entity;
using System.Net;
using System.Web.Mvc;
using Genome.Models;
using Genome.Helpers;
using Renci.SshNet;

namespace Genome.Controllers
{
    public class GenomeAssemblyController : Controller
    {
        private GenomeAssemblyDbContext db = new GenomeAssemblyDbContext();

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
                // TODO: Consider the JELLYFISH SIZE. Rec size is est_genome_size  * estimated coverage.
                // Should that be a field that we include in the model so we can calculate a safe size or just
                // have them do it? I think just have them do it with a tooltip and make it a required field.
                try
                {
                    // If they don't have jump reads.
                    if (genomeModel.JumpReads == false)
                        genomeModel.JumpLength = 0;

                    // If they don't have paired-end reads.
                    if (genomeModel.PEReads == false)
                        genomeModel.PairedEndLength = 0;

                    if (genomeModel.MasurcaPEMean == null)
                        // Set Mean default value.

                    if (genomeModel.MasurcaPEStdev == null)
                        // Set std dev default value.

                    if (genomeModel.MasurcaGraphKMerValue == null)
                        // Set graph kmer default value.

                    if (genomeModel.MasurcaKMerErrorCount == null)
                        // Set masurca kmer error threshold value.

                    if (genomeModel.MasurcaThreadNum == null)
                        genomeModel.MasurcaThreadNum = 20;

                    int numAssemblers = 0;

                    if (genomeModel.UseMasurca)
                        numAssemblers++;

                    if (genomeModel.UseSGA)
                        numAssemblers++;

                    if (genomeModel.UseWGS)
                        numAssemblers++;


                    genomeModel.MasurcaCurrentStep = 1;
                    genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), 1);

                    genomeModel.OverallCurrentStep = 1;
                    genomeModel.OverallStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GenerateOverallStepList(numAssemblers), 1);

                    genomeModel.CreatedBy = User.Identity.Name;
                    genomeModel.CreatedDate = DateTime.UtcNow;

                    // THIS IS FOR SUBMITTING A JOB ONLY. IT NEEDS A VALID VALUE THAT WE WILL OVERWRITE LATER. REMOVE LATER.
                    //genomeModel.CompletedDate = null;
                    // THIS IS FOR SUBMITTING A JOB ONLY. IT NEEDS A VALID VALUE THAT WE WILL OVERWRITE LATER. REMOVE LATER.

                    //string path = "temp";
                    //ConfigBuilder builder = new ConfigBuilder();

                    //string[] dataArray = genomeModel.DataSource.Split(',');

                    //builder.BuildMasurcaConfig(genomeModel, dataArray);
                    //builder.BuildInitConfig(genomeModel, dataArray);

                    string error = "";

                    //SSHConfig ssh = new SSHConfig("login-0-0.research.siu.edu", genomeModel, builder.InitConfigURL);
                    SSHConfig ssh = new SSHConfig(Locations.GetBigDogIp(), genomeModel, "", out error);

                    ssh.CreateJob(out error);

                    //ssh.CreateConnection(out error);

                    // No error so proceed.
                    if (string.IsNullOrEmpty(error))
                    {
                        db.GenomeModels.Add(genomeModel);
                        db.SaveChanges();
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
                CheckJobStatus.UpdateStatuses(id, ref jobsToUpload, out error);

            if (command == "Cancel Job")
            {
                using (var client = new SshClient(Locations.GetBigDogIp(), Locations.GetBigDogUpdateKeyLocation()))
                {
                    client.Connect();

                    LinuxCommands.CancelJob(client, genomeModel.SGEJobId, out error);

                    if(string.IsNullOrEmpty(error))
                        ViewBag.JobCancelSuccess = "Your job has been successfully cancelled. All progress will reflect its current position at the time it was cancelled.";

                    else
                        ViewBag.JobCancelFailure = "Your job has not been successfully cancelled with the following error: " + error;
                }
            }
                
                // Need to run the qdel command on bigdog. Needs to take the SGEID of the job and should be good to go.

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
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
