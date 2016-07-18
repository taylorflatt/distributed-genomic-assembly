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
        /// <summary>
        /// Creates the private key connection that we use to do the updates to jobs.
        /// </summary>
        /// <returns>A connection information needed to make an SSH connection.</returns>
        private static ConnectionInfo CreatePrivateKeyConnectionInfo()
        {
            // Reference for key: http://www.jokecamp.com/blog/connecting-to-sftp-with-key-file-and-password-using-ssh-net/
            var keyFile = new PrivateKeyFile(@"[ABSOLUTE PATH OF OPENSSH PRIVATE PPK KEY]");
            var keyFiles = new[] { keyFile };
            var username = "[USERNAME FOR UPDATE ACCOUNT]"; // This is the account name we will use to run the updates for jobs.

            var methods = new List<AuthenticationMethod>();
            methods.Add(new PrivateKeyAuthenticationMethod(username, keyFiles));

            return new ConnectionInfo(Locations.BD_IP, 22, username, methods.ToArray());
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
        protected internal static void UpdateStatus(int jobUuid, ref bool jobsToUpload, out string error)
        {
            error = "";

            using (var client = new SshClient(CreatePrivateKeyConnectionInfo()))
            {
                try
                {
                    client.Connect();

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
                        if (LinuxCommands.JobRunning(client, Convert.ToInt32(genomeModel.SGEJobId), out error))
                        {
                            // Now we need to check the status of EACH assembler, updating their statuses before we know the overall status.
                            if (string.IsNullOrEmpty(error))
                            {
                                #region Check if Masurca is running

                                if (LinuxCommands.DirectoryHasFiles(client, Locations.GetMasurcaOutputPath(jobUuid), out error))
                                {
                                    // Now we need to check if it has completed those assembly jobs.
                                    int masurcaCurrentStep = LinuxCommands.GetCurrentStep(client, Locations.GetMasurcaOutputPath(jobUuid), jobUuid, StepDescriptions.GetMasurcaStepList(), out error);

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
                            if (LinuxCommands.AssemblerSuccess(client, Locations.GetMasurcaSuccessLogPath(jobUuid), jobUuid, out error))
                            {
                                int masurcaCurrentStep = StepDescriptions.GetMasurcaStepList().Last().step;

                                genomeModel.MasurcaCurrentStep = masurcaCurrentStep;
                                genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), masurcaCurrentStep);
                            }

                            // Error.
                            else
                            {
                                int masurcaCurrentStep = StepDescriptions.GetMasurcaStepList().Last().step;

                                genomeModel.MasurcaCurrentStep = masurcaCurrentStep;
                                genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), masurcaCurrentStep) + " - Error"; // explicitly note an error for the description.
                            }

                            // Add the job to the list of jobs to upload to the file server FTP.
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
                    error = "The SSH connection couldn't be established. " + e.Message;
                }

                // Authentication failure.
                catch (SshAuthenticationException e)
                {
                    error = "The credentials were entered incorrectly. " + e.Message;
                }

                // The SSH connection was dropped.
                catch (SshConnectionException e)
                {
                    error = "The connection was terminated unexpectedly. " + e.Message;
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
        protected internal static void UploadData(SshClient client, int uuid, out string error)
        {
            using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
            {
                // Pull the model data.
                GenomeModel genomeModel = db.GenomeModels.Find(uuid);

                // Move to the overall job directory.
                LinuxCommands.ChangeDirectory(client, Locations.GetJobPath(uuid), out error);

                // Grab the unique list of steps for this particular model.
                Hashtable overallStepList = StepDescriptions.GenerateOverallStepList(genomeModel.NumAssemblers);

                #region Compress Data
                if (string.IsNullOrEmpty(error))
                {
                    // Get the compressing data step number.
                    int stepNum = StepDescriptions.GetCompressingDataStepNum(overallStepList.Count);

                    // Set the overall status compressing.
                    genomeModel.OverallStatus = StepDescriptions.GetCurrentStepDescription(overallStepList, stepNum);

                    db.SaveChanges();

                    // Compress Data.
                    LinuxCommands.ZipFiles(client, 9, Locations.GetCompressedDataPath(uuid), Locations.GET_MASTER_PATH, out error, "-y -r");

                    if (!string.IsNullOrEmpty(error))
                    {
                        genomeModel.OverallStatus = StepDescriptions.COMPRESSION_ERROR;
                        db.SaveChanges();
                    }
                }

                #endregion

                #region Connect to SFTP
                if (string.IsNullOrEmpty(error))
                {
                    // Get the connecting to sftp data step number.
                    int stepNum = StepDescriptions.GetConnectingToSftpStepNum(overallStepList.Count);

                    // Set the overall status to connecting to sftp.
                    genomeModel.OverallStatus = StepDescriptions.GetCurrentStepDescription(overallStepList, stepNum);

                    db.SaveChanges();

                    // Connect to SFTP.
                    LinuxCommands.ConnectSFTP(client, Locations.FTP_URL, Locations.PUBLIC_KEY_PATH, out error);

                    if (!string.IsNullOrEmpty(error))
                    {
                        genomeModel.OverallStatus = StepDescriptions.SFTP_CONNECTION_ERROR;
                        db.SaveChanges();
                    }
                }

                #endregion

                #region Upload Data and Set Download Link

                if (string.IsNullOrEmpty(error))
                {
                    // Get the upload data step number.
                    int stepNum = StepDescriptions.GetUploadDataStepNum(overallStepList.Count);

                    // Set the overall status to uploading data.
                    genomeModel.OverallStatus = StepDescriptions.GetCurrentStepDescription(overallStepList, stepNum);

                    db.SaveChanges();

                    // Upload files.
                    LinuxCommands.SftpUploadFile(client, Locations.ZIP_STORAGE_PATH, out error);

                    if (!string.IsNullOrEmpty(error))
                        genomeModel.OverallStatus = StepDescriptions.UPLOAD_TO_FTP_ERROR;

                    else
                    {
                        genomeModel.DownloadLink = Locations.GetDataDownloadLink(genomeModel.CreatedBy, uuid);
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