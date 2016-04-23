using Renci.SshNet;
using Genome.Models;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using Renci.SshNet.Common;
using System.Net.Sockets;
using System.Linq;

// TODO: CREATE ANOTHER PARAMETER IN THE MODEL THAT REFLECTS THE DIFFERENT STATUSES OF EACH ASSEMBLER AS WELL AS THE OVERALL STATUS. THIS WILL ALL
// NEED TO BE CHANGED.
namespace Genome.Helpers
{
    public class CheckJobStatus
    {
        private string ip = "";
        private const string FILESERVER_FTP_URL = "UNKNOWN";
        private const string  PUBLIC_KEY_LOCATION = "UNKNOWN";
        //private GenomeAssemblyDbContext db = new GenomeAssemblyDbContext();

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

                        var stepList = new List<KeyValuePair<int, string>>();
                        stepList.Add(new KeyValuePair<int, string>(1, "FILENAME"));
                        stepList.Add(new KeyValuePair<int, string>(2, "FILENAME"));
                        stepList.Add(new KeyValuePair<int, string>(3, "FILENAME"));
                        stepList.Add(new KeyValuePair<int, string>(4, "FILENAME"));
                        stepList.Add(new KeyValuePair<int, string>(5, "FILENAME"));

                        var jobList = (from j in db.GenomeModels
                                       where j.JobStatus.Equals("Running")
                                       select (new { j.uuid, j.SGEJobId }));

                        foreach(var job in jobList)
                        {
                            // Check to see if the current job is actually running.
                            if(LinuxCommands.JobRunning(client, Convert.ToInt32(job.SGEJobId), out error))
                            {
                                // Get the current step of the job.
                                if (string.IsNullOrEmpty(error))
                                {
                                    // Get the model that corresponds to this particular job.
                                    GenomeModel genomeModel = db.GenomeModels.Find(job.uuid);

                                    // Set the current step.
                                    genomeModel.CurrentStep = GetCurrentStep(client, Convert.ToInt32(job.uuid), stepList, out error);

                                    // Save the changes to the model in the database.
                                    db.SaveChanges();
                                }
                            }

                            // The job isn't running.
                            else
                            {
                                // We now need to check to see if it has completed or if it has errored out.
                                CheckJobCompleted(client, job.SGEJobId, workingDirectory, out error);
                            }
                        }
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

        // This has NOT been tested yet.
        private bool CheckJobCompleted(SshClient client, int jobId, string workingDirectory, out string error)
        {
            // We are assuming that the job has completed running. So we need to check the error file in our log directory. If that log has 
            // ANYTHING in it, then we have an error and need to notify the user.

            // If the error log is empty, then we assume that it has completed successfully and we notify the user. We follow all the basic steps 
            // for both methods but change the notification message.

            // There is an error with the job. Aka there is information written to the error log.
            if (LinuxCommands.JobHasError(client, jobId, workingDirectory, out error))
            {
                string zipStoreLocation = workingDirectory + "Job" + jobId + "/Output/job" + jobId + ".zip";
                string jobLocation = workingDirectory + "Job" + jobId;

                // Let's compress the job (-y: store sym links, -r: travel recursively).
                if (string.IsNullOrEmpty(error)) { LinuxCommands.ZipFiles(client, 9, zipStoreLocation, jobLocation, out error, "-y -r"); }

                else
                {
                    using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
                    {
                        GenomeModel genomeModel = db.GenomeModels.Find(jobId);
                        genomeModel.JobStatus = "Error Packaging Data";
                        db.SaveChanges();
                    }

                }

                // Now we connect to the file server FTP
                if (string.IsNullOrEmpty(error)) { LinuxCommands.ConnectSFTP(client, FILESERVER_FTP_URL, PUBLIC_KEY_LOCATION, out error); }

                // Now we need to upload our zip file.
                if (string.IsNullOrEmpty(error))
                {
                    using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
                    {
                        GenomeModel genomeModel = db.GenomeModels.Find(jobId);
                        genomeModel.JobStatus = "Transferring";
                        db.SaveChanges();

                        LinuxCommands.SftpUploadFile(client, zipStoreLocation, out error);

                        genomeModel.JobStatus = "Completed";
                        db.SaveChanges();

                    }
                }

                // There was an error transferring the files.
                else
                {
                    using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
                    {
                        GenomeModel genomeModel = db.GenomeModels.Find(jobId);
                        genomeModel.JobStatus = "Error Transferring";
                        db.SaveChanges();
                    }
                }

                // Now we need to update the status of the job.
            }
        }

        

        // This will change depending on how we approach doing the check for the status.
        // Returns the current step of the job or -1 if there was an error encountered.
        private int GetCurrentStep(SshClient client, int jobUuid, List<KeyValuePair<int, string>> stepList, out string error)
        {
            error = "";

            // A quick and dirty way to check for specific files is to create a dictionary of known files associated with the steps
            // and then successively run through each command to see the job is.

            int currentStep = 0;

            // Here item1 = uuid and item2 = sgejobid.
            string workingDirectory = "/share/scratch/Genome/Job" + jobUuid;

            LinuxCommands.ChangeDirectory(client, workingDirectory, out error);

            foreach (var step in stepList)
            {
                using (var cmd = client.CreateCommand("ls -l | grep " + step.Value + " | wc -l"))
                {
                    cmd.Execute();

                    // File found.
                    if (Convert.ToInt32(cmd.Result.ToString()) > 0)
                        currentStep = step.Key;

                    if (LinuxErrorHandling.CommandError(cmd, out error) || Convert.ToInt32(cmd.Result.ToString()) <= 0)
                        break;
                }
            }

            if (string.IsNullOrEmpty(error))
                return currentStep;

            else
                return -1;
        }
    }
}