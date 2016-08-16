using Genome.Models;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace Genome.Helpers
{
    public class JobBuilder
    {
        public string MasurcaConfigURL { get; set; }
        public string InitConfigURL { get; set; }
        public string SchedulerConfigURL { get; set; }

        private string username { get; set; }
        public int seed { get; set; }
        private string urlPath { get; set; }
        private string localPath { get; set; }

        private GenomeModel genomeModel;
        private List<string> dataSources;

        /// <param name="genomeModel">The model data of the current job.</param>
        /// <param name="dataSources">A stored list of strings containing the location(s) of the user's data set.</param>
        /// <param name="seed">Seed value for a unique name for the job.</param>
        public JobBuilder(GenomeModel genomeModel, List<string> dataSources, int seed)
        {
            this.genomeModel = genomeModel;
            this.seed = seed;
            this.dataSources = dataSources;
        }

        /// <summary>
        /// Generates the config files necessary for the assembler run.
        /// </summary>
        public void GenerateConfigs()
        {
            username = HelperMethods.GetUsername();
            urlPath = "/AssemblerConfigs/" + "Job-" + username + "-" + seed + "/";
            localPath = @"D:\AssemblerConfigs\Job-" + username + "-" + seed + "\\";

            CreateDirectory();

            BuildInitConfig();
            if (genomeModel.UseMasurca) { BuildMasurcaConfig(); }
        }

        /// <summary>
        /// Creates the directory on the FTP with a randomized name.
        /// </summary>
        /// <param name="seed">Random integer value.</param>
        private void CreateDirectory()
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
        private void BuildInitConfig()
        {
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

                            // Concat the left reads together into a leftReads.fastq file and delete old files.
                            tw.WriteLine("cat " + concatFiles + " > leftReads.fastq");
                            tw.WriteLine("rm leftData_*");
                            concatFiles = "";

                            // Now add the wgets for the right reads URLs and rename them to rightData_[i]:
                            for (int i = 0; i < rightReads.Count; i++)
                            {
                                tw.WriteLine("wget -O rightData_" + i + " " + rightReads[i].ToString());
                                concatFiles = concatFiles + " rightData_" + i;
                            }

                            // Concat the right reads together into a rightReads.fastq file and delete old files.
                            tw.WriteLine("cat " + concatFiles + " > rightReads.fastq");
                            tw.WriteLine("rm rightData_*");
                        }

                        InitConfigURL = Accessors.FTP_URL + urlPath + fileName;
                    }
                }

                catch (Exception e)
                {
                    LinuxErrorHandling.error = e.Message;
                }
            }

            // We have a problem since the file already exists.
            else
            {
                LinuxErrorHandling.error = "Unfortunately, we couldn't create the necessary configuration files to submit your job. Please contact an administrator.";

                throw new IOException("Attempted to create \"" + fullPath + "\" but it already exists so we cannot create the file. Continuing is not advised. ");
            }
        }

        /// <summary>
        /// Creates the configuration file for the Masurca assembler for each unique run.
        /// </summary>
        private void BuildMasurcaConfig()
        {
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

                        MasurcaConfigURL = Accessors.FTP_URL + urlPath + fileName;
                    }
                }

                catch (Exception e)
                {
                    LinuxErrorHandling.error = e.Message;
                }
            }

            // We have a problem since the file already exists.
            else
            {
                LinuxErrorHandling.error = "Unfortunately, we couldn't create the necessary configuration files to submit your job. Please contact an administrator.";

                throw new IOException("Attempted to create \"" + fullPath + "\" but it already exists so we cannot create the file. Continuing is not advised. ");
            }
        }

        /// <summary>
        /// Creates all the necessary folders, downloads the config scripts, and adds the job to the scheduler on BigDog.
        /// </summary>
        /// <returns>Returns true only if a job gets successfully added to SGE.</returns>
        public bool CreateJob()
        {
            // The init.sh script will contain all the basic logic to download the data and initiate the job on the assembler(s).
            using (var client = new SshClient(Accessors.BD_IP, genomeModel.SSHUser, genomeModel.SSHPass))
            {
                // Set defaults
                Accessors.masterPath = "/share/scratch/bioinfo/" + HelperMethods.GetUsername();
                string node = Accessors.BD_COMPUTE_NODE1; // default  
                string wgetLogParameter = "--output-file=" + Accessors.GetJobLogPath(seed) + "wget.error";
                string initPath = Accessors.GetJobConfigPath(seed) + "init.sh";
                string masurcaPath = Accessors.GetJobConfigPath(seed) + "masurca_config.txt";
                string jobName = HelperMethods.GetUsername() + "-" + seed;

                try
                {
                    client.Connect();

                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.CreateDirectory(client, Accessors.masterPath, "-p"); }
                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.CreateDirectory(client, Accessors.GetJobPath(seed), "-p"); }
                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.CreateDirectory(client, Accessors.GetJobDataPath(seed), "-p"); }
                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.CreateDirectory(client, Accessors.GetJobConfigPath(seed), "-p"); }
                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.CreateDirectory(client, Accessors.GetJobOutputPath(seed), "-p"); }
                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.CreateDirectory(client, Accessors.GetJobLogPath(seed), "-p"); }

                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.DownloadFile(client, initPath, InitConfigURL, wgetLogParameter); }
                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.RunDos2Unix(client, initPath); }

                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.DownloadFile(client, masurcaPath, MasurcaConfigURL, wgetLogParameter); }
                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.RunDos2Unix(client, masurcaPath); }

                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.ChangePermissions(client, Accessors.GetJobPath(seed), "777", "-R"); }

                    if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                    {
                        // So COMPUTENODE2 has a smaller load, we want to use that instead.
                        if (LinuxCommands.GetNodeLoad(client, Accessors.BD_COMPUTE_NODE1) > LinuxCommands.GetNodeLoad(client, Accessors.BD_COMPUTE_NODE2))
                            node = Accessors.BD_COMPUTE_NODE2;

                        else
                            node = Accessors.BD_COMPUTE_NODE1;
                    }

                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { LinuxCommands.AddJobToScheduler(client, Accessors.GetJobDataPath(seed), Accessors.GetJobLogPath(seed), node, jobName, initPath); }

                    if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { genomeModel.SGEJobId = LinuxCommands.SetJobNumber(client, genomeModel.SSHUser, jobName); }

                    // There were no errors.
                    if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                        return true;

                    else
                        return false;
                }

                // SSH Connection couldn't be established.
                catch (SocketException e)
                {
                    LinuxErrorHandling.error = "The SSH connection couldn't be established. " + e.Message;

                    return false;
                }

                // Authentication failure.
                catch (SshAuthenticationException e)
                {
                    LinuxErrorHandling.error = "The credentials were entered incorrectly. " + e.Message;

                    return false;
                }

                // The SSH connection was dropped.
                catch (SshConnectionException e)
                {
                    LinuxErrorHandling.error = "The connection was terminated unexpectedly. " + e.Message;

                    return false;
                }

                catch (Exception e)
                {
                    LinuxErrorHandling.error = "There was an uncaught exception. " + e.Message;

                    return false;
                }
            }
        }
    }
}