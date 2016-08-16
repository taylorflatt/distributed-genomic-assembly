using Genome.Models;
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
        public const int NUM_BASE_OVERALL_STEPS = 8;
        public const string INITIAL_STEP = "Program Queued";

        /// <summary>
        /// Gets the list of masurca steps.
        /// </summary>
        /// <returns> A hastable that consists of a key, a filename that will exist if the step has been reached, and a description of the step.</returns>
        public static HashSet<Assembler> GetMasurcaStepList()
        {
            HashSet<Assembler> masurcaStepFiles = new HashSet<Assembler>();
            masurcaStepFiles.Add(new Assembler(1, "", "Queued"));
            masurcaStepFiles.Add(new Assembler(2, "FILENAME2", "Description"));
            masurcaStepFiles.Add(new Assembler(3, "FILENAME3", "Description"));
            masurcaStepFiles.Add(new Assembler(4, "FILENAME4", "Description"));
            masurcaStepFiles.Add(new Assembler(5, "FILENAME5", "Description"));
            masurcaStepFiles.Add(new Assembler(6, "FILENAME6", "Description"));
            masurcaStepFiles.Add(new Assembler(7, "masurca_finished.olog", "Complete"));

            return masurcaStepFiles;
        }

        /// <summary>
        /// Dynamically creates a list of steps and descriptions for a current job given the total number of assemblers.
        /// </summary>
        /// <param name="numOverallSteps"> The total number of steps that the list will contain</param>
        /// <returns> Returns a hashtable that consists of a key corresponding to the step number and a description.</returns>
        /// <remarks>IMPORTANT: If the total number of base steps changes, be sure to change that at the top of the class as well.</remarks>
        /// <remarks>IMPORTANT: If the RUNNING ASSEMBLERS position changes, then the SetAssemblersRunningStep() needs to be changed as well.</remarks>
        public static Hashtable GenerateOverallStepList(int numOverallSteps)
        {
            int numAssemblers = numOverallSteps - NUM_BASE_OVERALL_STEPS;
            int stepNum = 1;

            Hashtable stepList = new Hashtable(numOverallSteps);
            stepList.Add(stepNum++, INITIAL_STEP);
            stepList.Add(stepNum++, "Data Conversion");
            stepList.Add(stepNum++, "Running Assemblers");

            for (int index = 1; index <= numAssemblers; index++)
            {
                stepList.Add(stepNum++, "Finished Assembler (" + index + " of " + numAssemblers + ")");
            }

            stepList.Add(stepNum++, "Data Analysis");
            stepList.Add(stepNum++, "Compressing Data");
            stepList.Add(stepNum++, "Connecting to SFTP");
            stepList.Add(stepNum++, "Uploading Data to FTP");
            stepList.Add(stepNum, "Completed");

            if (stepNum != numOverallSteps)
                throw new ArgumentOutOfRangeException(Convert.ToString(stepNum), "While creating the overall step list, the method ran into an error. "
                    + "The values of the overall step list and that of the internal counter should not be different. numOverallSteps = " + numOverallSteps + " and stepNum = " + stepNum + ".");

            return stepList;
        }

        /// <summary>
        /// Generates the list of errors for a job.
        /// </summary>
        /// <param name="numOverallSteps">The number of steps for a particular job.</param>
        /// <returns>Returns a hashtable containing the list of errors for a job.</returns>
        /// <remarks>The NeedsUploaded() method depends upon the state of this list. So if you change this list, you might have to change that method so it remains correct.</remarks>
        private static Hashtable GenerateOverallStepErrors(int numOverallSteps)
        {
            int numAssemblers = numOverallSteps - NUM_BASE_OVERALL_STEPS;
            int stepNum = 1;

            Hashtable errorList = new Hashtable(numOverallSteps);
            errorList.Add(stepNum++, "Error queueing program.");
            errorList.Add(stepNum++, "Error converting the sequenced data.");
            errorList.Add(stepNum++, "Error running assembler(s).");

            for (int index = 1; index <= numAssemblers; index++)
            {
                errorList.Add(stepNum++, "Internal error with one or more assemblers.");
            }

            errorList.Add(stepNum++, "Error with data analysis.");
            errorList.Add(stepNum++, "Error compressing data.");
            errorList.Add(stepNum++, "Error connecting to the FTP.");
            errorList.Add(stepNum++, "Error uploading data to the FTP.");
            errorList.Add(stepNum, "Error completing the job.");

            if (stepNum != numOverallSteps)
                throw new ArgumentOutOfRangeException(Convert.ToString(stepNum), "While creating the overall step error list, the method ran into an error. "
                    + "The values of the overall step list and that of the internal counter should not be different. numOverallSteps = " + numOverallSteps + " and stepNum = " + stepNum + ".");

            return errorList;
        }

        /// <summary>
        /// Updates the status of the job by appropriately increasing the step and changing the status.
        /// </summary>
        /// <param name="genomeModel">The model that represents the current job.</param>
        /// <param name="hasError">If there is an error during execution, it will set the current status of the job to an error state and sets the step number to the final step.</param>
        public static void NextOverallStep(GenomeModel genomeModel, bool hasError = false, string customErrorMsg = "")
        {
            if (hasError)
            {
                if (string.IsNullOrWhiteSpace(customErrorMsg))
                    genomeModel.OverallStatus = GenerateOverallStepErrors(genomeModel.OverallStepSize)[genomeModel.OverallCurrentStep].ToString();
                else
                    genomeModel.OverallStatus = customErrorMsg;

                genomeModel.OverallCurrentStep = genomeModel.OverallStepSize;
            }

            else
                genomeModel.OverallStatus = GenerateOverallStepList(genomeModel.OverallStepSize)[++genomeModel.OverallCurrentStep].ToString();
        }

        /// <summary>
        /// Sets the current step to 3 (the step number running assemblers) and sets the appropriate status.
        /// </summary>
        /// <param name="genomeModel">The model that represents the current job.</param>
        /// <remarks>Since the running assemblers is always checked, there needs to be a way to keep consistency. So I simply "reset" the value
       ///  here which will be modified if needed later.</remarks>
        public static void SetAssemblersRunningStep(GenomeModel genomeModel)
        {
            genomeModel.OverallStatus = GenerateOverallStepList(genomeModel.OverallStepSize)[3].ToString();
            genomeModel.OverallCurrentStep = 3;
        }

        ///// <summary>
        ///// Gets the Data Conversion step number for the job.
        ///// </summary>
        ///// <returns>Returns an integer representing the conversion step number.</returns>
        //public static int GetDataConversionStepNum()
        //{
        //    return 2;
        //}

        ///// <summary>
        ///// Gets the Running Assembler step number for the job.
        ///// </summary>
        ///// <returns>Returns an integer representing the running assembler step number.</returns>
        //public static int GetRunningAssemblersStepNum()
        //{
        //    return 3;
        //}

        ///// <summary>
        ///// Gets the upload data step number for the job.
        ///// </summary>
        ///// <param name="listSize">The size of the list containing the job steps.</param>
        ///// <returns>Returns locations of the step in the list. </returns>
        //public static int GetUploadDataStepNum(int listSize)
        //{
        //    return listSize - 1;
        //}

        ///// <summary>
        ///// Gets the connecting to SFTP step number for the job.
        ///// </summary>
        ///// <param name="listSize">The size of the list containing the job steps.</param>
        ///// <returns>Returns locations of the step in the list. </returns>
        //public static int GetConnectingToSftpStepNum(int listSize)
        //{
        //    return listSize - 2;
        //}

        ///// <summary>
        ///// Gets the compressing data step number for the job.
        ///// </summary>
        ///// <param name="listSize">The size of the list containing the job steps.</param>
        ///// <returns>Returns locations of the step in the list. </returns>
        //public static int GetCompressingDataStepNum(int listSize)
        //{
        //    return listSize - 3;
        //}

        ///// <summary>
        ///// Gets the data analysis step number for the job.
        ///// </summary>
        ///// <param name="listSize">The size of the list containing the job steps.</param>
        ///// <returns>Returns locations of the step in the list. </returns>
        //public static int GetDataAnalysisStepNum(int listSize)
        //{
        //    return listSize - 4;
        //}

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

        ///// <summary>
        ///// This will return the particular description for the given step.
        ///// </summary>
        ///// <param name="stepList"> The particular step list.</param>
        ///// <param name="stepId"> The step id that corresponds to the description that you want.</param>
        ///// <returns> Returns the description of the associated step id or returns that it could not be found.</returns>
        //public static string GetCurrentStepDescription(Hashtable stepList, int stepId)
        //{
        //    foreach (DictionaryEntry item in stepList)
        //    {
        //        if (Convert.ToInt32(item.Key) == stepId)
        //            return item.Value.ToString();
        //    }

        //    return "We could not find that step. " + stepId;
        //}

        ///// <summary>
        ///// This will return the particular description for the given step.
        ///// </summary>
        ///// <param name="stepList"> The particular step list.</param>
        ///// <param name="stepId"> The step id that corresponds to the description that you want.</param>
        ///// <returns> Returns the description of the associated step id or returns that it could not be found.</returns>
        //public static string GetCurrentStepDescription(Hashtable stepList, int stepId)
        //{
        //    var stepDescription = stepList[stepId].ToString();

        //    return string.IsNullOrWhiteSpace(stepDescription) ?  "We could not find that step and/or description." : stepDescription;
        //}
    }
}