﻿using Genome.Models;
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

        public string BuildMasurcaConfig(GenomeModel genomeModel, List<string> dataSource)
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
                    if(dataSource.Count > 1)
                        tw.WriteLine("PE= pe " + genomeModel.MasurcaMean + genomeModel.MasurcaStdev + "leftReads.fastq rightReads.fastq");

                    else
                        tw.WriteLine("PE= pe " + genomeModel.MasurcaMean + genomeModel.MasurcaStdev + "sequentialData.fastq");
                }

                // These locations are not the same, used for now for development purposes
                else if (genomeModel.JumpReads)
                {
                    if (dataSource.Count > 1)
                        tw.WriteLine("JUMP= sh " + genomeModel.MasurcaMean + genomeModel.MasurcaStdev + "leftReads.fastq rightReads.fastq");

                    else
                        tw.WriteLine("JUMP= sh " + genomeModel.MasurcaMean + genomeModel.MasurcaStdev + "sequentialData.fastq");
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

        public string BuildInitConfig(GenomeModel genomeModel, List<string> dataSources)
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

                // If we have sequential reads there will be only a single URL:
                if(dataSources.Count == 1)
                {
                    tw.WriteLine("wget -O sequentialData.fastq" + dataSources[0].ToString());
                }

                // If we have any other type of reads there will be at least a left and right read:
                else
                {
                    List<string> leftReads = new List<string>();
                    List<string> rightReads = new List<string>();

                    // Create the URL lists with the left and right reads split.
                    HelperMethods.CreateUrlLists(dataSources, out leftReads, out rightReads);

                    // Now add the wgets for the left reads URLs and rename them to leftData_[j]:
                    for (int j = 0; j < leftReads.Count; j++)
                    {
                        tw.WriteLine("wget -O leftData_" + j  + " " + leftReads[j].ToString());
                    }

                    // If we have MULTIPLE sets of urls we need to concat them into a single file:
                    if (dataSources.Count > 2)
                    {
                        string concatFiles = "";

                        for(int j = 0; j < leftReads.Count; j++)
                        {
                            concatFiles = concatFiles + " " + leftReads[j].ToString();
                        }

                        // Concat the left reads together into a leftReads.fastq file.
                        tw.WriteLine("cat " + concatFiles + "> leftReads.fastq");
                    }

                    // Now add the wgets for the right reads URLs and rename them to rightData_[i]:
                    for (int i = 0; i < rightReads.Count; i++)
                    {
                        tw.WriteLine("wget -O leftData_" + i + " " + rightReads[i].ToString());
                    }

                    // If we have MULTIPLE sets of urls we need to concat them into a single file:
                    if (dataSources.Count > 2)
                    {
                        string concatFiles = "";

                        for (int i = 0; i < rightReads.Count; i++)
                        {
                            concatFiles = concatFiles + " " + rightReads[i].ToString();
                        }

                        // Concat the right reads together into a rightReads.fastq file.
                        tw.WriteLine("cat " + concatFiles + "> rightReads.fastq");
                    }
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