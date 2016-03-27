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
                    // Example if they leave things blank.
                    if(genomeModel.JumpLength == null)
                    {
                        // Set to a default length
                    }

                    if (genomeModel.PairedEndLength == null)
                    {
                        // Set to a default size
                    }

                    if (genomeModel.MasurcaJellyfishHashSize == null)
                    {
                        // Set to a default size
                    }

                    if(genomeModel.MasurcaGraphKMerValue == null)
                        // null means set to auto in the script. so GRAPH_KMER_SIZE = auto.

                    if(genomeModel.MasurcaThreadNum == null)
                        genomeModel.MasurcaThreadNum = 20;


                    genomeModel.CreatedBy = User.Identity.Name;
                    genomeModel.CreatedDate = DateTime.Now;
                    genomeModel.JobStatus = "Pending";

                    db.GenomeModels.Add(genomeModel);
                    db.SaveChanges();

                    SSHConfig ssh = new SSHConfig("Big Dog IP", genomeModel.SSHUser, genomeModel.SSHPass);
                }

                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                return RedirectToAction("Confirmation");
            }
            // SSHConfig ssh = new SSHConfig("login-0-0.research.siu.edu", "", "");
            return View(genomeModel);
            //return View();
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
