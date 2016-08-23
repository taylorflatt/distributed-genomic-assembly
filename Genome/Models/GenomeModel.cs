using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// TODO: Need to consider changing the model to pass the assemblers to different tables rather than having 1 large table with all the data. I'm thinking of 
/// spinning off the assemblers into their own models and then adding a navigation property into the genomeModel entity. This will be done but in the second 
/// release of the program. Leaving this comment here for posterity.
namespace Genome.Models
{
    [Serializable]
    public class GenomeModel
    {
        [Key]
        [DisplayName("UUID")]
        public int uuid { get; set; }

        /// <summary>
        /// This is different from the UUID. Unfortunately we cannot access UUID at creation of a job to know a unique value to 
        /// give our job. So instead we must create a random one and use THAT throughout the system.
        /// </summary>
        [DisplayName("Seed")]
        [Index(IsUnique = true)]
        public int Seed { get; set; }

        // The job ID that is seen in the scheduler of BigDog so we can use this to check for updates.
        public int SGEJobId { get; set; }

        [NotMapped]
        [Display(Name = "Big Dog Username:")]
        public string SSHUser { get; set; }

        [NotMapped]
        [Display(Name = "Big Dog Password:")]
        public string SSHPass { get; set; }

        // Add this throughout the program and set error to this. Then the step if error is encountered is either the step it
        // happened on or the last step (end the job completely).
        [Display(Name = "Error")]
        public string JobError { get; set; }

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

        [Display(Name = "Sequential Reads")]
        public bool SequentialReads { get; set; }

        [Display(Name = "Sequential Length")]
        public int? SequentialLength { get; set; }

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

        [Display(Name = "Masurca Mean")]
        public int? MasurcaMean { get; set; }

        [Display(Name = "Masurca Standard Deviation")]
        public int? MasurcaStdev { get; set; }

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
        [Range(1, 20)]
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
        [Display(Name = "SGA Assembler")]
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

        [Display(Name = "Number of Assemblers Choosen")]
        public int NumberOfAssemblers { get; set; }

        [Display(Name = "Current Status")]
        public string OverallStatus { get; set; }

        [Display(Name = "Current Step")]
        [DefaultValue(1)]
        public int OverallCurrentStep { get; set; }

        // WE NEED TO RESET THIS VALUE TO NULL WHEN THE DOWNLOAD IS EVENTUALLY PRUNED.
        [DefaultValue("")]
        [Display(Name = "DownloadLink")]
        public string DownloadLink { get; set; }
    }
}