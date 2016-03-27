using Genome.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Genome.Helpers
{
    public class ConfigBuilder
    {
        //private string BuildMasurcaConfig(int jobNumber, GenomeModel genomeModel, string dataLocation1, string dataLocation2 = "")
        //{
        //    string path = @"~/AssemberConfigs/Job" + jobNumber;
        //    Directory.CreateDirectory(path);
        //    string fileName = "MasurcaConfig.txt";
        //    string fullPath = path + fileName;

        //    if (!File.Exists(fullPath))
        //    {
        //        File.Create(fullPath);
        //        TextWriter tw = new StreamWriter(fullPath);
        //        //tw.WriteLine("The very first line!");


        //        tw.WriteLine("DATA");
        //        //data params
        //        if (genomeModel.Step2.PEReads)
        //            tw.WriteLine("PE= pe " + genomeModel.Step2.PairedEndLength + " 20  " + dataLocation1 + "  " + dataLocation2);

        //        // These locations are not the same, used for now for development purposes
        //        if (genomeModel.Step2.JumpReads)
        //            tw.WriteLine("JUMP= sh " + genomeModel.Step2.JumpLength + " 200  " + dataLocation1 + "  " + dataLocation2);

        //        tw.WriteLine("END");


        //        tw.WriteLine("PARAMETERS");
        //        // Parameter params
        //        tw.WriteLine("GRAPH_KMER_SIZE = auto");
        //        tw.WriteLine("USE_LINKING_MATES = " + Convert.ToInt32(genomeModel.Step3.MasurcaLinkingMates));
        //        tw.WriteLine("LIMIT_JUMP_COVERAGE = " + genomeModel.Step3.MasurcaLimitJumpCoverage);

        //        if (genomeModel.Step3.MasurcaCAParameters)
        //            tw.WriteLine("CA_PARAMETERS = cgwErrorRate=0.25 ovlMemory=4GB");

        //        else
        //            tw.WriteLine("CA_PARAMETERS = cgwErrorRate=0.15 ovlMemory=4GB");

        //        // Minimum count k-mers used in error correction 1 means all k-mers are used.  one can increase to 2 if coverage >100
        //        tw.WriteLine("KMER_COUNT_THRESHOLD = 1");
        //        tw.WriteLine("NUM_THREADS = " + genomeModel.Step3.MasurcaThreadNum);
        //        // this is mandatory jellyfish hash size -- a safe value is estimated_genome_size*estimated_coverage
        //        tw.WriteLine("JF_SIZE = " + genomeModel.Step3.MasurcaJellyfishHashSize);
        //        tw.WriteLine("DO_HOMOPOLYMER_TRIM = " + genomeModel.Step3.HomoTrim);
        //        tw.WriteLine("END");

        //        tw.Close();
        //    }

        //    return fullPath;
        //}

        public void InitConfig(GenomeModel genomeModel, List<string> dataSource)
        {
            string path = @"~/AssemberConfigs/Job" + genomeModel.uuid + "/";
            Directory.CreateDirectory(path);
            string fileName = "init_config_" + genomeModel.uuid + ".txt";
            string fullPath = path + fileName;

            if (!File.Exists(fullPath))
            {
                File.Create(fullPath);
                TextWriter tw = new StreamWriter(fullPath);

                tw.WriteLine("cd " + "WORKING DIRECTORY/Data"); // Change directory to working directory

                foreach (string url in dataSource)
                {
                    tw.WriteLine("wget " + url.ToString());
                    // Error check wget to make sure it completed.

                    // If(wget.ExitStatus == 0) { We stop the wgets and write to a file saying there is an error. }
                    // Here we would check the wget error code to see if the download completed successfully (0) or it didn't.


                }


                // Next step is to do wget error checking. 


                //tw.WriteLine("The very first line!");
            }
        }
    }
}