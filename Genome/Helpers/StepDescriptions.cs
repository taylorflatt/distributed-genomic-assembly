using Genome.Models;
using Renci.SshNet;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Genome.Helpers
{
    /// <summary>
    /// Create a custom type so we can create appropriate hashtables that consist of a step, the requisite filename associated with our step, and the description we attribute to the step.
    /// </summary>
    public class Assembler
    {
        public int step { get; set; }
        public string filename { get; set; }
        public string description { get; set; }

        /// <summary>
        /// Creates an Assembler object to be used for refrencing job updates and information.
        /// </summary>
        /// <param name="step"> The step number.</param>
        /// <param name="filename"> The name of the file that must exist for this step to be reached.</param>
        /// <param name="description"> The custom description given to define the step.</param>
        public Assembler(int step, string filename, string description)
        {
            this.step = step;
            this.filename = filename;
            this.description = description;
        }
    }

    public static class StepDescriptions
    {
        /// <summary>
        /// Number of BASE steps to each job. This will vary depending on the particular job (how many assemblers they choose).
        /// </summary>
        public const string INITIAL_STEP = "Program Queued";

        /// <summary>
        /// Gets the list of masurca steps.
        /// </summary>
        /// <returns> A hastable that consists of a key, a filename that will exist if the step has been reached, and a description of the step.</returns>
        public static HashSet<Assembler> GetMasurcaStepList()
        {
            HashSet<Assembler> masurcaStepFiles = new HashSet<Assembler>();
            masurcaStepFiles.Add(new Assembler(1, "", "Queued"));
            masurcaStepFiles.Add(new Assembler(2, "pe.cor.fa", "Generated error corrected reads. (No quality scores)"));
            masurcaStepFiles.Add(new Assembler(3, "FILENAME3", "Description"));
            masurcaStepFiles.Add(new Assembler(4, "FILENAME4", "Description"));
            masurcaStepFiles.Add(new Assembler(5, "FILENAME5", "Description"));
            masurcaStepFiles.Add(new Assembler(6, "FILENAME6", "Description"));
            masurcaStepFiles.Add(new Assembler(7, "masurca_finished.olog", "Complete"));

            return masurcaStepFiles;
        }

        public static void SetMasurcaError(SshClient client, GenomeModel genomeModel)
        {
            genomeModel.MasurcaCurrentStep = -1;
            genomeModel.MasurcaStatus = LinuxCommands.GetMasurcaError(client, genomeModel.Seed);
        }

        /// <summary>
        /// Dynamically creates a list of steps and descriptions for a current job given the total number of assemblers.
        /// </summary>
        /// <param name="numOverallSteps"> The total number of steps that the list will contain</param>
        /// <returns> Returns a hashtable that consists of a key corresponding to the step number and a description.</returns>
        /// <remarks>IMPORTANT: If the total number of base steps changes, be sure to change the total number of steps in GenomeAssembly/Details.cshtml</remarks>
        public static Hashtable GetOverallStepList()
        {
            Hashtable stepList = new Hashtable(6);

            stepList.Add(1, INITIAL_STEP);
            stepList.Add(2, "Data Conversion");
            stepList.Add(3, "Running Assemblers");
            stepList.Add(4, "Data Analysis");
            stepList.Add(5, "Uploading Data to FTP");
            stepList.Add(6, "Completed");
            stepList.Add(6, "Error");

            return stepList;
        }

        /// <summary>
        /// Generates the list of errors for a job.
        /// </summary>
        /// <param name="numOverallSteps">The number of steps for a particular job.</param>
        /// <returns>Returns a hashtable containing the list of errors for a job.</returns>
        /// <remarks>The NeedsUploaded() method depends upon the state of this list. So if you change this list, you might have to change that method so it remains correct.</remarks>
        private static Hashtable GetOverallStepErrors()
        {
            Hashtable errorList = new Hashtable(6);

            errorList.Add(1, "Error queueing program.");
            errorList.Add(2, "Error converting the sequenced data.");
            errorList.Add(3, "Error running assembler(s).");
            errorList.Add(4, "Error with data analysis.");
            errorList.Add(5, "Error uploading data to the FTP.");
            errorList.Add(6, "Error completing the job.");

            return errorList;
        }

        /// <summary>
        /// Updates the status of the job by appropriately increasing the step and changing the status.
        /// </summary>
        /// <param name="genomeModel">The model that represents the current job.</param>
        /// <param name="hasError">If there is an error during execution, it will set the current status of the job to an error state and sets the step number to the final step.</param>
        /// <param name="customErrorMsg">Instead of the default error messages associated with each step, you may list your own.</param>
        public static void NextStep(this GenomeModel genomeModel, bool hasError = false, string customErrorMsg = "")
        {
            if (hasError)
            {
                if (string.IsNullOrWhiteSpace(customErrorMsg))
                    genomeModel.OverallStatus = GetOverallStepErrors()[genomeModel.OverallCurrentStep].ToString();

                else
                    genomeModel.OverallStatus = customErrorMsg;
            }

            else
                genomeModel.OverallStatus = GetOverallStepList()[++genomeModel.OverallCurrentStep].ToString();
        }

        /// <summary>
        /// This will return the particular description for the given step.
        /// </summary>
        /// <param name="stepList"> The particular step list.</param>
        /// <param name="stepId"> The step id that corresponds to the description that you want.</param>
        /// <returns> Returns the description of the associated step id or returns that it could not be found.</returns>
        public static string GetCurrentStepDescription(HashSet<Assembler> stepList, int stepId)
        {
            foreach (var item in stepList)
            {
                if (item.step == stepId)
                    return item.description;
            }

            return "We could not find that step. " + stepId;
        }
    }
}