using System;
using System.ComponentModel.DataAnnotations;

namespace Genome.Models
{
    [Serializable]
    public class GenomeModel
    {
        [Key]
        public int uuid { get; set; }

        [Required]
        [Display(Name = "Sequenced Data Location")]
        public string DataSource { get; set; }

        [Display(Name = "Primers")]
        public bool Primers { get; set; }

        [Display(Name = "Jump Reads")]
        public bool JumpReads { get; set; }

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

        [Display(Name = "Masurca CPU Thread Count")]
        public int MasurcaThreadNum { get; set; }

        [Display(Name = "Masurca Jellyfish hash size (10x size of genome)")]
        public int MasurcaJellyfishHashSize { get; set; }

        // Set this to 1 for Illumina-only assemblies and to 0 if you have 1x or more long (Sanger, 454) reads, you can also set this to 0 for large data sets with high jumping clone coverage, e.g. >50x
        [Display(Name = "Illumina Data Only?")]
        public bool MasurcaLinkingMates { get; set; }

        [Display(Name = "Masurca Limit Jump Coverage")]
        public bool MasurcaLimitJumpCoverage { get; set; }

        // Set cgwErrorRate=0.25 for bacteria and 0.1<=cgwErrorRate<=0.15 for other organisms
        [Display(Name = "Bacteria?")]
        public bool MasurcaCAParameters { get; set; }

        // Must return 1 for true, or 0 for false
        [Display(Name = "Homoplymer Trim")]
        public bool HomoTrim { get; set; }

        [Required]
        [Display(Name = "I Agree")]
        public bool Agreed { get; set; }

        public string CreatedBy { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:MM/dd/yy HH:mm}")]
        public DateTime CreatedDate { get; set; }

        public string JobStatus { get; set; }
    }
}