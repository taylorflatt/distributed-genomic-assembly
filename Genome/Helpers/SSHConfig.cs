using Renci.SshNet;
using Genome.Models;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using Renci.SshNet.Common;
using System.Net.Sockets;
using Genome.Helpers;

/// <summary>
/// TODO: Output ANY error information to a log file with the user's details and session data. Should probably be done in the controller.
/// TODO: Get update with regards to permission issues when reading other user's directories for status updates.
/// </summary>
namespace Genome.Helpers
{
    public class SSHConfig
    {
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
            //using (var sshClient = new SshClient("fake", genomeModel.SSHUser, genomeModel.SSHPass))
            //{
            //    try
            //    {
            //        sshClient.Connect();

            //        string jobName = genomeModel.SSHUser.ToString() + genomeModel.uuid.ToString();
            //        //string pattern = "step";

            //        if (errorCount == 0) { CreateTestJob(sshClient, jobName, out error); }

            //        //GetStatusOutput(sshClient, "[job_name].o[job_number]", pattern);
            //    }

            //    // SSH Connection couldn't be established.
            //    catch (SocketException e)
            //    {
            //        error = "The SSH connection couldn't be established. " + e.Message;
            //    }

            //    // Authentication failure.
            //    catch(SshAuthenticationException e)
            //    {
            //        error = "The credentials were entered incorrectly. " + e.Message;
            //    }

            //    // The SSH connection was dropped.
            //    catch(SshConnectionException e)
            //    {
            //        error = "The connection was terminated unexpectedly. " + e.Message;
            //    }
            //}
        }

        //// Debug purposes only.
        //private void CreateTestJob(SshClient client, string jobName, out string error)
        //{
        //    // qsub -pe make 20 -V -e /tmp/Genome/ -o /tmp/Genome/ -b y -l hostname=compute-0-24 -N taylor1 ./HelloWorld
        //    using (var cmd = client.CreateCommand("qub -pe make 20 -V -b y -cwd -l hostname=" + COMPUTENODE1 + " -N " + jobName + " ./HelloWorld"))
        //    {
        //        cmd.Execute();

        //        CatchError(cmd, out error);
        //    }
        //}

        // Will return TRUE if successful connection and commands all run or FALSE if ANY error is encountered.
        public bool CreateJob(out string error)
        {
            error = "";

            // The init.sh script will contain all the basic logic to download the data and initiate the job on the assembler(s).
            using (var client = new SshClient(ip, genomeModel.SSHUser, genomeModel.SSHPass))
            {
                string workingDirectory = "/share/scratch/Genome/Job" + genomeModel.uuid;
                string dataPath = workingDirectory + "/Data";
                string configPath = workingDirectory + "/Config"; 
                string outputPath = workingDirectory + "/Output";
                string logPath = workingDirectory + "/Logs";

                // The outward facing (FTP) path to where the scripts are stored for download.
                string initScriptUrl = "PUBLIC PATH TO THE LOCATION WHERE THE INIT SCRIPT WILL BE STORED.";
                string masurcaScriptUrl = "PUBLIC PATH TO THE LOCATION WHERE THE MASUCRA SCRIPT WILL BE STORED.";

                string initName = "init" + genomeModel.uuid;
                string masurcaName = "masurca" + genomeModel.uuid;

                // Location we store the wget error log.
                string wgetLogParameter = "-O " + logPath + "wget.error";

                string jobName = genomeModel.SSHUser.ToString() + genomeModel.uuid.ToString();
                string node = COMPUTENODE1; // default         

                try
                {
                    client.Connect();

                    if (string.IsNullOrEmpty(error)) { LinuxCommands.CreateDirectory(client, workingDirectory, out error, "-p"); }
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.CreateDirectory(client, dataPath, out error, "-p"); }
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.CreateDirectory(client, configPath, out error, "-p"); }
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.CreateDirectory(client, outputPath, out error, "-p"); }
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.CreateDirectory(client, logPath, out error, "-p"); }

                    if (string.IsNullOrEmpty(error)) { LinuxCommands.DownloadFile(client, initScriptUrl, out error, wgetLogParameter); }
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.DownloadFile(client, masurcaScriptUrl, out error, wgetLogParameter); }

                    if (string.IsNullOrEmpty(error)) { LinuxCommands.ChangePermissions(client, workingDirectory, "777", out error, "-R"); }

                    if(string.IsNullOrEmpty(error))
                    {
                        // So COMPUTENODE2 has a smaller load, we want to use that instead.
                        if(LinuxCommands.GetNodeLoad(client, COMPUTENODE1, out error) > LinuxCommands.GetNodeLoad(client, COMPUTENODE2, out error))
                            node = COMPUTENODE2;

                        else
                            node = COMPUTENODE1;
                    }

                    // May need to investigate this. But I'm fairly certain you need to be in the current directory or we can always call it 
                    // via its absolute path but this is probably easier. It is late, but I am pretty sure you aren't able to do a qsub on 
                    // anything but the login node. So we might need to investigate the way in which we store the information.
                    if(string.IsNullOrEmpty(error)) { LinuxCommands.ChangeDirectory(client, outputPath, out error); }

                    if (string.IsNullOrEmpty(error)) { LinuxCommands.AddJobToScheduler(client, logPath, node, jobName, out error); }

                    if (string.IsNullOrEmpty(error)) { SetJobNumber(client, jobName, out error); }

                    // There were no errors.
                    if (string.IsNullOrEmpty(error))
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

                LinuxErrorHandling.CommandError(cmd, out error);

                // We return the second element [1] here because the first and last element of the array is always the empty "". Trimming
                // doesn't remove it. So we will always return the first element which corresponds to the job number.

                if (string.IsNullOrEmpty(error))
                    genomeModel.SGEJobId = Convert.ToInt32(jobNumber[1]);

                else
                    error = "Failed to get the job ID for the job. Something went wrong with the scheduler. Please contact an administrator.";
            }
        }
    }
}