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
        public const int NUM_BASE_OVERALL_STEPS = 7;

        /// <summary>
        /// List of errors for the steps.
        /// </summary>
        public const string COMPRESSION_ERROR = "Error compressing data";
        public const string SFTP_CONNECTION_ERROR = "Error connecting to SFTP";
        public const string UPLOAD_TO_FTP_ERROR = "Error uploading data to SFTP";

        /// <summary>
        /// The two steps that will always be static.
        /// </summary>
        public const string INITIAL_STEP = "Program Queued";
        public const string FINAL_STEP = "Completed";

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
        /// <remarks>If the total number of base steps changes, be sure to change that at the top of the class as well.</remarks>
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
            stepList.Add(stepNum, FINAL_STEP);

            if (stepNum != numOverallSteps)
                throw new ArgumentOutOfRangeException(Convert.ToString(stepNum), "While creating the overall step list, the method ran into an error. "
                    + "The values of the overall step list and that of the internal counter should not be different. numOverallSteps = " + numOverallSteps + " and stepNum = " + stepNum + ".");

            return stepList;
        }

        /// <summary>
        /// Gets the Data Conversion step number for the job.
        /// </summary>
        /// <returns>Returns an integer representing the conversion step number.</returns>
        public static int GetDataConversionStepNum()
        {
            return 2;
        }

        /// <summary>
        /// Gets the Running Assembler step number for the job.
        /// </summary>
        /// <returns>Returns an integer representing the running assembler step number.</returns>
        public static int GetRunningAssemblersStepNum()
        {
            return 3;
        }

        /// <summary>
        /// Gets the upload data step number for the job.
        /// </summary>
        /// <param name="listSize">The size of the list containing the job steps.</param>
        /// <returns>Returns locations of the step in the list. </returns>
        public static int GetUploadDataStepNum(int listSize)
        {
            return listSize - 1;
        }

        /// <summary>
        /// Gets the connecting to SFTP step number for the job.
        /// </summary>
        /// <param name="listSize">The size of the list containing the job steps.</param>
        /// <returns>Returns locations of the step in the list. </returns>
        public static int GetConnectingToSftpStepNum(int listSize)
        {
            return listSize - 2;
        }

        /// <summary>
        /// Gets the compressing data step number for the job.
        /// </summary>
        /// <param name="listSize">The size of the list containing the job steps.</param>
        /// <returns>Returns locations of the step in the list. </returns>
        public static int GetCompressingDataStepNum(int listSize)
        {
            return listSize - 3;
        }

        /// <summary>
        /// Gets the data analysis step number for the job.
        /// </summary>
        /// <param name="listSize">The size of the list containing the job steps.</param>
        /// <returns>Returns locations of the step in the list. </returns>
        public static int GetDataAnalysisStepNum(int listSize)
        {
            return listSize - 4;
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

        /// <summary>
        /// This will return the particular description for the given step.
        /// </summary>
        /// <param name="stepList"> The particular step list.</param>
        /// <param name="stepId"> The step id that corresponds to the description that you want.</param>
        /// <returns> Returns the description of the associated step id or returns that it could not be found.</returns>
        public static string GetCurrentStepDescription(Hashtable stepList, int stepId)
        {
            foreach (DictionaryEntry item in stepList)
            {
                if (Convert.ToInt32(item.Key) == stepId)
                    return item.Value.ToString();
            }

            return "We could not find that step. " + stepId;
        }
    }
}