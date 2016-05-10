using Genome.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Genome.Helpers
{
    public class HelperMethods
    {
        protected internal static List<string> ParseUrlString(string urlString)
        {
            return urlString.Split(',').Select(sValue => sValue.Trim()).ToList();
        }

        protected internal static int NumberOfAssemblers(GenomeModel genomeModel)
        {
            int numAssemblers = 0;

            if (genomeModel.UseMasurca)
                numAssemblers++;

            if (genomeModel.UseSGA)
                numAssemblers++;

            if (genomeModel.UseWGS)
                numAssemblers++;

            return numAssemblers;
        }

        protected internal static void CreateUrlLists(List<string> dataSources, out List<string> leftReads, out List<string> rightReads)
        {
            leftReads = new List<string>();
            rightReads = new List<string>();

            // Split the data URLs into their respective lists so they get concatenated correctly.
            for (int i = 0; i < dataSources.Count; i++)
            {
                if (i % 2 == 0)
                    leftReads.Add(dataSources[i]);
                else
                    rightReads.Add(dataSources[i]);
            }
        }

        protected internal static GenomeModel SetDefaultMasurcaValues(GenomeModel genomeModel)
        {
            // If they don't have jump reads.
            if (genomeModel.JumpReads == false)
                genomeModel.JumpLength = 0;

            // If they don't have paired-end reads.
            if (genomeModel.PEReads == false)
                genomeModel.PairedEndLength = 0;

            if (genomeModel.MasurcaMean == null)
                genomeModel.MasurcaMean = 180;

            if (genomeModel.MasurcaStdev == null)
                genomeModel.MasurcaStdev = 20;

            // This could ALSO be set to auto which makes it calculated by the program.
            if (genomeModel.MasurcaGraphKMerValue == null)
                genomeModel.MasurcaGraphKMerValue = 50; 

            if (genomeModel.MasurcaKMerErrorCount == null)
                genomeModel.MasurcaKMerErrorCount = 1;

            if (genomeModel.MasurcaThreadNum == null)
                genomeModel.MasurcaThreadNum = 20;

            genomeModel.MasurcaCurrentStep = 1;
            genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), 1);

            return genomeModel;
        }
    }
}