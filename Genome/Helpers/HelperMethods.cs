using Genome.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using System.Web;
using Renci.SshNet;

namespace Genome.Helpers
{
    public static class HelperMethods
    {
        /// <summary>
        /// Splits the list of URLs by commas (,) created by the wizard.
        /// </summary>
        /// <param name="urlString">The concatenated string of URLs from the wizard.</param>
        /// <returns>A list of split urls.</returns>
        public static List<string> ParseUrlString(string urlString)
        {
            return urlString.Split(',').Select(sValue => sValue.Trim()).ToList();
        }

        /// <summary>
        /// Determines if an integer is between two other integers. Inclusivity is optional but not default.
        /// </summary>
        /// <param name="num">An integer representing the calling object. The thing we are testing.</param>
        /// <param name="lowerBound">The lowest bound.</param>
        /// <param name="upperBound">The highest bound.</param>
        /// <param name="inclusive">Forces the check to be inclusive (includes the boundary).</param>
        /// <returns>Returns true is the number is between the boundary points, false otherwise.</returns>
        public static bool IsBetween(this int num, int lowerBound, int upperBound, bool inclusive=false)
        {
            if(inclusive)
            {
                if (num > upperBound)
                    return false;
                else if (num < lowerBound)
                    return false;
                else
                    return true;
            }

            else
            {
                if (num >= upperBound)
                    return false;
                else if (num <= lowerBound)
                    return false;
                else
                    return true;
            }
        }

        /// <summary>
        /// Gets the username of the current user. 
        /// </summary>
        /// <returns>Returns the username (email without everything after the @ symbol) of the current user.</returns>
        public static string GetUsername()
        {
            return HttpContext.Current.User.Identity.Name.ToString().Split('@')[0];
        }

        /// <summary>
        /// Tests whether the URLs entered by the user in the wizard are connectable.
        /// </summary>
        /// <returns>Returns a null string if the URL is connectable. Otherwise it will return the URL that is malfunctioning.</returns>
        public static string TestJobUrls(GenomeModel genomeModel)
        {
            using (var client = new SshClient(Accessors.BD_IP, genomeModel.SSHUser, genomeModel.SSHPass))
            {
                client.Connect();

                List<string> urlList = ParseUrlString(genomeModel.DataSource);

                foreach (var url in urlList)
                {
                    if (!LinuxCommands.CheckDataAvailability(client, url))
                        return url;
                }

                return "";
            }
        }

        /// <summary>
        /// Determines how many assemblers have been chosen by the user for a particular job.
        /// </summary>
        /// <param name="genomeModel">The model data for a particular job.</param>
        /// <returns>An integer representing how many assemblers have been chosen for a particular job.</returns>
        public static int NumberOfAssemblers(GenomeModel genomeModel)
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

        /// <summary>
        /// Splits up a list of URLs into left and right reads provided the data type is Jump Reads or Paired-End. Note, this method is not applicable for Sequential Reads.
        /// </summary>
        /// <param name="dataSources">List of string datasources alternating between left and right reads in order.</param>
        /// <param name="leftReads">List of string datasources representing the left reads being sent out of the method.</param>
        /// <param name="rightReads">List of string datasources representing the right reads being sent out of the method.</param>
        public static void CreateUrlLists(List<string> dataSources, out List<string> leftReads, out List<string> rightReads)
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

        /// <summary>
        /// Sets default values if the user decided not to enter any information for optional values in the wizard.
        /// </summary>
        /// <param name="genomeModel">The model data for a particular job.</param>
        /// <returns>Returns the new job model with the default values.</returns>
        public static GenomeModel SetDefaultMasurcaValues(GenomeModel genomeModel)
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

            // We don't specify anything here. If it is null, we set it to AUTO in the masurca method of the config builder.
            if (genomeModel.MasurcaGraphKMerValue == null)

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