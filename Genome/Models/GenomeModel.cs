﻿using System;
using System.ComponentModel.DataAnnotations;

namespace Genome.Models
{
    [Serializable]
    public class GenomeModel
    {
        [Key]
        public int uuid { get; set; }

        //[Display(Name = "Start Page")]
        //public GenomeAssemblyStep1 Step1 { get; set; }

        //[Display(Name = "Initial Data")]
        //public GenomeAssemblyStep2 Step2 { get; set; }

        //[Display(Name = "Masurca Assembler")]
        //public GenomeAssemblyStep3 Step3 { get; set; }

        //[Display(Name = "Confirm Data")]
        //public GenomeAssemblyStep4 Step4 { get; set; }

        //[Display(Name = "Send Data")]
        //public GenomeAssemblyStep5 Step5 { get; set; }

        [Required]
        [Display(Name = "Accept")]
        public bool AcceptInstructions { get; set; }

        [Required]
        [Display(Name = "Sequenced Data Location")]
        public string DataSource { get; set; }

        [Required]
        [Display(Name = "Primers")]
        public bool Primers { get; set; }

        [Required]
        [Display(Name = "Jump Reads")]
        public bool JumpReads { get; set; }

        [Required]
        [Display(Name = "PE Reads")]
        public bool PEReads { get; set; }

        [Display(Name = "Paired End Length")]
        public int PairedEndLength { get; set; }

        [Display(Name = "Jump Length")]
        public int JumpLength { get; set; }

        // Masurca specific parameters
        [Required]
        [Display(Name = "Masurca K-Mer Value")]
        public int MasurcaKMerValue { get; set; }

        [Required]
        [Display(Name = "Masurca CPU Thread Count")]
        public int MasurcaThreadNum { get; set; }

        [Required]
        [Display(Name = "Masurca Jellyfish hash size (10x size of genome)")]
        public int MasurcaJellyfishHashSize { get; set; }

        // Set this to 1 for Illumina-only assemblies and to 0 if you have 1x or more long (Sanger, 454) reads, you can also set this to 0 for large data sets with high jumping clone coverage, e.g. >50x
        [Required]
        [Display(Name = "Illumina Data Only?")]
        public bool MasurcaLinkingMates { get; set; }

        [Required]
        [Display(Name = "Masurca Limit Jump Coverage")]
        public bool MasurcaLimitJumpCoverage { get; set; }

        // Set cgwErrorRate=0.25 for bacteria and 0.1<=cgwErrorRate<=0.15 for other organisms
        [Required]
        [Display(Name = "Bacteria?")]
        public bool MasurcaCAParameters { get; set; }

        // Must return 1 for true, or 0 for false
        [Required]
        [Display(Name = "Homoplymer Trim")]
        public bool HomoTrim { get; set; }

        [Required]
        public bool Agreed { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string JobStatus { get; set; }
    }

    //// Instructions
    //[Serializable]
    //public class GenomeAssemblyStep1
    //{

    //}

    //// Initial Data
    //[Serializable]
    //public class GenomeAssemblyStep2
    //{


    //}

    //// Masuca Assembly
    //[Serializable]
    //public class GenomeAssemblyStep3
    //{
 
    //}

    ////Change the steps 4 and 5 to something more static so when we add more assemblers it will be easier to add them to the prog.

    //// Confirm Data
    //[Serializable]
    //public class GenomeAssemblyStep4
    //{

    //}

    //// Send data (ssh).
    //[Serializable]
    //public class GenomeAssemblyStep5
    //{

    //}
}