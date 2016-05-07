using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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

    public class StepDescriptions
    {
        public const string COMPRESSION_ERROR = "Error compressing data";
        public const string SFTP_CONNECTION_ERROR = "Error connecting to SFTP";
        public const string UPLOAD_TO_FTP_ERROR = "Error uploading data to SFTP";

        public const string INITIAL_STEP = "Program Queued";
        public const string FINAL_STEP = "Completed";

        public static int offset = 1;

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
        /// <param name="numAssemblers"> The total number of assemblers that the job is currently running on.</param>
        /// <returns> Returns a hashtable that consists of a key corresponding to the step number and a description.</returns>
        public static Hashtable GenerateOverallStepList(int numAssemblers)
        {
            Hashtable stepList = new Hashtable();

            stepList.Add(offset++, INITIAL_STEP);
            stepList.Add(offset++, "Data Conversion");
            stepList.Add(offset++, "Running Assemblers");

            for (int index = 1; index <= numAssemblers; index++)
            {
                stepList.Add(offset++, "Finished Assembler " + index + " of " + numAssemblers + ")");
            }

            stepList.Add(offset++, "Data Analysis");                    // offset - 4
            stepList.Add(offset++, "Compressing Data");                 // offset - 3
            stepList.Add(offset++, "Connecting to SFTP");               // offset - 2
            stepList.Add(offset++, "Uploading Data to FTP");            // offset - 1

            // If you change this, you MUST change it in the CheckJobStatus.cs file in the jobList variable.
            stepList.Add(offset, FINAL_STEP);                           // offset - 0

            return stepList;
        }

        public static int GetOffset(int numAssemblers)
        {
            return numAssemblers + offset;
        }

        public static int GetUploadDataStepNum(int offset)
        {
            return offset - 1;
        }

        public static int GetConnectingToSftpStepNum(int offset)
        {
            return offset - 2;
        }

        public static int GetCompressingDataStepNum(int offset)
        {
            return offset - 3;
        }

        public static int GetDataAnalysisStepNum(int offset)
        {
            return offset - 4;
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