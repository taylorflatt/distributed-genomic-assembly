using Renci.SshNet;
using Genome.Models;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using Renci.SshNet.Common;
using System.Net.Sockets;

/// <summary>
/// TODO: Output ANY error information to a log file with the user's details and session data. Should probably be done in the controller.
/// TODO: Get update with regards to permission issues when reading other user's directories for status updates.
/// </summary>
namespace Genome.Helpers
{
    public class SSHConfig
    {
        private int errorCount = 0;
        private string error = "";

        private static string COMPUTENODE1 = "compute-0-24";
        private static string COMPUTENODE2 = "compute-0-25";

        private string ip;
        private GenomeModel genomeModel;
        private string path;

        public SSHConfig(string ip, GenomeModel genomeModel, string path, out string error)
        {
            error = "";

            this.ip = ip;
            this.genomeModel = genomeModel;
            this.path = path;

            //// This is for TESTING PURPOSES ONLY. It is commented out otherwise.
            //// We want to cat <job_name>.o<job_number> | grep "Status" and see where we are.
            using (var sshClient = new SshClient("fake", genomeModel.SSHUser, genomeModel.SSHPass))
            {
                try
                {
                    sshClient.Connect();

                    string jobName = genomeModel.SSHUser.ToString() + genomeModel.uuid.ToString();
                    //string pattern = "step";

                    if (errorCount == 0) { CreateTestJob(sshClient, jobName, out error); }

                    if (errorCount == 0) { string jobNumber = GetJobNumber(sshClient, jobName, out error); }

                    //GetStatusOutput(sshClient, "[job_name].o[job_number]", pattern);
                }

                // SSH Connection couldn't be established.
                catch (SocketException e)
                {
                    error = "The SSH connection couldn't be established. " + e.Message;
                }

                // Authentication failure.
                catch(SshAuthenticationException e)
                {
                    error = "The credentials were entered incorrectly. " + e.Message;
                }

                // The SSH connection was dropped.
                catch(SshConnectionException e)
                {
                    error = "The connection was terminated unexpectedly. " + e.Message;
                }
            }
        }

        // Debug purposes only.
        private void CreateTestJob(SshClient client, string jobName, out string error)
        {
            // qsub -pe make 20 -V -e /tmp/Genome/ -o /tmp/Genome/ -b y -l hostname=compute-0-24 -N taylor1 ./HelloWorld
            using (var cmd = client.CreateCommand("qub -pe make 20 -V -b y -cwd -l hostname=" + COMPUTENODE1 + " -N " + jobName + " ./HelloWorld"))
            {
                cmd.Execute();

                CatchError(cmd, out error);
            }
        }

        // Will return TRUE if successful connection and commands all run or FALSE if ANY error is encountered.
        public bool CreateJob(out string error)
        {
            error = "";
            this.error = error;

            // Need to create a directory here that is unique to the user if it doesn't already exist. 
            // For instance: WORKINGDIRECTORY/Taylor/Job1 and /WORKINGDIRECTORY/Taylor/Job2 and so on.

            // Then we wget all the scripts and store them in the WORKINGDIRECTORY/Taylor/Job1/Scripts.

            // Then we call the WORKINGDIRECTORY/Taylor/Job1/Scripts/Scheduler.sh which will launch the init.sh script.

            // The init.sh script will contain all the basic logic to download the data and initiate the job on the assembler(s).
            using (var client = new SshClient(ip, genomeModel.SSHUser, genomeModel.SSHPass))
            {
                string localPath = "/tmp/Genome/Job" + genomeModel.uuid;
                string dataPath = localPath + "/Data"; 
                string configPath = localPath + "/Config"; 
                string outputPath = localPath + "/Output";
                string logPath = localPath + "/Logs";

                // The outward facing (FTP) path to where the scripts are stored for download.
                string initScriptUrl = "PATH TO THE LOCATION WHERE THE INIT SCRIPT WILL BE STORED.";
                string masurcaScriptUrl = "PATH TO THE LOCATION WHERE THE MASUCRA SCRIPT WILL BE STORED.";
                //string schedulerScriptUrl = "PATH TO THE LOCATION WHERE THE SCHEDULER SCRIPT WILL BE STORED.";

                string initName = "init" + genomeModel.uuid;
                string masurcaName = "masurca" + genomeModel.uuid;
                string schedulerName = "scheduler" + genomeModel.uuid;

                // Location we store the wget error log.
                string wgetLogParameter = "-O " + logPath + "wget.error";

                string jobNumber = "";
                string jobName = genomeModel.SSHUser.ToString() + genomeModel.uuid.ToString();               

                try
                {
                    client.Connect();

                    if (errorCount == 0) { CreateDirectory(client, localPath, out error, "-p"); }
                    if (errorCount == 0) { CreateDirectory(client, dataPath, out error, "-p"); }
                    if (errorCount == 0) { CreateDirectory(client, configPath, out error, "-p"); }
                    if (errorCount == 0) { CreateDirectory(client, outputPath, out error, "-p"); }
                    if (errorCount == 0) { CreateDirectory(client, logPath, out error, "-p"); }

                    if (errorCount == 0) { DownloadFile(client, initScriptUrl, out error, wgetLogParameter); }
                    if (errorCount == 0) { DownloadFile(client, masurcaScriptUrl, out error, wgetLogParameter); }
                    //if (errorCount == 0) { DownloadFile(client, schedulerScriptUrl, out error, wgetLogParameter); }

                    if (errorCount == 0) { ChangePermissions(client, localPath, "777", out error, "-R"); }

                    // Need to make sure we are in the location of the assemble.sh script that we need to reference in the AddJobToScheduler method.

                    if (errorCount == 0) { AddJobToScheduler(client, logPath, COMPUTENODE1, jobName, out error); }

                    if (errorCount == 0) { jobNumber = SetJobNumber(client, jobName, out error); }

                    // There were no errors.
                    if (errorCount == 0)
                    {
                        // Set the scheduler Job ID only if we are completely successful and the job has been added to the scheduler.
                        genomeModel.SGEJobId = Convert.ToInt32(jobNumber);

                        return true;
                    }

                    else
                    {
                        return false;
                    }
                }

                // SSH Connection couldn't be established.
                catch (SocketException e)
                {
                    error = "The SSH connection couldn't be established. " + e.Message;

                    return false;
                }

                // Authentication failure.
                catch (SshAuthenticationException e)
                {
                    error = "The credentials were entered incorrectly. " + e.Message;

                    return false;
                }

                // The SSH connection was dropped.
                catch (SshConnectionException e)
                {
                    error = "The connection was terminated unexpectedly. " + e.Message;

                    return false;
                }

                catch(Exception e)
                {
                    error = "There was an uncaught exception. " + e.Message;

                    return false;
                }
            }
        }

        // Here we will use our account (or if we have to...the user's or we can curl) to grab the status of the job.
        // Returns TRUE if the update was successful. Returns FALSE if something went wrong.

        // TODO: Need to get update from admins on the issue with regards to inability to access the home directories of users. Maybe we can 
        // get a working directory with access for everyone.

        // What we can do is set a WORKING DIRECTORY to be /tmp/Genome and then have the entire directory 777 so everyone has access. We then redirect all outputs to that location under unique folders and we will have access to those files.
        // Step 1: Create Unique folders for output (error logs and eventual output).
        // Step 2: chmod the directories to 777.
        // Step 3: Initiate the job with the specific pathing variables.
        public bool UpdateJobStatus(out string error)
        {
            error = "";
            this.error = error;

            // We want to cat <job_name>.o<job_number> | grep "Status" and see where we are.
            // Reference for key: http://www.jokecamp.com/blog/connecting-to-sftp-with-key-file-and-password-using-ssh-net/
            var keyFile = new PrivateKeyFile(@"[ABSOLUTE PATH OF OPENSSH PRIVATE PPK KEY]");
            var keyFiles = new[] { keyFile };
            var username = "[USERNAME FOR UPDATE ACCOUNT]"; // This is the account name we will use to run the updates for jobs.

            var methods = new List<AuthenticationMethod>();
            methods.Add(new PrivateKeyAuthenticationMethod(username, keyFiles));

            var connectionInfo = new ConnectionInfo(ip, 22, username, methods.ToArray());

            using (var client = new SshClient(connectionInfo))
            {
                try
                {
                    client.Connect();

                    string jobName = genomeModel.SSHUser.ToString() + genomeModel.uuid.ToString();

                    // This is the thing we search for in the output log file to see which step we are at. Maybe we have a 
                    // dictionary that keeps track of what each step is and the corresponding number to keep parsing easy.
                    // Or at each step we print STEPID=5 and we just cat file.log | grep "STEPID" and then just grab the last one and see 
                    // the number and then that is the current step.
                    string pattern = "step"; // This is the thing we search for in the output file to see our CURRENT step.

                    GetStatusOutput(client, jobName + ".o" + genomeModel.SGEJobId, pattern, out error);

                    if (errorCount == 0)
                    {
                        return true;
                    }

                    else
                    {
                        return false;
                    }
                }

                // SSH Connection couldn't be established.
                catch (SocketException e)
                {
                    error = "The SSH connection couldn't be established. " + e.Message;

                    return false;
                }

                // Authentication failure.
                catch (SshAuthenticationException e)
                {
                    error = "The credentials were entered incorrectly. " + e.Message;

                    return false;
                }

                // The SSH connection was dropped.
                catch (SshConnectionException e)
                {
                    error = "The connection was terminated unexpectedly. " + e.Message;

                    return false;
                }
            }
        }

        private void ChangeNode(SshClient client, string node, out string error)
        {
            // USAGE: ssh compute-0-24/25
            // Changes to the specific node that we are using.
            using (var cmd = client.CreateCommand("ssh " + node))
            {
                cmd.Execute();

                CatchError(cmd, out error);
            }
        }

        private void CreateDirectory(SshClient client, string directoryPath, out string error, string directoryParameters = "")
        {
            // USAGE: mkdir /tmp/Genome/tflatt1029/Logs -p
            using (var cmd = client.CreateCommand("mkdir " + directoryPath + " " + directoryParameters))
            {
                cmd.Execute();

                CatchError(cmd, out error);
            }
        }

        private void DownloadFile(SshClient client, string URL, out string error, string parameters = "")
        {
            using (var cmd = client.CreateCommand("wget " + URL + " " + parameters))
            {
                cmd.Execute();

                CatchError(cmd, out error);
            }
        }

        private void ChangePermissions(SshClient client, string absolutePath, string newPermissions, out string error, string parameters = "")
        {
            // USAGE: chmod 777 /tmp/test -R
            using (var cmd = client.CreateCommand("chmod " + " " + newPermissions + " " + absolutePath + " " + parameters))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }
        }

        // Add the job to the scheduler.
        private void AddJobToScheduler(SshClient client, string logPath, string node, string jobName, out string error)
        {
            //qsub - pe make 20 - V - e / tmp / Genome / -o / tmp / Genome / -b y - l hostname = compute - 0 - 24 - N taylor1./ HelloWorld
            using (var cmd = client.CreateCommand("qsub -pe make 20 -V -e " + logPath + " -o " + logPath + " -b y -l hostname=" + node + "-N " +  jobName + "./assemble.sh"))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }
        }

        private void SetJobNumber(SshClient client, string jobName, out string error)
        {
            // USAGE: qstat -f -u "USERNAME" | grep "JOBNAME"
            // -f: Full Format
            // -u "[user]": Jobs for specific user
            // We want to get the job number (which is created at submit to the scheduler).
            using (var cmd = client.CreateCommand("qstat -f -u " + "\"" + genomeModel.SSHUser + "\"" + "| grep " + jobName))
            {
                cmd.Execute();

                // Grab only numbers, ignore the rest.
                string[] jobNumber = Regex.Split(cmd.Result, @"\D+");

                CatchError(cmd, out error);

                // We return the second element [1] here because the first and last element of the array is always the empty "". Trimming
                // doesn't remove it. So we will always return the first element which corresponds to the job number.

                if (errorCount == 0)
                {
                    genomeModel.SGEJobId = Convert.ToInt32(jobNumber[1]);
                }

                else
                {
                    error = "Failed to get the job ID for the job. Something went wrong with the scheduler. Please contact an administrator.";
                }
            }
        }

        // This will change depending on how we approach doing the check for the status.
        private void GetStatusOutput(SshClient client, string file, string pattern, out string error)
        {
            // USAGE IN LINUX:        cat file1 | grep "Step"
            // USAGE IN ABOVE METHOD: GetStatusOutput(client, "[job_name].o[job_number]", "STEP");

            using (var cmd = client.CreateCommand("cat " + file + "| " + "grep " + pattern))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }

            // OR, we check for the files in the CWD and compare those to a dictionary of known steps and return the current step.
            // A quick and dirty way to check for this is to simply find a known file and if it exists, we are in that step or after that step.
            // Then we need to check for the next file until we reach an error (no file found or something like that).

            using (var cmd = client.CreateCommand("grep " + step1File + " | wc -l"))
            {
                cmd.Execute();

                if(Convert.ToInt32(cmd.Result.ToString()) > 0)
                {
                    //Step 1 achieved
                }

                CatchError(cmd, out error);
            }
        }

        private void CatchError(SshCommand cmd, out string error)
        {
            error = "";

            // There is an error return the error so we can display it to the user.
            if (!string.IsNullOrEmpty(cmd.Error))
            {
                errorCount++;
                error = cmd.Error;
            }
        }
    }
}