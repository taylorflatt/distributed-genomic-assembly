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

        private string ip; // To login node.
        private GenomeModel genomeModel;
        private string path; // marty added this?

        public SSHConfig(string ip, GenomeModel genomeModel, string path, out string error)
        {
            error = "";

            this.ip = ip;
            this.genomeModel = genomeModel;
            this.path = path;

            //// This is for TESTING PURPOSES ONLY. It is commented out otherwise.
            using (var sshClient = new SshClient("fake", genomeModel.SSHUser, genomeModel.SSHPass))
            {
                try
                {
                    sshClient.Connect();

                    string jobName = genomeModel.SSHUser.ToString() + genomeModel.uuid.ToString();
                    //string pattern = "step";

                    if (errorCount == 0) { CreateTestJob(sshClient, jobName, out error); }

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

            // The init.sh script will contain all the basic logic to download the data and initiate the job on the assembler(s).
            using (var client = new SshClient(ip, genomeModel.SSHUser, genomeModel.SSHPass))
            {
                string workingDirectory = "/tmp/Genome/Job" + genomeModel.uuid;
                string dataPath = workingDirectory + "/Data"; 
                string configPath = workingDirectory + "/Config"; 
                string outputPath = workingDirectory + "/Output";
                string logPath = workingDirectory + "/Logs";

                // The outward facing (FTP) path to where the scripts are stored for download.
                string initScriptUrl = "PUBLIC PATH TO THE LOCATION WHERE THE INIT SCRIPT WILL BE STORED.";
                string masurcaScriptUrl = "PUBLIC PATH TO THE LOCATION WHERE THE MASUCRA SCRIPT WILL BE STORED.";
                //string schedulerScriptUrl = "PATH TO THE LOCATION WHERE THE SCHEDULER SCRIPT WILL BE STORED.";

                string initName = "init" + genomeModel.uuid;
                string masurcaName = "masurca" + genomeModel.uuid;
                //string schedulerName = "scheduler" + genomeModel.uuid;

                // Location we store the wget error log.
                string wgetLogParameter = "-O " + logPath + "wget.error";

                string jobName = genomeModel.SSHUser.ToString() + genomeModel.uuid.ToString();
                string node = COMPUTENODE1; // default         

                try
                {
                    client.Connect();

                    if (errorCount == 0) { CreateDirectory(client, workingDirectory, out error, "-p"); }
                    if (errorCount == 0) { CreateDirectory(client, dataPath, out error, "-p"); }
                    if (errorCount == 0) { CreateDirectory(client, configPath, out error, "-p"); }
                    if (errorCount == 0) { CreateDirectory(client, outputPath, out error, "-p"); }
                    if (errorCount == 0) { CreateDirectory(client, logPath, out error, "-p"); }

                    if (errorCount == 0) { DownloadFile(client, initScriptUrl, out error, wgetLogParameter); }
                    if (errorCount == 0) { DownloadFile(client, masurcaScriptUrl, out error, wgetLogParameter); }
                    //if (errorCount == 0) { DownloadFile(client, schedulerScriptUrl, out error, wgetLogParameter); }

                    if (errorCount == 0) { ChangePermissions(client, workingDirectory, "777", out error, "-R"); }

                    if(errorCount == 0)
                    {
                        // So COMPUTENODE2 has a smaller load, we want to use that instead.
                        if(GetNodeLoad(client, COMPUTENODE1) > GetNodeLoad(client, COMPUTENODE2))
                            node = COMPUTENODE2;

                        else
                            node = COMPUTENODE1;
                    }

                    // May need to investigate this. But I'm fairly certain you need to be in the current directory or we can always call it 
                    // via its absolute path but this is probably easier. It is late, but I am pretty sure you aren't able to do a qsub on 
                    // anything but the login node. So we might need to investigate the way in which we store the information.
                    if(errorCount == 0) { ChangeDirectory(client, configPath, out error); }

                    if (errorCount == 0) { AddJobToScheduler(client, logPath, node, jobName, out error); }

                    if (errorCount == 0) { SetJobNumber(client, jobName, out error); }

                    // There were no errors.
                    if (errorCount == 0)
                        return true;

                    else
                        return false;
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

        public bool UpdateJobStatus(out string error)
        {
            error = "";

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

                    GetCurrentStep(client, out error);

                    if (errorCount == 0)
                        return true;

                    else
                        return false;
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

        private void ChangeDirectory(SshClient client, string newDirectory, out string error, string parameters = "")
        {
            // USAGE: cd /opt/testDirectory
            using (var cmd = client.CreateCommand("cd " + newDirectory + " " + parameters))
            {
                cmd.Execute();

                CatchError(cmd, out error);
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
                    genomeModel.SGEJobId = Convert.ToInt32(jobNumber[1]);

                else
                    error = "Failed to get the job ID for the job. Something went wrong with the scheduler. Please contact an administrator.";
            }
        }

        // This will change depending on how we approach doing the check for the status.
        private int GetCurrentStep(SshClient client, out string error)
        {
            error = "";

            // A quick and dirty way to check for specific files is to create a dictionary of known files associated with the steps
            // and then successively run through each command to see the job is.

            var stepList = new List<KeyValuePair<int, string>>();
            stepList.Add(new KeyValuePair<int, string>(1, "FILENAME"));
            stepList.Add(new KeyValuePair<int, string>(2, "FILENAME"));
            stepList.Add(new KeyValuePair<int, string>(3, "FILENAME"));
            stepList.Add(new KeyValuePair<int, string>(4, "FILENAME"));
            stepList.Add(new KeyValuePair<int, string>(5, "FILENAME"));

            int currentStep = 0;

            foreach(var item in stepList)
            {
                using(var cmd = client.CreateCommand("ls -l | grep " + item.Value + " | wc -l"))
                {
                    cmd.Execute();

                    // File found.
                    if (Convert.ToInt32(cmd.Result.ToString()) > 0)
                        currentStep = item.Key;

                    CatchError(cmd, out error);
                }
            }

            return currentStep;
        }

        private int GetNodeLoad(SshClient client, string node)
        {
            // We only want to get the load average of the specific node. 
            using (var cmd = client.CreateCommand("qstat -f | grep " + node + " | awk '{print $4;}'"))
            {
                cmd.Execute();

                CatchError(cmd, out error);

                return Convert.ToInt32(cmd.Result);
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