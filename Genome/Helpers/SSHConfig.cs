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
        private static string COMPUTENODE1 = "compute-0-24";
        private static string COMPUTENODE2 = "compute-0-25";

        private string ip; // To login node.
        private GenomeModel genomeModel;
        private string path; // marty added this?

        public SSHConfig(string ip, out string error)
        {
            error = "";
            this.ip = ip;
        }

        public bool VerifyQuota(string SSHUser, string SSHPass, out string error, out int quotaAmount)
        {
            quotaAmount = 0;
            error = "";

            using (var client = new SshClient(ip, SSHUser, SSHPass))
            {
                try
                {
                    client.Connect();

                    int minimumQuota = 50; // Minimum quota size (in Gb) the user can have.

                    if (string.IsNullOrEmpty(error) && LinuxCommands.CheckQuota(client, minimumQuota, out error, out quotaAmount))
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

                catch (Exception e)
                {
                    error = "There was an uncaught exception. " + e.Message;

                    return false;
                }
            }
        }

        // We attempt to create a directory 
        public bool VerifyPermissions(string SSHUser, string SSHPass, out string error)
        {
            error = "";

            using (var client = new SshClient(ip, SSHUser, SSHPass))
            {
                try
                {
                    client.Connect();

                    string testDirectory = "/share/scratch/tflatt/testPermissions";

                    LinuxCommands.CreateDirectory(client, testDirectory, out error);

                    // There is an error. Here we specifically overwrite the error with our own. Since ANY write error here is a problem.
                    if (!string.IsNullOrEmpty(error))
                    {
                        error = "You do not have sufficient permissions to write to the proper directories. Please contact the BigDog Linux team about addressing this problem.";

                        return false;
                    }

                    else
                    {
                        // We want to remove the directory we just created as a test. We recursively force the deletion.
                        LinuxCommands.RemoveFile(client, testDirectory, out error, "-rf");

                        return true;
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

                catch (Exception e)
                {
                    error = "There was an uncaught exception. " + e.Message;

                    return false;
                }
            }
        }

        public SSHConfig(string ip, GenomeModel genomeModel, string path, out string error)
        {
            error = "";

            this.ip = ip;
            this.genomeModel = genomeModel;
            this.path = path;
        }

        // Will return TRUE if successful connection and commands all run or FALSE if ANY error is encountered.
        public bool CreateJob(string initUrl, string masurcaUrl, out string error)
        {
            error = "";
            // UUID is not being assigned before it hits this method so we have a problem. We need to save it to the DB prior to hitting this method but that causes other problems.......need to look into this. We can probably find a work around by checking the db and seeing the previous uuid and just incrementing the previous uuid.

            // The init.sh script will contain all the basic logic to download the data and initiate the job on the assembler(s).
            using (var client = new SshClient(Locations.BD_IP, genomeModel.SSHUser, genomeModel.SSHPass))
            {
                int id = genomeModel.uuid;

                // Location we store the wget error log.
                string wgetLogParameter = "-O " + Locations.GetJobLogPath(id) + "wget.error";

                string jobName = genomeModel.SSHUser.ToString() + genomeModel.uuid.ToString();
                string node = COMPUTENODE1; // default  

                try
                {
                    client.Connect();

                    if (string.IsNullOrEmpty(error)) { LinuxCommands.CreateDirectory(client, Locations.GetJobPath(id), out error, "-p"); }
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.CreateDirectory(client, Locations.GetJobDataPath(id), out error, "-p"); }
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.CreateDirectory(client, Locations.GetJobConfigPath(id), out error, "-p"); }
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.CreateDirectory(client, Locations.GetJobOutputPath(id), out error, "-p"); }
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.CreateDirectory(client, Locations.GetJobLogPath(id), out error, "-p"); }

                    if (string.IsNullOrEmpty(error)) { LinuxCommands.ChangeDirectory(client, Locations.GetJobConfigPath(id), out error); }

                    // This command has NOT been tested.
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.DownloadFile(client, initUrl, out error, wgetLogParameter); }

                    // This command has NOT been tested.
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.DownloadFile(client, masurcaUrl, out error, wgetLogParameter); }

                    if (string.IsNullOrEmpty(error)) { LinuxCommands.ChangePermissions(client, Locations.GetJobPath(id), "777", out error, "-R"); }

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
                    if(string.IsNullOrEmpty(error)) { LinuxCommands.ChangeDirectory(client, Locations.GetJobOutputPath(id), out error); }

                    // This command has NOT been tested. We may need an absolute path rather than the relative one that we reference in this method since we switch directories to the output path directory.
                    if (string.IsNullOrEmpty(error)) { LinuxCommands.AddJobToScheduler(client, Locations.GetJobLogPath(id), node, jobName, out error); }

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