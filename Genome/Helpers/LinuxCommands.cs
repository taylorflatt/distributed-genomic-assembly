using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Genome.Helpers
{
    public class LinuxCommands
    {

        #region General Commands

        /// <summary>
        /// Checks to see if a particular file is accessible remotely from the BigDog cluster. WARNING: This may or may not be exact. Don't assume if this passes that the file is and 
        /// will always be accessible.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="url">The file URL that needs to be verified.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>Returns a boolean value as to whether or not a URL is accessible from the BigDog cluster.</returns>
        protected internal static bool CheckDataAvailability(SshClient client, string url, out string error)
        {
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // BUG: For some reason, during the case in which a good link is passed first (L0 position), all subsequent links are then passed.
            // I have tried using random logs (to make sure they generate differently) and that didn't work. I have tried using echo &? to tell 
            // me the exit status of the previous command (which will ALWAYS be the previous one in the pipe). I'm honestly not sure why this 
            // behavior is occurring. I need to do some more debugging in this method and its use in the SSHConfig class.
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            // We use wget with the spider option to see if we can access the header of the particular file. We then redirect the output to a file and check whether the file exists or not.
            using (var cmd = client.CreateCommand("wget --server-response --spider -O - " + '"' + url + '"' + " 2> /dev/null | echo $?"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);

                if(string.IsNullOrEmpty(error))
                {
                    if (string.IsNullOrEmpty(error) && Convert.ToInt32(cmd.Result) == 0)
                        return true;

                    return false;
                }

                else
                    return false;              
            }
        }

        // Check whether the quota is returning in Gb or Mb.
        /// <summary>
        /// Determines whether or not the quota is in GB or MB.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>Returns a string G or M corresponding to the size of the quota.</returns>
        /// TODO: We need to make sure that we are grabbing the correct location. I need to check with BigDog admin on how they do quota increases and if they carry over to all 
        /// directories or just specific areas. Right now we just grab the first letter of the first return but there can be multiple ones like "G\nG\nG\n" for instance or just one "G\n".
        protected internal static string CheckQuotaType(SshClient client, out string error)
        {
            // First we print out the quota then we grab only the quota column, then filter out everything that doesn't have a number in it and then remove all numbers from what is left.
            using (var cmd = client.CreateCommand("quota -vs | awk '{print $2}' | grep '[0-9][0-9]*' | grep -o '[a-zA-Z]'"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);

                if (!cmd.Result.ToString().Substring(0, 1).Equals("M") && !cmd.Result.ToString().Substring(0, 1).Equals("G"))
                    throw new Exception("There was undetermined behavior in the quota return value. We expected 'M' or 'G' but instead we got: " + cmd.Result.ToString());

                return cmd.Result.ToString().Substring(0, 1);
            }
        }

        // Check the actual number size of the quota being returned regardless of the type (Gb or Mb).
        /// <summary>
        /// Gets the size of quota the user has as an integer.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>Returns an integer number representing the quota size in MB or GB.</returns>
        protected internal static int CheckQuotaSize(SshClient client, out string error)
        {
            // Make sure they are in their home directory so we only get a single number. Need to investigate this. (DOESN'T WORK/MATTER)
            ChangeDirectory(client, "~", out error);

            // First we print out the quota then we grab only the quota column. Then we grab the number.
            using (var cmd = client.CreateCommand("quota -vs | awk '{print $2}' | grep -o '[0-9][0-9]*' | head -1"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);

                return Convert.ToInt32(cmd.Result);
            }
        }

        /// <summary>
        /// Checks the allotted quota of a specific user on the BigDog cluster and determines if they have enough space to perform our tasks.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="minQuota">The amount of minimum quota (in GB) that a user must have to proceed.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <param name="quotaAmount">The amount of quota the user has in GB after checking on BigDog.</param>
        /// <returns>Returns a boolean value as to whether the user has sufficient quota or not.</returns>
        protected internal static bool CheckQuota(SshClient client, int minQuota, out string error, out int quotaAmount)
        {
            quotaAmount = 0;
            string byteType = CheckQuotaType(client, out error); // Check whether the quota is in MB or GB.

            // Now we need to get the quota size.
            if (string.IsNullOrEmpty(error)) { quotaAmount = CheckQuotaSize(client, out error); }

            // If the quota is in MB, we need to convert it. Otherwise, we are fine.
            if (string.IsNullOrEmpty(error) && byteType.Equals("M")) { quotaAmount = ConvertToGigabyte(quotaAmount); }

            // If they have less than 'minQuota' then we return an error telling them the problem and how to rectify it.
            if (quotaAmount < minQuota)
            {
                error = "You do not have the requisite amount of disk space (" + minQuota + "Gb) for us to safely run a general assembly "
                    + "job. Please contact the BigDog admin team to increase your quota. You currently have " + quotaAmount + "Gb space to use.";

                return false;
            }

            // They have at least the minimum quota.
            else
                return true;
        }

        /// <summary>
        /// Compresses specific files/directories. We recommend the following optional parameters: -yrq (Store symbolic links, recursively traverse the directory, and quite mode).
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="compressionSpeed">The speed of the compression where 9 is the slowest. Recommend: 9</param>
        /// <param name="outputName">The name of the resulting compression.</param>
        /// <param name="sourceDirectory">The directory that needs compressed.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <param name="parameters">Any optional parameters for the zip command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void ZipFiles(SshClient client, int compressionSpeed, string outputName, string sourceDirectory, out string error, string parameters = "")
        {
            // USAGE (optimal run): zip -9 -y -r -q file.zip folder/
            // -9 optimal compression speed
            // -y store symbolic links
            // -r recursively traverse the directory
            // -q quiet mode 
            using (var cmd = client.CreateCommand("zip " + "-" + compressionSpeed + " " + outputName + " " + sourceDirectory + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        /// <summary>
        /// Determines whether or not an assembler has completed successfully.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="successLog">Filename associated with a successful run.</param>
        /// <param name="jobUuid">An integer number representing the particular job ID via the website (key-value of the submitted job).</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>Returns a boolean value whether the assembler has successfully finished.</returns>
        protected internal static bool AssemblerSuccess(SshClient client, string successLog, int jobUuid, out string error)
        {
            ChangeDirectory(client, Locations.GetJobLogPath(jobUuid), out error);

            // Determine if the job has finished successfully by searching for the success log.
            using (var cmd = client.CreateCommand("find " + successLog))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);

                // We couldn't find the success file.
                if (cmd.Result.ToString().Contains("No such file or directory"))
                    return false;

                else
                    return true;
            }
        }

        /// <summary>
        /// Gets the current step of a particular job.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="workingDirectory">The particular assembler output directory.</param>
        /// <param name="jobUuid">An integer number representing the particular job ID via the website (key-value of the submitted job).</param>
        /// <param name="stepList">The list of steps for a particular assembler.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>Returns an integer representing the current step of a particular assembler or a -1 if there was an error.</returns>
        protected internal static int GetCurrentStep(SshClient client, string workingDirectory, int jobUuid, HashSet<Assembler> stepList, out string error)
        {
            // Change to the assembler output directory.
            ChangeDirectory(client, workingDirectory, out error);

            int currentStep = 1;

            // Run through the step list checking which files have been created and determine where the assembler is in the process.
            foreach (var item in stepList)
            {
                using (var cmd = client.CreateCommand("find " + item.filename + " | wc -l"))
                {
                    cmd.Execute();

                    string file = cmd.Result.ToString();

                    // File found. We don't break because there may be another step after this one.
                    if (!file.Contains("No such file or directory"))
                        currentStep = item.step;

                    // Base Case: For the first step, if we don't find anything, break the loop. The assembler hasn't started.
                    else if (item.step == 1 && file.Contains("No such file or directory"))
                        break;

                    // Error Case: If we come across an error or recieve a negative number, break the loop.
                    if (LinuxErrorHandling.CommandError(cmd, out error))
                        break;
                }
            }

            // Provided there was no error, return the current step back to the user.
            if (string.IsNullOrEmpty(error))
                return currentStep;

            else
                return -1;
        }

        #endregion

        #region Scheduler Commands

        /// <summary>
        /// Adds a job to SGE (scheduler).
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="logPath">The location of the output and error logs.</param>
        /// <param name="node">The node on which the job will execute.</param>
        /// <param name="jobName">A particular name we would like the job to be called.</param>
        /// <param name="error">Any error encountered by the command.</param>
        protected internal static void AddJobToScheduler(SshClient client, string logPath, string node, string jobName, out string error)
        {
            // USAGE: qsub -pe make 20 -V -e /tmp/Genome/ -o/tmp/Genome/ -b y -l hostname=compute-0-24 -N taylor1 ./HelloWorld
            // qsub -pe make 20 -V  -b y -l hostname=compute-0-24 -cwd -N taylor1 ./HelloWorld
            using (var cmd = client.CreateCommand("qsub -pe make 20 -V -e " + logPath + " -o " + logPath + " -b y -l hostname=" + node + "-N " + jobName + "./assemble.sh"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        /// <summary>
        /// Sets the job number the scheduler assigns a job once it has been added to the queue.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="SSHUser">The user who submitted the job to the scheduler.</param>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>Returns an integer representing the SGE job ID if successful or -1 if unsuccessful.</returns>
        protected internal static int SetJobNumber(SshClient client, string SSHUser, string jobName, out string error)
        {
            // USAGE: qstat -f -u "USERNAME" | grep "JOBNAME"
            // -f: Full Format
            // -u "[user]": Jobs for specific user
            // We want to get the job number (which is created at submit to the scheduler).
            using (var cmd = client.CreateCommand("qstat -f -u " + "\"" + SSHUser + "\"" + "| grep " + jobName))
            {
                cmd.Execute();

                // Grab only numbers, ignore the rest.
                string[] jobNumber = Regex.Split(cmd.Result, @"\D+");

                LinuxErrorHandling.CommandError(cmd, out error);

                // We return the second element [1] here because the first and last element of the array is always the empty "". Trimming
                // doesn't remove it. So we will always return the first element which corresponds to the job number.

                if (string.IsNullOrEmpty(error))
                    return Convert.ToInt32(jobNumber[1]);

                else
                {
                    error = "Failed to get the job ID for the job. Something went wrong with the scheduler. Please contact an administrator.";
                    return -1;
                }
            }
        }

        /// <summary>
        /// Grabs the current load of a specified node.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="node">Particular node on BigDog.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>The load of the node specified in a double format unless there is an error which returns a -1.</returns>
        protected internal static double GetNodeLoad(SshClient client, string node, out string error)
        {
            // qstat -f returns specific information about all nodes. We then grep for a single node and then print out the load of that node.
            using (var cmd = client.CreateCommand("qstat -f | grep " + node + " | awk '{print $4;}'"))
            {
                cmd.Execute();

                if (LinuxErrorHandling.CommandError(cmd, out error) == false)
                    return Convert.ToDouble(cmd.Result);

                else
                    return -1;
            }
        }

        // Returns true if the job is currently running or false if the job isn't running/if an error exists.
        /// <summary>
        /// Determines if a job is currently added to the SGE scheduler.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="sgeJobId">An integer number representing the particular job ID.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>Returns a boolean representing whether the job is currently in the job queue of SGE.</returns>
        protected internal static bool JobRunning(SshClient client, int sgeJobId, out string error)
        {
            // Grabs the entire list of jobs and then picks the one with our job ID and prints out its information.
            using (var cmd = client.CreateCommand("qstat -j " + sgeJobId))
            {
                cmd.Execute();

                // So long as there isn't an error, we know it has been added to the scheduler.
                if (LinuxErrorHandling.CommandError(cmd, out error) == false)
                {
                    if (cmd.Result.Contains("Following jobs do not exist:"))
                        return false;

                    else
                        return true;
                }

                // Need to come back to this to address better exception handling. This should be fine in the meantime.
                else
                {
                    throw new Exception();
                }
            }
        }

        /// <summary>
        /// Cancels the specified job in SGE.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="sgeJobId">An integer number representing the particular job ID.</param>
        /// <param name="error">Any error encountered by the command.</param>
        protected internal static void CancelJob(SshClient client, int sgeId, out string error)
        {
            // Deletes the job with ID sgeId in SGE.
            using (var cmd = client.CreateCommand("qdel " + sgeId))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        #endregion

        #region Auxiliary Commands

        /// <summary>
        /// Converts from MB to GB.
        /// </summary>
        /// <param name="megabyteSize">The number that needs converted to GB.</param>
        /// <returns>An integer converted from MB to GB.</returns>
        protected internal static int ConvertToGigabyte(int megabyteSize)
        {
            return megabyteSize * (1 / 1024);
        }

        /// <summary>
        /// Determines whether a directory has any content. Note, this doesn't consider . or ..
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="directory">The particular directory that needs to be checked.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>Returns a boolean value as to whether the directory is empty or not.</returns>
        protected internal static bool DirectoryHasFiles(SshClient client, string directory, out string error)
        {
            using (var cmd = client.CreateCommand("ls -A " + directory + " | wc -l"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);

                // The directory is not empty.
                if (Convert.ToInt32(cmd.Result) > 0)
                    return true;

                else
                    return false;
            }
        }

        /// <summary>
        /// Creates an sftp connection to a particular location. Note, this requires a public key.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="fileServerFtp">The url to the SFTP.</param>
        /// <param name="publicKeyLocation">Absolute path to the public key in order to authenticate to the SFTP.</param>
        /// <param name="error">Any error encountered by the command.</param>
        protected internal static void ConnectSFTP(SshClient client, string fileServerFtp, string publicKeyLocation, out string error)
        {
            // Initiate an SFTP connection with a particular public key (-i [key location]) to a particular sftp url.
            using (var cmd = client.CreateCommand("sftp -i " + publicKeyLocation + fileServerFtp))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        /// <summary>
        /// Using an existing SFTP connection, upload a file.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="fileLocation">The local path for the file that will be uploaded (probably the zipped completed file).</param>
        /// <param name="error">Any error encountered by the command.</param>
        protected internal static void SftpUploadFile(SshClient client, string fileLocation, out string error)
        {
            // Puts a file from the local machine onto the server we have connected with previously. (See "ConnectSFTP" method).
            using (var cmd = client.CreateCommand("put " + fileLocation))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        /// <summary>
        /// Changes the CWD to a specified directory.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="newDirectory">The path to the new directory.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <param name="parameters">Any optional parameters for the cd command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void ChangeDirectory(SshClient client, string newDirectory, out string error, string parameters = "")
        {
            // Changes directories with any optional parameters mentioned.
            using (var cmd = client.CreateCommand("cd " + newDirectory + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        /// <summary>
        /// Changes to a different node on BigDog.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="node">The new node's name. (For instance: compute-0-24)</param>
        /// <param name="error">Any error encountered by the command.</param>
        protected internal static void ChangeNode(SshClient client, string node, out string error)
        {
            // Changes to a specified node.
            using (var cmd = client.CreateCommand("ssh " + node))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="directoryPath">The new directory's name/location.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <param name="parameters">Any optional parameters for the mkdir command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void CreateDirectory(SshClient client, string directoryPath, out string error, string directoryParameters = "")
        {
            // Makes a new directory at [directoryPath] with any optional parameters.
            using (var cmd = client.CreateCommand("mkdir " + directoryPath + " " + directoryParameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        /// <summary>
        /// Removes a particular file/directory.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="path">The location of the file to be deleted.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <param name="parameters">Any optional parameters for the rm command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void RemoveFile(SshClient client, string path, out string error, string directoryParameters = "")
        {
            // Removes a particular file at [path] with any optional parameters. We don't do any permission checks but those should be caught by our commanderror method.
            using (var cmd = client.CreateCommand("rm " + path + " " + directoryParameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        /// <summary>
        /// Downloads a particular file with wget.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="URL">The URL of the particular file.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <param name="parameters">Any optional parameters for the wget command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void DownloadFile(SshClient client, string URL, out string error, string parameters = "")
        {
            // Download the file to the CWD unless specified by an optional parameter.
            using (var cmd = client.CreateCommand("wget " + URL + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        /// <summary>
        /// Changes the permissions of a particular file/directory.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="path">The path to the file/directory whose permissions need changed.</param>
        /// <param name="newPermissions">The new permissions in the form of octal notation.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <param name="parameters">Any optional parameters for the chmod command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void ChangePermissions(SshClient client, string path, string newPermissions, out string error, string parameters = "")
        {
            // USAGE: chmod 777 /tmp/test -R
            using (var cmd = client.CreateCommand("chmod " + " " + newPermissions + " " + path + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        #endregion
    }
}