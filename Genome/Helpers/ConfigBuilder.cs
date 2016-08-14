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

        private string username { get; set; }
        private int seed { get; set; }
        private string urlPath { get; set; }
        private string localPath { get; set; }

        /// <summary>
        /// Generates the config files necessary for the assembler run.
        /// </summary>
        /// <param name="genomeModel">The model data of the current job.</param>
        /// <param name="dataSources">A stored list of strings containing the location(s) of the user's data set.</param>
        /// <param name="seed">Seed value for a unique name for the job.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <param name="initUrl"></param>
        /// <param name="masurcaUrl"></param>
        public void GenerateConfigs(GenomeModel genomeModel, List<string> dataSources, int seed, out string error)
        {
            this.seed = seed;
            username = HttpContext.Current.User.Identity.Name.ToString().Split('@')[0];
            urlPath = "/AssemblerConfigs/" + "Job-" + username + "-" + seed + "/";
            localPath = @"D:\AssemblerConfigs\Job-" + username + "-" + seed + "\\";

            CreateDirectory(seed);
            BuildInitConfig(dataSources, seed, out error);
            if (genomeModel.UseMasurca) { BuildMasurcaConfig(genomeModel, seed, out error); }
        }

        private void CreateDirectory(int seed)
        {
            while (Directory.Exists(localPath))
            {
                Random rand = new Random();
                seed = rand.Next(198, 1248712);
            }

            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);

            else
                throw new IOException("Couldn't create a new directory for the assembler configs for some reason.");
        }

        /// <summary>
        /// Creates the initial file which is run at the beginning of each run. The primary function of which is to download the user's data at runtime.
        /// </summary>
        /// <param name="dataSources">A stored list of strings containing the location(s) of the user's data set.</param>
        /// <param name="seed">Seed value for a unique name for the job.</param>
        /// <param name="error">Any error encountered by the command.</param>
        private void BuildInitConfig(List<string> dataSources, int seed, out string error)
        {
            error = "";

            string fileName = "init_" + seed + ".sh";
            string fullPath = localPath + fileName;

            if (!File.Exists(fullPath))
            {
                try
                {
                    using (TextWriter tw = new StreamWriter(fullPath))
                    {
                        // If we have sequential reads there will be only a single URL:
                        if (dataSources.Count == 1)
                            tw.WriteLine("wget -O sequentialData.fastq " + dataSources[0].ToString());

                        // If we have any other type of reads there will be at least a left and right read:
                        else
                        {
                            List<string> leftReads = new List<string>();
                            List<string> rightReads = new List<string>();
                            string concatFiles = "";

                            // Create the URL lists with the left and right reads split.
                            HelperMethods.CreateUrlLists(dataSources, out leftReads, out rightReads);

                            // Now add the wgets for the left reads URLs and rename them to leftData_[j]:
                            for (int j = 0; j < leftReads.Count; j++)
                            {
                                tw.WriteLine("wget -O leftData_" + j + " " + leftReads[j].ToString());
                                concatFiles = concatFiles + " leftData_" + j;
                            }

                            // Concat the left reads together into a leftReads.fastq file.
                            tw.WriteLine("cat " + concatFiles + " > leftReads.fastq");
                            concatFiles = "";

                            // Now add the wgets for the right reads URLs and rename them to rightData_[i]:
                            for (int i = 0; i < rightReads.Count; i++)
                            {
                                tw.WriteLine("wget -O rightData_" + i + " " + rightReads[i].ToString());
                                concatFiles = concatFiles + " rightData_" + i;
                            }

                            // Concat the right reads together into a rightReads.fastq file.
                            tw.WriteLine("cat " + concatFiles + " > rightReads.fastq");
                        }

                        InitConfigURL = Locations.FTP_URL + urlPath + fileName;
                    }
                }

                catch (Exception e)
                {
                    error = e.Message;
                }
            }

            // We have a problem since the file already exists.
            else
            {
                error = "Unfortunately, we couldn't create the necessary configuration files to submit your job. Please contact an administrator.";

                throw new IOException("Attempted to create \"" + fullPath + "\" but it already exists so we cannot create the file. Continuing is not advised. ");
            }
        }

        /// <summary>
        /// Creates the configuration file for the Masurca assembler for each unique run.
        /// </summary>
        /// <param name="genomeModel">The model data of the current job.</param>
        /// <param name="seed">Seed value for a unique name for the job.</param>
        /// <param name="error">Any error encountered by the command.</param>
        private void BuildMasurcaConfig(GenomeModel genomeModel, int seed, out string error)
        {
            error = "";

            string fileName = "MasurcaConfig_" + seed + ".txt";
            string fullPath = localPath + fileName;

            if (!File.Exists(fullPath))
            {
                try
                {
                    using (TextWriter tw = new StreamWriter(fullPath))
                    {
                        tw.WriteLine("DATA");
                        // Paired-end reads only.
                        if (genomeModel.PEReads)
                            tw.WriteLine("PE= pe " + genomeModel.MasurcaMean + " " + genomeModel.MasurcaStdev + " leftReads.fastq rightReads.fastq");

                        // Jump reads only.
                        else if (genomeModel.JumpReads)
                            tw.WriteLine("JUMP= sh " + genomeModel.MasurcaMean + " " + genomeModel.MasurcaStdev + " leftReads.fastq rightReads.fastq");

                        // Sequential reads only.
                        else if (genomeModel.SequentialReads)
                            tw.WriteLine("PE= pe " + genomeModel.MasurcaMean + " " + genomeModel.MasurcaStdev + " sequentialData.fastq");
                        tw.WriteLine("END");

                        tw.WriteLine("PARAMETERS");
                        if (genomeModel.MasurcaGraphKMerValue == null)
                            tw.WriteLine("GRAPH_KMER_SIZE = auto");

                        else
                            tw.WriteLine("GRAPH_KMER_SIZE = " + genomeModel.MasurcaGraphKMerValue);

                        tw.WriteLine("USE_LINKING_MATES = " + Convert.ToInt32(genomeModel.MasurcaLinkingMates));

                        // For bacteria
                        if (genomeModel.MasurcaLimitJumpCoverage)
                            tw.WriteLine("LIMIT_JUMP_COVERAGE = 60");

                        // For other organisms
                        else
                            tw.WriteLine("LIMIT_JUMP_COVERAGE = 300");

                        if (genomeModel.MasurcaCAParameters)
                            tw.WriteLine("CA_PARAMETERS = cgwErrorRate=0.25 ovlMemory=4GB");

                        else
                            tw.WriteLine("CA_PARAMETERS = cgwErrorRate=0.15 ovlMemory=4GB");

                        // Minimum count k-mers used in error correction 1 means all k-mers are used.  one can increase to 2 if coverage >100
                        tw.WriteLine("KMER_COUNT_THRESHOLD = " + genomeModel.MasurcaKMerErrorCount);
                        tw.WriteLine("NUM_THREADS = " + genomeModel.MasurcaThreadNum);
                        tw.WriteLine("JF_SIZE = " + genomeModel.MasurcaJellyfishHashSize); // Should be estimated_genome_size * estimated_coverage.

                        if (genomeModel.HomoTrim)
                            tw.WriteLine("DO_HOMOPOLYMER_TRIM = 1");

                        else
                            tw.WriteLine("DO_HOMOPOLYMER_TRIM = 0");
                        tw.WriteLine("END");

                        MasurcaConfigURL = Locations.FTP_URL + urlPath + fileName;
                    }
                }

                catch(Exception e)
                {
                    error = e.Message;
                }
            }

            // We have a problem since the file already exists.
            else
            {
                error = "Unfortunately, we couldn't create the necessary configuration files to submit your job. Please contact an administrator.";

                throw new IOException("Attempted to create \"" + fullPath + "\" but it already exists so we cannot create the file. Continuing is not advised. ");
            }
        }
    }
}