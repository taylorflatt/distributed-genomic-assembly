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
    public class JobMaintenance
    {
        /// <summary>
        /// Creates the private key connection that we use to do the updates to jobs.
        /// </summary>
        /// <returns>A connection information needed to make an SSH connection.</returns>
        private static ConnectionInfo CreatePrivateKeyConnectionInfo()
        {
            // Reference for key: http://www.jokecamp.com/blog/connecting-to-sftp-with-key-file-and-password-using-ssh-net/
            var keyFile = new PrivateKeyFile(Accessors.PRIVATE_KEY_PATH);
            var keyFiles = new[] { keyFile };
            var username = "tflatt"; // This is the account name we will use to run the updates for jobs.

            var methods = new List<AuthenticationMethod>();
            methods.Add(new PrivateKeyAuthenticationMethod(Accessors.UPDATE_ACCT, keyFiles));

            return new ConnectionInfo(Accessors.BD_IP, Accessors.BD_PORT, username, methods.ToArray());
        }

        // This will be called by a scheduler on a timed basis.
        // In the main method where this is called, it will loop through all the job uuids that need to be run and then call this method 
        // successively. This allows me to use the SAME method for a single update of a batch update.
        // Returns a jobsToUpload = true if this job is ready to be uploaded.
        /// <summary>
        /// Updates the status of a single job. But it does not perform the upload if that needs to happen.
        /// </summary>
        /// <param name="jobUuid">The unique ID of the job.</param>
        /// <param name="jobsToUpload">Returns, by reference, a flag indicating whether or not the job needs to be uploaded.</param>
        /// <param name="error">Returns, by value, a string that indicates whether there has been an error. </param>
        protected internal static void UpdateStatus(int jobUuid, ref bool jobsToUpload)
        {
            using (var client = new SshClient(CreatePrivateKeyConnectionInfo()))
            {
                try
                {
                    client.Connect();

                    /// TODO: Modify the code to skip entire sections if they have already been completed. This will be based off the CURRENT STEP stored in the model data.
                    using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
                    {
                        GenomeModel genomeModel = db.GenomeModels.Find(jobUuid);

                        #region Checking how many assemblers chosen

                        // Get the overallstep list generated from the number of assemblers the user chose to use.
                        Hashtable overallStepList = StepDescriptions.GenerateOverallStepList(genomeModel.NumAssemblers);

                        // Get the masurca step list.
                        HashSet<Assembler> masurcaStepList = StepDescriptions.GetMasurcaStepList();

                        #endregion

                        #region Check if the job is currently running

                        // Checks to see if the job is running.
                        if (LinuxCommands.JobRunning(client, Convert.ToInt32(genomeModel.SGEJobId)))
                        {
                            genomeModel.OverallCurrentStep = StepDescriptions.GetRunningAssemblersStepNum();

                            // Now we need to check the status of EACH assembler, updating their statuses before we know the overall status.
                            if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                            {
                                #region Check if Masurca is running

                                if (LinuxCommands.DirectoryHasFiles(client, Accessors.GetMasurcaOutputPath(jobUuid)))
                                {
                                    // Now we need to check if it has completed those assembly jobs.
                                    int masurcaCurrentStep = LinuxCommands.GetCurrentStep(client, Accessors.GetMasurcaOutputPath(jobUuid), jobUuid, StepDescriptions.GetMasurcaStepList());

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
                            if (LinuxCommands.AssemblerSuccess(client, Accessors.GetMasurcaSuccessLogPath(jobUuid), jobUuid))
                            {
                                // If masurca isn't finished, we need to check it.
                                if (!genomeModel.MasurcaCurrentStep.Equals(StepDescriptions.GetMasurcaStepList().Count))
                                {
                                    int masurcaCurrentStep = StepDescriptions.GetMasurcaStepList().Last().step;

                                    genomeModel.MasurcaCurrentStep = masurcaCurrentStep;
                                    genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), masurcaCurrentStep);

                                    // Update the overall step list to move forward a spot.
                                    genomeModel.OverallCurrentStep++;
                                }
                            }

                            // Error.
                            else
                            {
                                // Don't re-assign values if we have already done it.
                                if (!genomeModel.MasurcaCurrentStep.Equals(StepDescriptions.GetMasurcaStepList().Count))
                                {
                                    int masurcaCurrentStep = StepDescriptions.GetMasurcaStepList().Last().step;

                                    genomeModel.MasurcaCurrentStep = masurcaCurrentStep;
                                    genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), masurcaCurrentStep) + " - Error"; // explicitly note an error for the description.

                                    // This may need to be investigated further. Not sure how the failure of a single assembler will have on the entire process. (Forking etc).
                                    genomeModel.OverallCurrentStep++;
                                }

                            }

                            /// NOW CHECK SGA.

                            /// NOW CHECK WGS.

                            // Update the status description.
                            genomeModel.OverallStatus = StepDescriptions.GetCurrentStepDescription(overallStepList, genomeModel.OverallCurrentStep);

                            // If ALL assemblers have finished in some capacity, then we need to move focus to uploading the data.
                            if(genomeModel.MasurcaCurrentStep.Equals(StepDescriptions.GetMasurcaStepList().Last().step))
                                jobsToUpload = true;

                            #endregion
                        }

                        db.SaveChanges(); // Save the changes to the model in the database.
                    }

                    #endregion
                }

                // SSH Connection couldn't be established.
                catch (SocketException e)
                {
                    LinuxErrorHandling.error = "The SSH connection couldn't be established. " + e.Message;
                }

                // Authentication failure.
                catch (SshAuthenticationException e)
                {
                    LinuxErrorHandling.error = "The credentials were entered incorrectly. " + e.Message;
                }

                // The SSH connection was dropped.
                catch (SshConnectionException e)
                {
                    LinuxErrorHandling.error = "The connection was terminated unexpectedly. " + e.Message;
                }
            }
        }

        #region Compile and ship data to FTP for download by user.

        /// <summary>
        /// This method completes all the final steps for a completed job. It compresses the data, initiates a SFTP connection, uploads the data to the file server, 
        /// and sets the download link provided it uploads properly. Note, this ought to be called after the UpdateStatus method.
        /// </summary>
        /// <param name="client">Current SSH session client.</param>
        /// <param name="uuid">An integer number representing the particular job ID via the website (key-value of the submitted job).</param>
        /// <param name="error">Any error encountered by the command.</param>
        protected internal static void UploadData(SshClient client, int uuid)
        {
            using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
            {
                // Pull the model data.
                GenomeModel genomeModel = db.GenomeModels.Find(uuid);

                // Move to the overall job directory. (NOTE: THIS WILL NOT WORK...NEED TO MAKE SURE I USE ABSOLUTE PATHS.
                LinuxCommands.ChangeDirectory(client, Accessors.GetJobPath(uuid));

                // Grab the unique list of steps for this particular model.
                Hashtable overallStepList = StepDescriptions.GenerateOverallStepList(genomeModel.NumAssemblers);

                #region Compress Data
                if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                {
                    // Get the compressing data step number.
                    int stepNum = StepDescriptions.GetCompressingDataStepNum(overallStepList.Count);

                    // Set the overall status compressing.
                    genomeModel.OverallStatus = StepDescriptions.GetCurrentStepDescription(overallStepList, stepNum);

                    db.SaveChanges();

                    // Compress Data.
                    LinuxCommands.ZipFiles(client, 9, Accessors.GetCompressedDataPath(uuid), Accessors.masterPath, "-y -r");

                    if (!string.IsNullOrEmpty(LinuxErrorHandling.error))
                    {
                        genomeModel.OverallStatus = StepDescriptions.COMPRESSION_ERROR;
                        db.SaveChanges();
                    }
                }

                #endregion

                #region Connect to SFTP
                if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                {
                    // Get the connecting to sftp data step number.
                    int stepNum = StepDescriptions.GetConnectingToSftpStepNum(overallStepList.Count);

                    // Set the overall status to connecting to sftp.
                    genomeModel.OverallStatus = StepDescriptions.GetCurrentStepDescription(overallStepList, stepNum);

                    db.SaveChanges();

                    // Connect to SFTP.
                    LinuxCommands.ConnectSftpToFtp(client, Accessors.FTP_URL, Accessors.PUBLIC_KEY_PATH);

                    if (!string.IsNullOrEmpty(LinuxErrorHandling.error))
                    {
                        genomeModel.OverallStatus = StepDescriptions.SFTP_CONNECTION_ERROR;
                        db.SaveChanges();
                    }
                }

                #endregion

                #region Upload Data and Set Download Link

                if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                {
                    // Get the upload data step number.
                    int stepNum = StepDescriptions.GetUploadDataStepNum(overallStepList.Count);

                    // Set the overall status to uploading data.
                    genomeModel.OverallStatus = StepDescriptions.GetCurrentStepDescription(overallStepList, stepNum);

                    db.SaveChanges();

                    // Upload files.
                    LinuxCommands.SftpUploadFile(client, Accessors.ZIP_STORAGE_PATH);

                    if (!string.IsNullOrEmpty(LinuxErrorHandling.error))
                        genomeModel.OverallStatus = StepDescriptions.UPLOAD_TO_FTP_ERROR;

                    else
                    {
                        genomeModel.DownloadLink = Accessors.GetDataDownloadLink(genomeModel.CreatedBy, uuid);
                        genomeModel.CompletedDate = DateTime.UtcNow; // Set the completed date of the job.
                        genomeModel.OverallStatus = StepDescriptions.FINAL_STEP;
                    }

                    db.SaveChanges();
                }

                #endregion
            }
        }

        #endregion
    }
}