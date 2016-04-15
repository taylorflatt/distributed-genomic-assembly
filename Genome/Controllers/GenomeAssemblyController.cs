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
                    if(genomeModel.PEReads == false)
                        genomeModel.PairedEndLength = 0;

                    if(genomeModel.MasurcaPEMean == null)
                        // Set Mean default value.

                    if(genomeModel.MasurcaPEStdev == null)
                        // Set std dev default value.

                    if(genomeModel.MasurcaGraphKMerValue == null)
                        // Set graph kmer default value.

                    if(genomeModel.MasurcaKMerErrorCount == null)
                        // Set masurca kmer error threshold value.

                    if(genomeModel.MasurcaThreadNum == null)
                        genomeModel.MasurcaThreadNum = 20;


                    genomeModel.CreatedBy = User.Identity.Name;
                    genomeModel.CreatedDate = DateTime.Now;
                    genomeModel.JobStatus = "Pending";

                    //string path = "temp";
                    //string error = "";

                    //SSHConfig sshConnection = new SSHConfig();
                    //bool connectionStatus = sshConnection.CreateConnection("login-0-0.research.siu.edu", genomeModel, path, out error);

                    // STRICTLY FOR TESTING PURPOSES, DELETE AFTER DEMO
                    db.GenomeModels.Add(genomeModel);
                    db.SaveChanges();

                    SSHConfig ssh = new SSHConfig();
                    ConfigBuilder builder = new ConfigBuilder();
                    string[] dataArray = genomeModel.DataSource.Split(',');
                    builder.BuildMasurcaConfig(genomeModel, dataArray);
                    builder.BuildInitConfig(genomeModel, dataArray);
                    string error = "";
                    ssh.CreateConnection("login-0-0.research.siu.edu", genomeModel, builder.InitConfigURL, out error);
                    return RedirectToAction("Details", new { id = genomeModel.uuid });
                    // END COMMENT

                    // There were no errors making the connection. Add model to the db and continue.
                    //if (connectionStatus == true)
                    //{
                        //db.GenomeModels.Add(genomeModel);
                        //db.SaveChanges();

                        //return RedirectToAction("Details", new { id = genomeModel.uuid });
                    //}

                    // There was at least a single error. Show the error and redisplay their data.
                    //else
                    //{
                    //    ViewBag.ConnectionError = "There was an error with the connection to BigDog. The following is the error we encountered: " + error;

                    //    return View(genomeModel);
                    //}
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
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GenomeModel genomeModel = db.GenomeModels.Find(id);
            if (genomeModel == null)
            {
                return HttpNotFound();
            }
            return View(genomeModel);
        }

        // GET: GenomeAssembly/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GenomeModel genomeModel = db.GenomeModels.Find(id);
            if (genomeModel == null)
            {
                return HttpNotFound();
            }
            return View(genomeModel);
        }

        // POST: GenomeAssembly/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(GenomeModel genomeModel)
        {
            if (ModelState.IsValid)
            {
                db.Entry(genomeModel).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(genomeModel);
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
