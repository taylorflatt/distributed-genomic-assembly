using Renci.SshNet;
using Genome.Models;
using System.Collections.Generic;
using System;
using Renci.SshNet.Common;
using System.Net.Sockets;
using System.Linq;
using System.Collections;

namespace Genome.Helpers
{
    public class CheckJobStatus
    {
        private string ip = "";
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
                        // Get all of the jobs that aren't completed.
                        var jobList = (from j in db.GenomeModels
                                       where !j.OverallStatus.Equals("Complete")
                                       select j.uuid);

                        /// Logic for our job check:
                        /// There are multiple steps we need to go through in each job which change based on the number of assemblers 
                        /// chosen which will potentially be different for each job.
                        /// 
                        /// (1) Program Queued
                        /// (2) Data Conversion
                        /// (3) Running Assemblers
                        /// (4) Finished Assembler 1 of n
                        /// (5->n-1) Finished Assembler 2 of n
                        /// (n) Finished Assembler n of n
                        /// (n + 1) Data Analysis
                        /// (n + 2) Uploading Data
                        /// (n + 3) Complete
                        ///  
                        /// So we will check the transition between each of these steps below only updating the value when we are certain
                        /// that we are at that step. I separate the checks into regions.
                        foreach (var job in jobList)
                        {
                            GenomeModel genomeModel = db.GenomeModels.Find(job);

                            int id = job; // current uuid.
                            int numAssemblers = 0;

                            #region Checking how many assemblers chosen

                            // Check which assemblers the user chose to use.
                            if (genomeModel.UseMasurca) numAssemblers++;
                            if (genomeModel.UseSGA) numAssemblers++;
                            if (genomeModel.UseWGS) numAssemblers++;

                            // Get the overallstep list generated from the number of assemblers the user chose to use.
                            Hashtable overallStepList = StepDescriptions.GenerateOverallStepList(numAssemblers);

                            // Get the masurca step list.
                            HashSet<Assembler> masurcaStepList = StepDescriptions.GetMasurcaStepList();

                            #endregion

                            #region Check if the job is currently running

                            // Checks to see if the job is running.
                            if (LinuxCommands.JobRunning(client, Convert.ToInt32(genomeModel.SGEJobId), out error))
                            {
                                // Now we need to check the status of EACH assembler, updating their statuses before we know the overall status.
                                if (string.IsNullOrEmpty(error))
                                {
                                    #region Check if Masurca is running

                                    if (LinuxCommands.DirectoryHasFiles(client, Locations.GetMasurcaOutputPath(id), out error))
                                    {
                                        // Now we need to check if it has completed those assembly jobs.
                                        int masurcaCurrentStep = LinuxCommands.GetCurrentStep(client, Locations.GetMasurcaOutputPath(id), id, StepDescriptions.GetMasurcaStepList(), out error);

                                        // Now we need to set the step/status values for masurca.
                                        if (masurcaCurrentStep != -1)
                                        {
                                            genomeModel.MasurcaCurrentStep = masurcaCurrentStep;
                                            genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), masurcaCurrentStep);
                                        }
                                    }

                                    #endregion

                                    #region Check if SGA is running.
                                    // Similar code to the masurca check.
                                    #endregion

                                    #region Check if WGS is running.
                                    // Similar code to the masurca check.
                                    #endregion
                                }

                            }

                            #endregion

                            #region Check if the job has completed successfully or errored out

                            // The job isn't running according to the scheduler. So it finished either successfully or errored out.
                            else
                            {
                                #region Check if the assemblers completed successfully

                                // Now I need to go through all the assemblers and see if they exited successfully or not.
                                if (LinuxCommands.AssemblerSuccess(client, Locations.GetMasurcaErrorSuccessLogPath(id), workingDirectory, out error))
                                {
                                    int masurcaCurrentStep = StepDescriptions.GetMasurcaStepList().Last().step;

                                    genomeModel.MasurcaCurrentStep = masurcaCurrentStep;
                                    genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), masurcaCurrentStep);
                                }

                                else
                                {
                                    int masurcaCurrentStep = StepDescriptions.GetMasurcaStepList().Last().step;

                                    genomeModel.MasurcaCurrentStep = masurcaCurrentStep;
                                    genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), masurcaCurrentStep) + " - Error"; // explicitly note an error for the description.
                                }

                                #endregion

                                #region Compile and ship data to FTP for download by user.

                                /// PLEASE NOTE:
                                /// This is a step that can be heavily revised. Currently, each job will take its time to upload its data if 
                                /// it has been deemed to be finished and in need to upload. To refactor this, it might be prudent to instead 
                                /// offload a list of jobs that need uploaded to the server that can be processed AFTER the status updates have 
                                /// finished. We can even methodize it and run that immediately after this scheduled task. That would probably 
                                /// be the best solution. For right now, I will keep this AS-IS because methodizing it would involve essentially 
                                /// copying and pasting.
                                /// 
                                /// More to the point, these statuses won't even be seen by the user because they will be done in succession.
                                /// So they NEED to be move to another method that will actually perform their functions individually.

                                bool hasUploaded = false;

                                if (string.IsNullOrEmpty(error))
                                    LinuxCommands.ChangeDirectory(client, Locations.GetJobPath(id), out error);

                                if (string.IsNullOrEmpty(error))
                                {
                                    LinuxCommands.ZipFiles(client, 9, Locations.GetCompressedDataPath(id), Locations.GetMasterPath(), out error, "-y -r");

                                    if (string.IsNullOrEmpty(error))
                                        genomeModel.OverallStatus = "Compressing Data";

                                    else
                                        genomeModel.OverallStatus = "Error Compressing Data";
                                }

                                if (string.IsNullOrEmpty(error))
                                {
                                    LinuxCommands.ConnectSFTP(client, Locations.GetFtpUrl(), PUBLIC_KEY_LOCATION, out error);

                                    if (string.IsNullOrEmpty(error))
                                        genomeModel.OverallStatus = "Connecting to SFTP";

                                    else
                                        genomeModel.OverallStatus = "Error connecting to SFTP";
                                }

                                if (string.IsNullOrEmpty(error))
                                {
                                    genomeModel.OverallStatus = "Uploading Data to SFTP";

                                    LinuxCommands.SftpUploadFile(client, Locations.GetZipFileStoragePath(), out error);

                                    if (!string.IsNullOrEmpty(error))
                                        genomeModel.OverallStatus = "Error uploading data to SFTP";
                                }
                                    #endregion
                            }
                        }

                        #endregion

                        // Save the changes to the model in the database.
                        db.SaveChanges();
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
    }
}