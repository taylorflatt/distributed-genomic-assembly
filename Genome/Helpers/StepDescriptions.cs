using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Genome.Helpers
{
    public class Assembler
    {
        public int step { get; set; }
        public string filename { get; set; }
        public string description { get; set; }

        public Assembler(int step, string filename, string description)
        {
            this.step = step;
            this.filename = filename;
            this.description = description;
        }
    }

    public class StepDescriptions
    {
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

        public static Hashtable GenerateOverallStepList(int numAssemblers)
        {
            Hashtable stepList = new Hashtable();

            stepList.Add(1, "Program Queued");
            stepList.Add(2, "Data Conversion");
            stepList.Add(3, "Running Assemblers");

            int offset = 4;
            
            for(int index = 1; index <= numAssemblers; index++)
            {
                stepList.Add(offset++, "Finished Assembler " + index + " of " + numAssemblers + ")");
            }

            stepList.Add(offset++, "Data Analysis");
            stepList.Add(offset++, "Uploading Data");

            // If you change this, you MUST change it in the CheckJobStatus.cs file in the jobList variable.
            stepList.Add(offset++, "Complete");

            return stepList;
        }

        public static string GetCurrentStepDescription(HashSet<Assembler> stepList, int stepId)
        {
            string stepDescription = "";

            foreach (var item in stepList)
            {
                if (item.step == stepId)
                {
                    stepDescription = item.description;
                    break;
                }
            }

            // Our description was never assigned so we never found the given step.
            if (string.IsNullOrEmpty(stepDescription))
                return "We could not find that step. " + stepId;

            else
                return stepDescription;
        }



        public static string GetCurrentStepDescription(Hashtable stepList, int stepId)
        {
            string stepDescription = "";

            foreach (DictionaryEntry item in stepList)
            {
                if (Convert.ToInt32(item.Key) == stepId)
                {
                    stepDescription = item.Value.ToString();
                    break;
                }
            }

            // Our description was never assigned so we never found the given step.
            if (string.IsNullOrEmpty(stepDescription))
                return "We could not find that step. " + stepId;

            else
                return stepDescription;
        }
    }
}