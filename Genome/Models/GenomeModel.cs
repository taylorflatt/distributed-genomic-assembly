using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Genome.Models
{
    [Serializable]
    public class GenomeModel
    {
        [Key]
        public int uuid { get; set; }

        // The job ID that is seen in the scheduler of BigDog so we can use this to check for updates.
        public int SGEJobId { get; set; }

        [NotMapped]
        [Display(Name = "Big Dog Username:")]
        public string SSHUser { get; set; }

        [NotMapped]
        [Display(Name = "Big Dog Password:")]
        public string SSHPass { get; set; }

        //////////////////////////////
        // Data specific parameters //
        /////////////////////////////

        // IF the data is PE or Jump then we will only have two files...otherwise n-files.
        [Required]
        [DefaultValue("")]
        [Display(Name = "Sequenced Data Location")]
        public string DataSource { get; set; }

        [Display(Name = "Primers")]
        public bool Primers { get; set; }

        [Display(Name = "PE Reads")]
        public bool PEReads { get; set; }

        [Display(Name = "Paired End Length")]
        public int? PairedEndLength { get; set; }

        [Display(Name = "Jump Reads")]
        public bool JumpReads { get; set; }

        [Display(Name = "Jump Length")]
        public int? JumpLength { get; set; }

        /////////////////////////////////
        // Masurca specific parameters //
        /////////////////////////////////
        [Display(Name = "Masurca Assembler")]
        public bool UseMasurca { get; set; }

        [Display(Name = "Masurca Current Status")]
        public string MasurcaStatus { get; set; }

        [Display(Name = "Masurca Current Step")]
        [DefaultValue(1)]
        public int MasurcaCurrentStep { get; set; }

        [Display(Name = "Masurca PE Mean")]
        public int? MasurcaPEMean { get; set; }

        [Display(Name = "Masurca PE Standard Deviation")]
        public int? MasurcaPEStdev { get; set; }

        [Display(Name = "Masurca Graph K-Mer Value")]
        [Range(25, 101)]
        public int? MasurcaGraphKMerValue { get; set; }

        // Set this to 1 for Illumina-only assemblies and to 0 if you have 1x or more long (Sanger, 454) reads, you can also set this to 0 for large data sets with high jumping clone coverage, e.g. >50x
        [Display(Name = "Illumina Data Only?")]
        public bool MasurcaLinkingMates { get; set; }

        [Display(Name = "Masurca Limit Jump Coverage")]
        public bool MasurcaLimitJumpCoverage { get; set; }

        // Set cgwErrorRate=0.25 for bacteria and 0.1<=cgwErrorRate<=0.15 for other organisms
        [Display(Name = "Bacteria?")]
        public bool MasurcaCAParameters { get; set; }

        [Display(Name = "K-Mer count Threshold")]
        [Range(1, 2)]
        public int? MasurcaKMerErrorCount { get; set; }

        [Display(Name = "Masurca CPU Thread Count")]
        [Range(1,20)]
        public int? MasurcaThreadNum { get; set; }

        [Required]
        [Display(Name = "Masurca Jellyfish hash size (10x size of genome)")]
        public int MasurcaJellyfishHashSize { get; set; }

        // Must return 1 for true, or 0 for false
        [Display(Name = "Homoplymer Trim")]
        public bool HomoTrim { get; set; }

        /////////////////////////////////
        // SGA specific parameters     //
        /////////////////////////////////
        [Display(Name ="SGA Assembler")]
        public bool UseSGA { get; set; }

        /////////////////////////////////
        // WGS specific parameters     //
        /////////////////////////////////
        [Display(Name = "WGS Assembler")]
        public bool UseWGS { get; set; }

        ///////////////////////////////
        // Misc specific parameters //
        /////////////////////////////

        [Required]
        [Display(Name = "I Agree")]
        public bool Agreed { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:MM/dd/yy HH:mm}")]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:MM/dd/yy HH:mm}")]
        [Display(Name = "Completed Date")]
        public DateTime? CompletedDate { get; set; }

        [Display(Name = "Current Status")]
        public string OverallStatus { get; set; }

        [Display(Name = "Current Step")]
        [DefaultValue(1)]
        public int OverallCurrentStep { get; set; }

        [DefaultValue("")]
        [Display(Name = "DownloadLink")]
        public string DownloadLink { get; set; }
    }
}