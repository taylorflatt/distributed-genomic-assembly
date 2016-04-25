using Renci.SshNet;
using System;
using System.Collections.Generic;

namespace Genome.Helpers
{
    public class LinuxCommands
    {
        // Returns the node load or -1 if the command was unsuccessful.
        protected internal static double GetNodeLoad(SshClient client, string node, out string error)
        {
            // We only want to get the load average of the specific node. 
            using (var cmd = client.CreateCommand("qstat -f | grep " + node + " | awk '{print $4;}'"))
            {
                cmd.Execute();

                if(LinuxErrorHandling.CommandError(cmd, out error) == false)
                    return Convert.ToDouble(cmd.Result);

                else
                    return -1;
            }
        }

        // We check the user's current quota against a minQuota passed into the method. We assume in Gb.
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
                error = "You do not have the requisite amount of disk space (" + minQuota + "G) for us to safely run a general assembly job. Please contact the BigDog admin team to increase your quota. You currently have " + quotaAmount + " space to use.";

                return false;
            }

            // They have at least the minimum quota.
            else
                return true;
        }

        // Check whether the quota is returning in Gb or Mb.
        protected internal static string CheckQuotaType(SshClient client, out string error)
        {
            // TODO: Parse out the \n from the return value.
            // First we need to get whether it is in gigabytes or megabytes.
            using (var cmd = client.CreateCommand("quota -vs | awk '{print $2}' | grep '[0-9][0-9]*' | grep -o '[a-zA-Z]'"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);

                return cmd.Result.ToString().Substring(0, 1);
            }
        }

        // Check the actual number size of the quota being returned regardless of the type (Gb or Mb).
        protected internal static int CheckQuotaSize(SshClient client, out string error)
        {
            // Make sure they are in their home directory so we only get a single number. Need to investigate this.
            ChangeDirectory(client, "~", out error);

            // Now we need to check the number.
            using (var cmd = client.CreateCommand("quota -vs | awk '{print $2}' | grep -o '[0-9][0-9]*' | head -1"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);

                return Convert.ToInt32(cmd.Result);
            }
        }

        // Convert FROM Mb TO Gb. 
        protected internal static int ConvertToGigabyte(int megabyteSize)
        {
            return megabyteSize * (1 / 1024);
        }

        // Returns true if the job is currently running or false if the job isn't running/if an error exists.
        protected internal static bool JobRunning(SshClient client, int sgeJobId, out string error)
        {
            // USAGE: qstat -j 5120
            using (var cmd = client.CreateCommand("qstat -j " + sgeJobId))
            {
                cmd.Execute();

                // So long as there isn't an error...
                if(LinuxErrorHandling.CommandError(cmd, out error) == false)
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

        protected internal static void ZipFiles(SshClient client, int compressionSpeed, string outputName, string sourceDirectory, out string error, string parameters = "")
        {
            // USAGE (optimal run): zip -9 -y -r -q file.zip folder/
            // -9 optimal compression speed
            // -y store symbolic links
            // -r recursively traverse the directory
            // -q quiet mode 
            using (var cmd = client.CreateCommand("zip " + "-" + compressionSpeed + " " + outputName + " " + sourceDirectory + " " + parameters ))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static bool DirectoryHasFiles(SshClient client, string directory, out string error)
        {
            using (var cmd = client.CreateCommand("ls -A " + directory + " | wc -l"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);

                // The directory has a file or directory
                if (Convert.ToInt32(cmd.Result) > 0)
                {
                    return true;
                }

                else
                    return false;
            }
        }

        protected internal static bool AssemblerSuccess(SshClient client, string successLog, string workingDirectory, out string error)
        {
            ChangeDirectory(client, workingDirectory + "/Logs", out error);

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

        // Returns the current step of the job or -1 if there was an error encountered.
        protected internal static int GetCurrentStep(SshClient client, string workingDirectory, int jobUuid, HashSet<Assembler> stepList, out string error)
        {
            // Change to the assembler output directory.
            ChangeDirectory(client, workingDirectory, out error);

            int currentStep = 1;

            foreach (var item in stepList)
            {
                using (var cmd = client.CreateCommand("find " + item.filename + " | wc -l"))
                {
                    cmd.Execute();

                    // File found.
                    if (Convert.ToInt32(cmd.Result.ToString()) > 0)
                        currentStep = item.step;

                    // Base Case: For the first step, if we don't find anything, break the loop. The assembler hasn't started.
                    else if (item.step == 1 && Convert.ToInt32(cmd.Result.ToString()) == 0)
                        break;

                    if (LinuxErrorHandling.CommandError(cmd, out error) || Convert.ToInt32(cmd.Result.ToString()) <= 0)
                        break;
                }
            }

            if (string.IsNullOrEmpty(error))
                return currentStep;

            else
                return -1;
        }

        protected internal static bool CheckJobComplete(SshClient client, int jobId, string workingDirectory, out string error)
        {
            ChangeDirectory(client, workingDirectory + "/Logs", out error);

            // USAGE: ls -l | grep e2014 | cat `awk '{print $9}'`
            // This effectively finds the error log in the cwd then prints the word count to stdout.
            using (var cmd = client.CreateCommand("ls -l | grep e" + jobId + " " + "| cat `awk '{print $9}'` | wc -w"))
            {
                cmd.Execute();

                // Provided there was no error, we now need to see if if there are any characters in that file. If ANY, we have a problem.
                if (LinuxErrorHandling.CommandError(cmd, out error) == false)
                {
                    // The error log is empty and so we had a successful run.
                    if (Convert.ToInt32(cmd.Result) > 0)
                    {
                        // We need to compress their data on bigdog prior to sending it.
                        // We need to download the data from bigdog back to the webserver under the public FTP.
                        // We need to make the download link accessible (or at least known) to the user.
                        // We need to now notify the user that their download is complete.
                        // We need to mark the job as completed (job status = completed/finished).

                        return true; // completed
                    }

                    // There is something in the error log, we had an unsuccessful run.
                    else
                    {
                        // We need to compress their data on bigdog prior to sending it.
                        // We need to donwload the data from bigdog back to the webserver under the public FTP.
                        // We need to add a README file or at least specify that the data located therein is not complete.
                        // We need to make the download link accessible (or at least known) to the user and write "Error" to the details page.
                        // We need to now notify the user that their download is complete and that an error occurred.
                        // We need to mark the job as error (job status = error).

                        return false; // not completed
                    }
                }

                else
                    return false; // error
            }
        }

        // Requires a public key.
        protected internal static void ConnectSFTP(SshClient client, string fileServerFtp, string publicKeyLocation, out string error, string parameters = "")
        {
            using (var cmd = client.CreateCommand("sftp -i " + publicKeyLocation + fileServerFtp))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static void SftpUploadFile(SshClient client, string fileLocation, out string error, string parameters = "")
        {
            using (var cmd = client.CreateCommand("put " + fileLocation))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static bool CheckMasurcaStep(SshClient client, string workingDirectory, string filename, out string error)
        {
            // Change to the masurca output directory.
            ChangeDirectory(client, workingDirectory, out error);

            using (var cmd = client.CreateCommand("find " + filename + " | wc -l"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);

                // If the file isn't in the masurca working directory, then it hasn't been created yet.
                if (Convert.ToInt32(cmd.Result) == 0)
                    return false;

                else
                    return true;

            }
        }

        protected internal static void ChangeDirectory(SshClient client, string newDirectory, out string error, string parameters = "")
        {
            // USAGE: cd /opt/testDirectory
            using (var cmd = client.CreateCommand("cd " + newDirectory + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static void ChangeNode(SshClient client, string node, out string error)
        {
            // USAGE: ssh compute-0-24/25
            // Changes to the specific node that we are using.
            using (var cmd = client.CreateCommand("ssh " + node))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static void CreateDirectory(SshClient client, string directoryPath, out string error, string directoryParameters = "")
        {
            // USAGE: mkdir /tmp/Genome/tflatt1029/Logs -p
            using (var cmd = client.CreateCommand("mkdir " + directoryPath + " " + directoryParameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        // Removes files OR directories (everything in linux is essentially considered a file....)
        protected internal static void RemoveFile(SshClient client, string path, out string error, string directoryParameters = "")
        {
            // USAGE: mkdir /tmp/Genome/tflatt1029/Logs -p
            using (var cmd = client.CreateCommand("rm " + path + " " + directoryParameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static void DownloadFile(SshClient client, string URL, out string error, string parameters = "")
        {
            // USAGE: wget www.google.com/file1 -o logoutput.error
            using (var cmd = client.CreateCommand("wget " + URL + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static void ChangePermissions(SshClient client, string absolutePath, string newPermissions, out string error, string parameters = "")
        {
            // USAGE: chmod 777 /tmp/test -R
            using (var cmd = client.CreateCommand("chmod " + " " + newPermissions + " " + absolutePath + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        // Add the job to the scheduler.
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
    }
}