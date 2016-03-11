using Genome.Models;
using System;
using System.IO;

namespace Genome.Helpers
{
    public class ConfigBuilder
    {
        private string BuildMasurcaConfig(int jobNumber, GenomeModel genomeModel, string dataLocation1, string dataLocation2 = "")
        {
            string path = @"~/AssemberConfigs/Job" + jobNumber;
            Directory.CreateDirectory(path);
            string fileName = "MasurcaConfig.txt";
            string fullPath = path + fileName;

            if (!File.Exists(fullPath))
            {
                File.Create(fullPath);
                TextWriter tw = new StreamWriter(fullPath);
                //tw.WriteLine("The very first line!");


                tw.WriteLine("DATA");
                //data params
                if (genomeModel.PEReads)
                    tw.WriteLine("PE= pe " + genomeModel.PairedEndLength + " 20  " + dataLocation1 + "  " + dataLocation2);

                // These locations are not the same, used for now for development purposes
                if (genomeModel.JumpReads)
                    tw.WriteLine("JUMP= sh " + genomeModel.JumpLength + " 200  " + dataLocation1 + "  " + dataLocation2);

                tw.WriteLine("END");


                tw.WriteLine("PARAMETERS");
                // Parameter params
                tw.WriteLine("GRAPH_KMER_SIZE = auto");
                tw.WriteLine("USE_LINKING_MATES = " + Convert.ToInt32(genomeModel.MasurcaLinkingMates));
                tw.WriteLine("LIMIT_JUMP_COVERAGE = " + genomeModel.MasurcaLimitJumpCoverage);

                if (genomeModel.MasurcaCAParameters)
                    tw.WriteLine("CA_PARAMETERS = cgwErrorRate=0.25 ovlMemory=4GB");

                else
                    tw.WriteLine("CA_PARAMETERS = cgwErrorRate=0.15 ovlMemory=4GB");

                // Minimum count k-mers used in error correction 1 means all k-mers are used.  one can increase to 2 if coverage >100
                tw.WriteLine("KMER_COUNT_THRESHOLD = 1");
                tw.WriteLine("NUM_THREADS = " + genomeModel.MasurcaThreadNum);
                // this is mandatory jellyfish hash size -- a safe value is estimated_genome_size*estimated_coverage
                tw.WriteLine("JF_SIZE = " + genomeModel.MasurcaJellyfishHashSize);
                tw.WriteLine("DO_HOMOPOLYMER_TRIM = " + genomeModel.HomoTrim);
                tw.WriteLine("END");

                tw.Close();                
            }

            return fullPath;
        }
    }
}