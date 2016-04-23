using Genome.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace Genome.Helpers
{
    public class ConfigBuilder
    {
        public string MasurcaConfigURL { get; set; }
        public string InitConfigURL { get; set; }
        public string SchedulerConfigURL { get; set; }

        public string BuildMasurcaConfig(GenomeModel genomeModel, string[] dataSource)
        {
            string urlPath = "AssemblerConfigs/" + "Job" + genomeModel.uuid + "/";
            string path = AppDomain.CurrentDomain.BaseDirectory + "AssemblerConfigs\\" + "Job" + genomeModel.uuid + "\\";
            Directory.CreateDirectory(path);
            string fileName = "MasurcaConfig_" + genomeModel.uuid + ".txt";
            string fullPath = path + fileName;

            if (!File.Exists(fullPath))
            {
                TextWriter tw = new StreamWriter(fullPath);

                tw.WriteLine("DATA");
                string dataString = "";
                int counter = 0;
                foreach (string url in dataSource)
                {
                    dataString = dataString + "Data_" + counter + ".fastq";
                }
                //data params
                if (genomeModel.PEReads)
                {
                    tw.WriteLine("PE= pe " + genomeModel.PairedEndLength + " 20  " + dataString);
                }

                // These locations are not the same, used for now for development purposes
                else if (genomeModel.JumpReads)
                {
                    tw.WriteLine("JUMP= sh " + genomeModel.JumpLength + " 200  " + dataString);
                }

                tw.WriteLine("END");


                tw.WriteLine("PARAMETERS");
                // Parameter params
                tw.WriteLine("GRAPH_KMER_SIZE = auto");
                tw.WriteLine("USE_LINKING_MATES = " + Convert.ToInt32(genomeModel.MasurcaLinkingMates));

                if (genomeModel.MasurcaLimitJumpCoverage)
                    tw.WriteLine("LIMIT_JUMP_COVERAGE = 60");
                else
                    tw.WriteLine("LIMIT_JUMP_COVERAGE = 300");

                if (genomeModel.MasurcaCAParameters)
                    tw.WriteLine("CA_PARAMETERS = cgwErrorRate=0.25 ovlMemory=4GB");

                else
                    tw.WriteLine("CA_PARAMETERS = cgwErrorRate=0.15 ovlMemory=4GB");

                // Minimum count k-mers used in error correction 1 means all k-mers are used.  one can increase to 2 if coverage >100
                tw.WriteLine("KMER_COUNT_THRESHOLD = " + genomeModel.MasurcaKMerErrorCount);
                tw.WriteLine("NUM_THREADS = " + genomeModel.MasurcaThreadNum);
                // this is mandatory jellyfish hash size -- a safe value is estimated_genome_size*estimated_coverage
                tw.WriteLine("JF_SIZE = " + genomeModel.MasurcaJellyfishHashSize);
                tw.WriteLine("DO_HOMOPOLYMER_TRIM = " + genomeModel.HomoTrim);
                tw.WriteLine("END");

                tw.Close();
            }

            MasurcaConfigURL = "http://" + HttpContext.Current.Request.Url.Authority.ToString() + "/" + urlPath + fileName;
            return MasurcaConfigURL;
        }

        public string BuildInitConfig(GenomeModel genomeModel, string[] dataSource)
        {
            string urlPath = "AssemblerConfigs/" + "Job" + genomeModel.uuid + "/";
            string path = AppDomain.CurrentDomain.BaseDirectory + "AssemblerConfigs\\" + "Job" + genomeModel.uuid + "\\";
            Directory.CreateDirectory(path);
            string fileName = "init_" + genomeModel.uuid + ".sh";
            string fullPath = path + fileName;
            if (!File.Exists(fullPath))
            {
                TextWriter tw = new StreamWriter(fullPath);

                //tw.WriteLine("cd " + "WORKING DIRECTORY/Data"); // Change directory to working directory
                //tw
                foreach (string url in dataSource)
                {
                    tw.WriteLine("wget " + url.ToString());
                    // Error check wget to make sure it completed.

                    // If(wget.ExitStatus == 0) { We stop the wgets and write to a file saying there is an error. }
                    // Here we would check the wget error code to see if the download completed successfully (0) or it didn't.


                }
                tw.Close();

                // Next step is to do wget error checking. 

            }
            InitConfigURL = "http://" + HttpContext.Current.Request.Url.Authority.ToString() + "/" + urlPath + fileName;
            return InitConfigURL;
        }

        public string BuildSchedulerConfig(GenomeModel genomeModel)
        {
            string urlPath = "AssemblerConfigs/" + "Job" + genomeModel.uuid + "/";
            string path = AppDomain.CurrentDomain.BaseDirectory + "AssemblerConfigs\\" + "Job" + genomeModel.uuid + "\\";
            Directory.CreateDirectory(path);
            string fileName = "Scheduler_" + genomeModel.uuid + ".sh";
            string fullPath = path + fileName;

            if (!File.Exists(fullPath))
            {
                TextWriter tw = new StreamWriter(fullPath);

                tw.WriteLine("Test Line");

                tw.Close();
            }
            SchedulerConfigURL = "http://" + HttpContext.Current.Request.Url.Authority.ToString() + "/" + urlPath + fileName;
            return SchedulerConfigURL;
        }
    }
}