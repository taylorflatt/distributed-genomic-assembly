using System;
using System.Data.Entity;
using System.Net;
using System.Web.Mvc;
using Genome.Models;
using Genome.Helpers;

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
                    SSHConfig ssh = new SSHConfig("login-0-0.research.siu.edu", genomeModel, "", out error);

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
                return View(genomeModel);

            else
                return RedirectToAction("DetailsPermissionError", "Error");
        }

        [HttpPost]
        public ActionResult Details()
        {
            // This is where I will handle the update for a specific job.
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
