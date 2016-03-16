using MVC.Wizard.ViewModels;
using MVC.Wizard.Web.ViewModels;
using System;
using System.ComponentModel.DataAnnotations;

namespace Genome.Models
{
    public class GenomeModel : WizardViewModel
    {
        private const int numberOfSteps = 5;

        public GenomeModel()
            : base(numberOfSteps)
        {
            Step1 = new GenomeAssemblyStep1();
            Step2 = new GenomeAssemblyStep2();
            Step3 = new GenomeAssemblyStep3();
            Step4 = new GenomeAssemblyStep4();
            Step5 = new GenomeAssemblyStep5();
        }

        public GenomeModel(int uuid)
            : this()
        {
            Step1.uuid = uuid;
        }

        [Display(Name = "Start Page")]
        public GenomeAssemblyStep1 Step1 { get; set; }

        [Display(Name = "Initial Data")]
        public GenomeAssemblyStep2 Step2 { get; set; }

        [Display(Name = "Masurca Assembler")]
        public GenomeAssemblyStep3 Step3 { get; set; }

        [Display(Name = "Confirm Data")]
        public GenomeAssemblyStep4 Step4 { get; set; }

        [Display(Name = "Send Data")]
        public GenomeAssemblyStep5 Step5 { get; set; }

    }

    // Instructions
    public class GenomeAssemblyStep1 : WizardStep
    {
        [Key]
        public int uuid { get; set; }
    }

    // Initial Data
    public class GenomeAssemblyStep2 : WizardStep
    {
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

    }

    // Masuca Assembly
    public class GenomeAssemblyStep3 : WizardStep
    {
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

    }

    // Confirm Data
    public class GenomeAssemblyStep4 : WizardStep
    {
        [Required]
        public bool Agreed { get; set; }
    }

    // Send data (ssh).
    public class GenomeAssemblyStep5 : WizardStep
    {
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}