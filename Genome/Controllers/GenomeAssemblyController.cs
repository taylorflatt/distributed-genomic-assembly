using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Genome.Models;
using MVC.Wizard.Controllers;
using MVC.Wizard.Web.ViewModels;

namespace Genome.Controllers
{
    public class GenomeAssemblyController : WizardController
    {
        private GenomeJobDbContext db = new GenomeJobDbContext();

        public ActionResult Index()
        {
            InitializeWizard();
            var ViewModel = new GenomeModel();

            return View(ViewModel);
        }

        protected void ProcessToNext(GenomeModel model)
        {
            // Do here some custom things if you navigate to the next step.

            if (model.GetType() == typeof(SampleWizardViewModel))
            {
                // Check the type so you could use multiple wizards in one controller.
            }
        }

        protected void ProcessToPrevious(GenomeModel model)
        {
            // Do here some custom things if you navigate to the next step.

            if (model.GetType() == typeof(SampleWizardViewModel))
            {
                // Check the type so you could use multiple wizards in one controller.
            }
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
        public ActionResult Edit([Bind(Include = "uuid,Primers,JumpReads,PEReads,PairedEndLength,JumpLength,CreatedBy,MasurcaKMerValue,MasurcaThreadNum,MasurcaJellyfishHashSize,MasurcaLinkingMates,MasurcaLimitJumpCoverage,MasurcaCAParameters,HomoTrim")] GenomeModel genomeModel)
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
