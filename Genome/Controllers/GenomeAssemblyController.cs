﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Genome.Models;
using Genome.Helpers;

namespace Genome.Controllers
{
    public class GenomeAssemblyController : Controller
    {
        private GenomeAssemblyDbContext db = new GenomeAssemblyDbContext();

        public ActionResult Create(int? id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(GenomeModel genomeModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    genomeModel.CreatedBy = User.Identity.Name;
                    genomeModel.CreatedDate = DateTime.Now;
                    genomeModel.JobStatus = "Pending";

                    db.GenomeModels.Add(genomeModel);
                    db.SaveChanges();

                    // ssh
                    //SSHConfig ssh = new SSHConfig("", "pw", "login-0-0.research.siu.edu");
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
