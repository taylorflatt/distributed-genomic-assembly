using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Genome.Models;
using Microsoft.AspNet.Identity;

namespace Genome.Controllers
{
    public class GenomeJobController : Controller
    {
        private GenomeJobDbContext db = new GenomeJobDbContext();

        // GET: GenomeJob
        /* Here we want to return the first step of the wizard whilst running a check
            to make sure that the user only has one job processing or in queue. They 
            can only have one job at a time.
        */
        public ActionResult Index()
        {
            return View("Index");
        }

        // GET: GenomeJob/Details/5
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

        // GET: GenomeJob/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: GenomeJob/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "uuid,MasurcaKMerValue,MasurcaThreadNum,MasurcaJellyfishHashSize,MasurcaLinkingMates,MasurcaLimitJumpCoverage,MasurcaSoapAssembly")] GenomeModel genomeModel)
        {
            if (ModelState.IsValid)
            {
                db.GenomeModels.Add(genomeModel);
                db.SaveChanges();

                //This is passed into the sshconfig thread to create the directory //share/genomeassembly/user/job#
                int jobNumber = db.GenomeModels.Where(j => j.CreatedBy.Equals(User.Identity.Name)).Count() + 1; 
                return RedirectToAction("Index");
            }

            return View(genomeModel);
        }

        // GET: GenomeJob/Edit/5
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

        // POST: GenomeJob/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "uuid,MasurcaKMerValue,MasurcaThreadNum,MasurcaJellyfishHashSize,MasurcaLinkingMates,MasurcaLimitJumpCoverage,MasurcaSoapAssembly")] GenomeModel genomeModel)
        {
            if (ModelState.IsValid)
            {
                db.Entry(genomeModel).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(genomeModel);
        }

        // GET: GenomeJob/Delete/5
        public ActionResult Delete(int? id)
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

        // POST: GenomeJob/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            GenomeModel genomeModel = db.GenomeModels.Find(id);
            db.GenomeModels.Remove(genomeModel);
            db.SaveChanges();
            return RedirectToAction("Index");
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
