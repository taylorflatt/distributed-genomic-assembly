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
        private GenomeAssemblyDbContext db = new GenomeAssemblyDbContext();

        //public ActionResult Index()
        //{
        //    if (Session["Job"] != null)
        //    {
        //        GenomeModel wizard = (GenomeModel)Session["Job"];

        //        return View(wizard.Step1);
        //    }

        //    return View();
        //}

        //[HttpPost]
        //public ActionResult Index(GenomeAssemblyStep1 step1)
        //{
        //    if(ModelState.IsValid)
        //    {
        //        // Create a new object that will contain all of the session details and set the step to 1.
        //        GenomeModel wizard = new GenomeModel();
        //        wizard.Step1 = step1;

        //        // Store the wizard information in the session.
        //        Session["Job"] = wizard;

        //        // Move to step 2.
        //        return RedirectToAction("Step2");
        //    }

        //    // If we reached this point, there is a problem!
        //    return View(step1);
        //}

        //// Get
        //public ActionResult Step2()
        //{
        //    if (Session["Job"] != null)
        //    {
        //        GenomeModel wizard = (GenomeModel)Session["Job"];

        //        return View(wizard.Step2);
        //    }

        //    return View();
        //}

        //[HttpPost]
        //public ActionResult Step2(GenomeAssemblyStep2 step2)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Get the session details from previous steps.
        //        GenomeModel wizard = (GenomeModel)Session["Job"];
        //        wizard.Step2 = step2;

        //        // Store the wizard information in the session.
        //        Session["Job"] = wizard;

        //        // Move to step 3.
        //        return RedirectToAction("Step3");
        //    }

        //    // If we reached this point, there is a problem!
        //    return View(step2);
        //}

        //// GET: 
        //public ActionResult Step3()
        //{
        //    if (Session["Job"] != null)
        //    {
        //        GenomeModel wizard = (GenomeModel)Session["Job"];

        //        return View(wizard.Step3);
        //    }

        //    return View();
        //}

        //[HttpPost]
        //public ActionResult Step3(GenomeAssemblyStep3 step3)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Get the session details from previous steps.
        //        GenomeModel wizard = (GenomeModel)Session["Job"];
        //        wizard.Step3 = step3;

        //        // Store the wizard information in the session.
        //        Session["Job"] = wizard;

        //        // Move to step 3.
        //        return RedirectToAction("Step4");
        //    }

        //    // If we reached this point, there is a problem!
        //    return View(step3);
        //}

        //// GET: 
        //public ActionResult Step4()
        //{
        //    if (Session["Job"] != null)
        //    {
        //        GenomeModel wizard = (GenomeModel)Session["Job"];

        //        return View(wizard.Step4);
        //    }

        //    return View();
        //}

        //[HttpPost]
        //public ActionResult Step4(GenomeAssemblyStep4 step4)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Get the session details from previous steps.
        //        GenomeModel wizard = (GenomeModel)Session["Job"];
        //        wizard.Step4 = step4;

        //        // Store the wizard information in the session.
        //        Session["Job"] = wizard;

        //        // Move to step 3.
        //        return RedirectToAction("Step5");
        //    }

        //    // If we reached this point, there is a problem!
        //    return View(step4);
        //}

        //// GET:
        //public ActionResult Step5()
        //{
        //    GenomeModel wizard = (GenomeModel)Session["Job"];

        //    return View(wizard.Step5);
        //}

        //[HttpPost]
        //public ActionResult Step5(GenomeAssemblyStep5 step5)
        //{
        //    if(ModelState.IsValid)
        //    {
        //        GenomeModel wizard = (GenomeModel)Session["Job"];

        //        wizard.Step5 = step5;

        //        // Now we can save the data to the database.
        //        GenomeModel job = new GenomeModel();

        //        // Step 1 Data:
        //        job.Step1.AcceptInstructions = wizard.Step1.AcceptInstructions;

        //        // Step 2 Data:
        //        job.Step2.DataSource = wizard.Step2.DataSource;
        //        job.Step2.JumpLength = wizard.Step2.JumpLength;
        //        job.Step2.JumpReads = wizard.Step2.JumpReads;
        //        job.Step2.PairedEndLength = wizard.Step2.PairedEndLength;
        //        job.Step2.PEReads = wizard.Step2.PEReads;
        //        job.Step2.Primers = wizard.Step2.Primers;

        //        /* Assembler Data */
        //        // Step 3 Data:
        //        job.Step3.HomoTrim = wizard.Step3.HomoTrim;
        //        job.Step3.MasurcaCAParameters = wizard.Step3.MasurcaCAParameters;
        //        job.Step3.MasurcaJellyfishHashSize = wizard.Step3.MasurcaJellyfishHashSize;
        //        job.Step3.MasurcaKMerValue = wizard.Step3.MasurcaKMerValue;
        //        job.Step3.MasurcaLimitJumpCoverage = wizard.Step3.MasurcaLimitJumpCoverage;
        //        job.Step3.MasurcaLinkingMates = wizard.Step3.MasurcaLinkingMates;
        //        job.Step3.MasurcaThreadNum = wizard.Step3.MasurcaThreadNum;
        //        /*End Assembler Data */
        //        // Step 4 Data:
        //        job.Step4.Agreed = wizard.Step4.Agreed;

        //        // Step 5 Data:
        //        job.Step5.CreatedBy = User.Identity.Name;
        //        job.Step5.CreatedDate = DateTime.Now;
        //        job.Step5.JobStatus = "Pending";

        //        db.GenomeModels.Add(job);
        //        db.SaveChanges();

        //        return RedirectToAction("Confirmation", "Shared");
        //    }

        //    return View(step5);
        //}

        public ActionResult Create(int? id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create()
        {
            return View();
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
