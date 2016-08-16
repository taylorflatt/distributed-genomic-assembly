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
        /// Checks to see if a particular file is accessible remotely from the BigDog cluster using the wget spider command.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="url">The file URL that needs to be verified.</param>
        /// <returns>Returns a boolean value as to whether or not a URL is accessible from the BigDog cluster.</returns>
        /// <remarks>This may need to include additional greps in the future to catch more cases. These are the only two I could think to include. </remarks>
        protected internal static bool CheckDataAvailability(SshClient client, string url)
        {
            string logFile = "download.log";
            bool fileConnectable = false;

            List<string> cmdList = new List<string>();
            cmdList.Add("wget -S --spider " + url + " 2>" + logFile);
            cmdList.Add("grep 'Transfer complete' " + logFile + " | wc -l");
            cmdList.Add("grep 'HTTP/1.1 200 OK' " + logFile + " | wc -l");

            using (var wget = client.CreateCommand(cmdList[0]))
            {
                wget.Execute();
                LinuxErrorHandling.CommandError(wget);

                if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                {
                    for (int i = 1; i < cmdList.Count; i++)
                    {
                        using (var grep = client.CreateCommand(cmdList[i]))
                        {
                            grep.Execute();
                            LinuxErrorHandling.CommandError(grep);

                            if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                            {
                                // If the file is connectable, we can stop checking.
                                if (string.IsNullOrEmpty(LinuxErrorHandling.error) && Convert.ToInt32(grep.Result) > 0)
                                {
                                    fileConnectable = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Remove our log file after we are finished with it.
                RemoveFile(client, logFile);

                return fileConnectable;
            }
        }

        /// <summary>
        /// Determines whether or not the quota is in GB or MB.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <returns>Returns a string G or M corresponding to the size of the quota.</returns>
        /// TODO: We need to make sure that we are grabbing the correct location. I need to check with BigDog admin on how they do quota increases and if they carry over to all 
        /// directories or just specific areas. Right now we just grab the first letter of the first return but there can be multiple ones like "G\nG\nG\n" for instance or just one "G\n".
        protected internal static string CheckQuotaType(SshClient client)
        {
            // First we print out the quota then we grab only the quota column, then filter out everything that doesn't have a number in it and then remove all numbers from what is left.
            using (var cmd = client.CreateCommand("quota -vs | awk '{print $2}' | grep '[0-9][0-9]*' | grep -o '[a-zA-Z]'"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);

                if (!cmd.Result.ToString().Substring(0, 1).Equals("M") && !cmd.Result.ToString().Substring(0, 1).Equals("G"))
                    throw new Exception("There was undetermined behavior in the quota return value. We expected 'M' or 'G' but instead we got: " + cmd.Result.ToString());

                return cmd.Result.ToString().Substring(0, 1);
            }
        }

        /// <summary>
        /// Gets the size of quota the user has as an integer.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <returns>Returns an integer number representing the quota size in MB or GB.</returns>
        protected internal static int CheckQuotaSize(SshClient client)
        {
            // First we print out the quota then we grab only the quota column. Then we grab the number.
            using (var cmd = client.CreateCommand("quota -vs | awk '{print $2}' | grep -o '[0-9][0-9]*' | head -1"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);

                return Convert.ToInt32(cmd.Result);
            }
        }

        /// <summary>
        /// Checks the allotted quota of a specific user on the BigDog cluster.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="minQuota">The amount of minimum quota (in GB) that a user must have to proceed.</param>
        /// <returns>Returns the amount of space (in Gb) in quota the user has to work with.</returns>
        protected internal static int GetQuota(SshClient client, int minQuota)
        {
            int quotaAmount = 0;
            string byteType = CheckQuotaType(client); // Check whether the quota is in MB or GB.

            // Now we need to get the quota size.
            if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { quotaAmount = CheckQuotaSize(client); }

            // If the quota is in MB, we need to convert it. Otherwise, we are fine.
            if (string.IsNullOrEmpty(LinuxErrorHandling.error) && byteType.Equals("M")) { quotaAmount = ConvertToGigabyte(quotaAmount); }

            return quotaAmount;


        }

        /// <summary>
        /// Compresses specific files/directories. We recommend the following optional parameters: -yrq (Store symbolic links, recursively traverse the directory, and quite mode).
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="compressionSpeed">The speed of the compression where 9 is the slowest. Recommend: 9</param>
        /// <param name="outputName">The name of the resulting compression.</param>
        /// <param name="sourceDirectory">The directory that needs compressed.</param>
        /// <param name="parameters">Any optional parameters for the zip command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void ZipFiles(SshClient client, int compressionSpeed, string outputName, string sourceDirectory, string parameters = "")
        {
            // USAGE (optimal run): zip -9 -y -r -q file.zip folder/
            // -9 optimal compression speed
            // -y store symbolic links
            // -r recursively traverse the directory
            // -q quiet mode 
            using (var cmd = client.CreateCommand("zip " + "-" + compressionSpeed + " " + outputName + " " + sourceDirectory + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);
            }
        }

        /// <summary>
        /// Determines whether or not an assembler has completed successfully.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="successLog">Absolute path to a file that is the result of a successful run.</param>
        /// <returns>Returns a boolean value whether the assembler has successfully finished.</returns>
        protected internal static bool AssemblerSuccess(SshClient client, string successLog)
        {
            // Determine if the job has finished successfully by searching for the success log.
            using (var cmd = client.CreateCommand("find " + successLog))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);

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
        /// <param name="stepList">The list of steps for a particular assembler.</param>
        /// <returns>Returns an integer representing the current step of a particular assembler or a -1 if there was an error.</returns>
        protected internal static int GetCurrentStep(SshClient client, string workingDirectory, HashSet<Assembler> stepList)
        {
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
                    if (LinuxErrorHandling.CommandError(cmd))
                        break;
                }
            }

            // Provided there was no error, return the current step back to the user.
            if (string.IsNullOrEmpty(LinuxErrorHandling.error))
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
        /// <param name="workingDirectory">Directory the scheduler will execute out of.</param>
        /// <param name="logPath">The location of the output and error logs.</param>
        /// <param name="node">The node on which the job will execute.</param>
        /// <param name="jobName">A particular name we would like the job to be called.</param>
        /// <param name="initScript">The program that will be executed by the scheduler. This must be an absolute path beginninig with a /.</param>
        protected internal static void AddJobToScheduler(SshClient client, string workingDirectory, string logPath, string node, string jobName, string initScript)
        {
            // USAGE: qsub -pe make 20 -V -e /tmp/Genome/ -o/tmp/Genome/ -b y -l hostname=compute-0-24 -N taylor1 ./HelloWorld
            // qsub -pe make 20 -V  -b y -l hostname=compute-0-24 -cwd -N taylor1 ./HelloWorld
            using (var cmd = client.CreateCommand("qsub -pe make 20 -V -wd " + workingDirectory + " -e " + logPath + " -o " + logPath + " -b y -l hostname=" + node + " -N " + jobName + " " + initScript))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);
            }
        }

        /// <summary>
        /// Sets the job number the scheduler assigns a job once it has been added to the queue.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="SSHUser">The user who submitted the job to the scheduler.</param>
        /// <param name="jobName">The name of the job.</param>
        /// <returns>Returns an integer representing the SGE job ID if successful or -1 if unsuccessful.</returns>
        protected internal static int SetJobNumber(SshClient client, string SSHUser, string jobName)
        {
            // USAGE: qstat -f -u "USERNAME" | grep "JOBNAME"
            // -f: Full Format
            // -u "[user]": Jobs for specific user
            // We want to get the job number (which is created at submit to the scheduler).
            using (var cmd = client.CreateCommand("qstat -f -u " + "\"" + SSHUser + "\"" + " | grep " + SSHUser))
            {
                cmd.Execute();

                // Grab only numbers, ignore the rest.
                string[] jobNumber = Regex.Split(cmd.Result, @"\D+");

                LinuxErrorHandling.CommandError(cmd);

                // We return the second element [1] here because the first and last element of the array is always the empty "". Trimming
                // doesn't remove it. So we will always return the first element which corresponds to the job number.

                if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                    return Convert.ToInt32(jobNumber[1]);

                else
                {
                    LinuxErrorHandling.error = "Failed to get the job ID for the job. Something went wrong with the scheduler. Please contact an administrator.";
                    return -1;
                }
            }
        }

        /// <summary>
        /// Grabs the current load of a specified node.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="node">Particular node on BigDog.</param>
        /// <returns>The load of the node specified in a double format unless there is an error which returns a -1.</returns>
        protected internal static double GetNodeLoad(SshClient client, string node)
        {
            // qstat -f returns specific information about all nodes. We then grep for a single node and then print out the load of that node.
            using (var cmd = client.CreateCommand("qstat -f | grep " + node + " | awk '{print $4;}'"))
            {
                cmd.Execute();

                if (LinuxErrorHandling.CommandError(cmd) == false)
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
        /// <returns>Returns a boolean representing whether the job is currently in the job queue of SGE.</returns>
        protected internal static bool JobRunning(SshClient client, int sgeJobId)
        {
            // Grabs the entire list of jobs and then picks the one with our job ID and prints out its information.
            using (var cmd = client.CreateCommand("qstat -j " + sgeJobId))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);

                // Since NOT finding a job is considered "failing" on the part of the command, it will likely be erroneously caught by our error function. 
                if (LinuxErrorHandling.error.Contains("Following jobs do not exist:"))
                    return false;

                else if (string.IsNullOrEmpty(LinuxErrorHandling.error))
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
        protected internal static void CancelJob(SshClient client, int sgeId)
        {
            // Deletes the job with ID sgeId in SGE.
            using (var cmd = client.CreateCommand("qdel " + sgeId))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);
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
        /// <returns>Returns a boolean value as to whether the directory is empty or not.</returns>
        protected internal static bool DirectoryHasFiles(SshClient client, string directory)
        {
            using (var cmd = client.CreateCommand("ls -A " + directory + " | wc -l"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);

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
        protected internal static void ConnectSftpToFtp(SshClient client, string fileServerFtp, string publicKeyLocation)
        {
            // Initiate an SFTP connection with a particular public key (-i [key location]) to a particular sftp url.
            using (var cmd = client.CreateCommand("sftp -i " + publicKeyLocation + fileServerFtp))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);
            }
        }

        /// <summary>
        /// Using an existing SFTP connection, upload a file.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="fileLocation">The local path for the file that will be uploaded (probably the zipped completed file).</param>
        protected internal static void SftpUploadFile(SshClient client, string fileLocation, bool runAsBackground)
        {
            string linuxCmd;

            if (runAsBackground)
                linuxCmd = "put " + fileLocation + " &";
            else
                linuxCmd = "put " + fileLocation;

            // Puts a file from the local machine onto the server we have connected with previously. (See "ConnectSFTP" method).
            using (var cmd = client.CreateCommand(linuxCmd))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);
            }
        }

        /// <summary>
        /// Changes to a different node on BigDog.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="node">The new node's name. (For instance: compute-0-24)</param>
        protected internal static void ChangeNode(SshClient client, string node)
        {
            // Changes to a specified node.
            using (var cmd = client.CreateCommand("ssh " + node))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);
            }
        }

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="directoryPath">The new directory's name/location.</param>
        /// <param name="parameters">Any optional parameters for the mkdir command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void CreateDirectory(SshClient client, string directoryPath, string directoryParameters = "")
        {
            // Makes a new directory at [directoryPath] with any optional parameters.
            using (var cmd = client.CreateCommand("mkdir " + directoryPath + " " + directoryParameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);
            }
        }

        /// <summary>
        /// Removes a particular file/directory.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="path">The location of the file to be deleted.</param>
        /// <param name="parameters">Any optional parameters for the rm command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void RemoveFile(SshClient client, string path, string directoryParameters = "")
        {
            // Removes a particular file at [path] with any optional parameters. We don't do any permission checks but those should be caught by our commanderror method.
            using (var cmd = client.CreateCommand("rm " + path + " " + directoryParameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);
            }
        }

        /// <summary>
        /// Downloads a particular file with wget.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="URL">The URL of the particular file.</param>
        /// <param name="parameters">Any optional parameters for the wget command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void DownloadFile(SshClient client, string outputLocation, string url, string parameters = "")
        {
            using (var cmd = client.CreateCommand("wget --output-document=" + outputLocation + " " + url + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);
            }
        }

        /// <summary>
        /// Runs the Dos2Unix program which will strip away the dumb stuff Windows does to files when writing them. This is required prior to running premade scripts.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="scriptLocation">The absolute path of the script which you want to run Dos2Unix on.</param>
        /// <remarks>This program must be run on the config scripts created by the ConfigBuilder.</remarks>
        protected internal static void RunDos2Unix(SshClient client, string scriptLocation)
        {
            using (var cmd = client.CreateCommand("dos2unix " + scriptLocation))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);

                // For some reason, this is an error but in reality it is actually the script doing its job correctly. So we nullify the "error".
                if (LinuxErrorHandling.error.Equals("dos2unix: converting file " + scriptLocation + " to UNIX format ...\n"))
                    LinuxErrorHandling.error = "";
            }
        }

        /// <summary>
        /// Changes the permissions of a particular file/directory.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="path">The path to the file/directory whose permissions need changed.</param>
        /// <param name="newPermissions">The new permissions in the form of octal notation.</param>
        /// <param name="parameters">Any optional parameters for the chmod command. Every optional parameter needs to conform to typical Linux bash syntax (must be preceeded by a dash). </param>
        protected internal static void ChangePermissions(SshClient client, string path, string newPermissions, string parameters = "")
        {
            // USAGE: chmod 777 /tmp/test -R
            using (var cmd = client.CreateCommand("chmod " + " " + newPermissions + " " + path + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd);
            }
        }

        #endregion
    }
}