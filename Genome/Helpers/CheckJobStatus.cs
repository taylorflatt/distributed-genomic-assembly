using Renci.SshNet;
using Genome.Models;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using Renci.SshNet.Common;
using System.Net.Sockets;
using System.Linq;

namespace Genome.Helpers
{
    public class CheckJobStatus
    {
        private string ip = "";
        private GenomeAssemblyDbContext db = new GenomeAssemblyDbContext();

        public CheckJobStatus(string ip, out string error)
        {
            error = "";

            this.ip = ip;
        }

        private ConnectionInfo CreatePrivateKeyConnectionInfo()
        {
            var keyFile = new PrivateKeyFile(@"[ABSOLUTE PATH OF OPENSSH PRIVATE PPK KEY]");
            var keyFiles = new[] { keyFile };
            var username = "[USERNAME FOR UPDATE ACCOUNT]"; // This is the account name we will use to run the updates for jobs.

            var methods = new List<AuthenticationMethod>();
            methods.Add(new PrivateKeyAuthenticationMethod(username, keyFiles));

            return new ConnectionInfo(ip, 22, username, methods.ToArray());

        }

        // Here we need to call the method that corresponds to updating ALL jobs marked 'running' in our database.
        // This will be called by our scheduler with updateAll as a true value.
        protected internal bool UpdateAllJobStatuses(out string error)
        {
            error = "";

            // We want to cat <job_name>.o<job_number> | grep "Status" and see where we are.
            // Reference for key: http://www.jokecamp.com/blog/connecting-to-sftp-with-key-file-and-password-using-ssh-net/

            using (var client = new SshClient(CreatePrivateKeyConnectionInfo()))
            {
                try
                {
                    client.Connect();

                    string workingDirectory = "/share/scratch/Genome/";

                    using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
                    {
                        // Find all of the jobs that are currently running and return their SGEJobId.
                        //var jobIds = (from j in db.GenomeModels
                        //              where j.JobStatus.Equals("Running")
                        //              select j.SGEJobId);

                        var jobInfo = (from j in db.GenomeModels
                                       where j.JobStatus.Equals("Running")
                                       select new { j.uuid, j.SGEJobId });

                        List<Tuple<int, int>> list = new List<Tuple<int, int>>();

                        foreach (var job in jobInfo)
                        {
                            list.Add(Tuple.Create(job.uuid, job.SGEJobId));
                        }

                        GetCurrentStep(client, list, out error);
                    }

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

            }
        }

        protected internal bool UpdateJobStatus(int uuid, out string error)
        {
            error = "";

            using (var client = new SshClient(CreatePrivateKeyConnectionInfo()))
            {
                try
                {
                    client.Connect();

                    string workingDirectory = "/share/scratch/Genome/Job" + uuid;

                    LinuxCommands.ChangeDirectory(client, workingDirectory + uuid, out error);
                    
                    //GetCurrentStep(client, out error);

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
            }
        }

        // This will change depending on how we approach doing the check for the status.
        // Returns the current step of the job or -1 if there was an error encountered.
        private int GetCurrentStep(SshClient client, List<Tuple<int, int>> jobInfo, out string error)
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

            // Here item1 = uuid and item2 = sgejobid.
            foreach(var job in jobInfo)
            {
                 string workingDirectory = "/share/scratch/Genome/Job" + job.Item1;

                LinuxCommands.ChangeDirectory(client, workingDirectory, out error);

                foreach (var item in stepList)
                {
                    using (var cmd = client.CreateCommand("ls -l | grep " + item.Value + " | wc -l"))
                    {
                        cmd.Execute();

                        // File found.
                        if (Convert.ToInt32(cmd.Result.ToString()) > 0)
                            currentStep = item.Key;

                        if (LinuxErrorHandling.CommandError(cmd, out error) || Convert.ToInt32(cmd.Result.ToString()) <= 0)
                            break;
                    }
                }

                // If the final file set has been created, we need to check to see if the job is still running or if it has completed.
                if (currentStep == stepList.Last().Key)
                {
                    // The job is NOT running we need to check the stderr file to make sure there aren't any errors. Otherwise we don't care.
                    if (LinuxCommands.JobRunning(client, job.Item2, out error) == false)
                        LinuxCommands.CheckJobError(client, job.Item2, workingDirectory, out error);

                    else
                        return currentStep;
                }
            }

            if (string.IsNullOrEmpty(error))
                return currentStep;

            else
                return -1;
        }
    }
}