using System.ComponentModel.DataAnnotations;

namespace Genome.Models
{
    public class GenomeModel
    {
        [Key]
        public int uuid { get; set; }

        [Required]
        [Display(Name = "Masurca K-Mer Value")]
        public int MasurcaKMerValue { get; set; }

        [Required]
        [Display(Name = "Masurca CPU Thread Count")]
        public int MasurcaThreadNum { get; set; }

        [Required]
        [Display(Name = "Masurca Jellyfish hash size (10x size of genome)")]
        public int MasurcaJellyfishHashSize { get; set; }

        [Required]
        [Display(Name = "Masurca Linking Mates (binary)")]
        public int MasurcaLinkingMates { get; set; }

        [Required]
        [Display(Name = "Masurca Limit Jump Coverage")]
        public bool MasurcaLimitJumpCoverage { get; set; }

        [Required]
        [Display(Name = "Masurca Soap Assembly")]
        public bool MasurcaSoapAssembly { get; set; }
    }
}