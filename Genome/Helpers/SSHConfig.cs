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
            using (var cmd = client.CreateCommand("qub -pe make 20 -V -b y -cwd -l hostname=" + COMPUTENODE1 + " -N " + jobName + " ./HelloWorld"))
            {
                cmd.Execute();

                CatchError(cmd, out error);
            }
        }


        // Here we will use our account (or if we have to...the user's or we can curl) to grab the status of the job.
        // Returns TRUE if the update was successful. Returns FALSE if something went wrong.

        // TODO: Need to get update from admins on the issue with regards to inability to access the home directories of users. Maybe we can 
        // get a working directory with access for everyone.
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

                    string jobNumber = "";

                    if (errorCount == 0) { CreateTestJob(client, jobName, out error); }
                    if (errorCount == 0) { jobNumber = GetJobNumber(client, jobName, out error); }
                    if (errorCount == 0) { GetStatusOutput(client, jobName + ".o" + jobNumber, pattern, out error); }

                    if (errorCount == 0)
                    {
                        // Only if successful do we set the UUID to the BigDog job ID.
                        genomeModel.uuid = Convert.ToInt32(jobNumber);

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

        // Will return TRUE if successful connection and commands all run or FALSE if ANY error is encountered.
        public bool CreateConnection(out string error)
        {
            error = "";
            this.error = error;

            // Need to create a directory here that is unique to the user if it doesn't already exist. 
            // For instance: WORKINGDIRECTORY/Taylor/Job1 and /WORKINGDIRECTORY/Taylor/Job2 and so on.

            // Then we wget all the scripts and store them in the WORKINGDIRECTORY/Taylor/Job1/Scripts.

            // Then we call the WORKINGDIRECTORY/Taylor/Job1/Scripts/Scheduler.sh which will launch the init.sh script.

            // The init.sh script will contain all the basic logic to download the data and initiate the job on the assembler(s).
            using (var sshClient = new SshClient(ip, genomeModel.SSHUser, genomeModel.SSHPass))
            {
                string localPath = "Job" + genomeModel.uuid;
                string dataPath = localPath + "/Data"; // Added dataPath variable - MG
                string configPath = localPath + "/Config"; // Added configPath variable - MG
                string outputPath = localPath + "/Output"; // Added outputPath variable - MG
                string initName = "init" + genomeModel.uuid;
                string masurcaName = "masurca" + genomeModel.uuid;
                string schedulerName = "scheduler" + genomeModel.uuid;

                try
                {
                    sshClient.Connect();

                    if (errorCount == 0) { CreateDirectories(sshClient, localPath, dataPath, configPath, outputPath, out error); }

                    if (errorCount == 0) { UploadInitScript(sshClient, path, initName, configPath, out error); }
                    if (errorCount == 0) { UploadMasurcaScript(sshClient, path, masurcaName, configPath, out error); }
                    if (errorCount == 0) { UploadSchedulerScript(sshClient, path, schedulerName, configPath, out error); }

                    if (errorCount == 0) { ChangePermissions(sshClient, localPath, out error); }

                    if (errorCount == 0) { AddJobToScheduler(sshClient, out error); }

                    // There were no errors.
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

        // Create the job directory.
        // Added 3 new parameters -MG
        private void CreateDirectories(SshClient client, string localPath, string dataPath, string configPath, string outputPath, out string error)
        {
            using (var cmd = client.CreateCommand("mkdir -p " + localPath))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }

            // Added following commands -MG
            using (var cmd = client.CreateCommand("mkdir -p " + dataPath))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }

            using (var cmd = client.CreateCommand("mkdir -p " + configPath))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }

            using (var cmd = client.CreateCommand("mkdir -p " + outputPath))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }
        }

        // Upload the init script (initializes all other assembler scripts and grabs data at runtime).
        private void UploadInitScript(SshClient client, string path, string initName, string configPath, out string error)
        {
            using (var cmd = client.CreateCommand("wget -O " + configPath + "/" + initName + ".sh " + path))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }
        }

        // Upload the Masurca assembler script.
        private void UploadMasurcaScript(SshClient client, string path, string masurcaName, string configPath, out string error)
        {
            using (var cmd = client.CreateCommand("wget -O " + configPath + "/" + masurcaName + ".txt " + path))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }
        }

        // Upload the scheduler script.
        private void UploadSchedulerScript(SshClient client, string path, string schedulerName, string configPath, out string error)
        {
            using (var cmd = client.CreateCommand("wget -0 " + configPath + "/" + schedulerName + ".sh " + path))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }
        }

        // Change the entire directory's contents to R/W/X.
        private void ChangePermissions(SshClient client, string localPath, out string error)
        {
            using (var cmd = client.CreateCommand("chmod -R +rwx " + localPath))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }
        }

        // Add the job to the scheduler.
        private void AddJobToScheduler(SshClient client, out string error)
        {
            using (var cmd = client.CreateCommand("./scheduler.sh"))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }
        }

        // Run init script, since scheduler is not implemented currently work in progress
        private void RunInitScript(SshClient client, string initName, string configPath, out string error)
        {
            using (var cmd = client.CreateCommand("./scheduler.sh"))
            {
                cmd.Execute();
                CatchError(cmd, out error);
            }
        }

        private string GetJobNumber(SshClient client, string jobName, out string error)
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
                    //error = errorOutput;
                    return jobNumber[1];
                }

                else
                {
                    //error = errorOutput;
                    return "failed";
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
        }

        // Cooked method for demo
        //private void RunDemo(SshClient client)
        //{
        //    using (var cmd = client.CreateCommand("cd ~/Demo/Output && /share/bio/masurca/bin/masurca ~/Demo/Config/config.txt"))
        //    {
        //        cmd.Execute();
        //        CatchError(cmd, out error);
        //    }
        //    using (var cmd = client.CreateCommand("cd ~/Demo/Output && ./test.sh"))
        //    {
        //        cmd.Execute();
        //        CatchError(cmd, out error);
        //    }
        //    client.Disconnect();
        //}

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