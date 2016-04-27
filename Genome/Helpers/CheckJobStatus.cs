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
        private const string  PUBLIC_KEY_LOCATION = "UNKNOWN";

        private static ConnectionInfo CreatePrivateKeyConnectionInfo()
        {
            var keyFile = new PrivateKeyFile(@"[ABSOLUTE PATH OF OPENSSH PRIVATE PPK KEY]");
            var keyFiles = new[] { keyFile };
            var username = "[USERNAME FOR UPDATE ACCOUNT]"; // This is the account name we will use to run the updates for jobs.

            var methods = new List<AuthenticationMethod>();
            methods.Add(new PrivateKeyAuthenticationMethod(username, keyFiles));

            return new ConnectionInfo(Locations.GetBigDogIp(), 22, username, methods.ToArray());
        }

        // This will be called by a scheduler on a timed basis.
        // In the main method where this is called, it will loop through all the job uuids that need to be run and then call this method 
        // successively. This allows me to use the SAME method for a single update of a batch update.
        // Returns a jobsToUpload = true if this job is ready to be uploaded.
        protected internal static void UpdateStatuses(int jobUuid, ref bool jobsToUpload, out string error)
        {
            error = "";

            // We want to cat <job_name>.o<job_number> | grep "Status" and see where we are.
            // Reference for key: http://www.jokecamp.com/blog/connecting-to-sftp-with-key-file-and-password-using-ssh-net/

            using (var client = new SshClient(CreatePrivateKeyConnectionInfo()))
            {
                try
                {
                    client.Connect();

                    using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
                    {
                        GenomeModel genomeModel = db.GenomeModels.Find(jobUuid);

                        #region Checking how many assemblers chosen

                        int numAssemblers = 0;

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
                            if (LinuxCommands.AssemblerSuccess(client, Locations.GetMasurcaErrorSuccessLogPath(jobUuid), jobUuid, out error))
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
        /// This will actually compress the data, initiate the sftp connection, and upload the files to the file server FTP. This should be called 
        /// AFTER the UpdateStatuses method. This also sets the download link for the file IF it uploaded successfully.
        /// </summary>
        /// <param name="client"> Current SSH session client.</param>
        /// <param name="uuid"> The id of the current job. </param>
        /// <param name="error"></param>
        protected internal static void UploadData(SshClient client, int uuid, out string error)
        {
            using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
            {
                GenomeModel genomeModel = db.GenomeModels.Find(uuid);

                LinuxCommands.ChangeDirectory(client, Locations.GetJobPath(uuid), out error);

                if (string.IsNullOrEmpty(error))
                {
                    genomeModel.OverallStatus = "Compressing Data";

                    db.SaveChanges();

                    LinuxCommands.ZipFiles(client, 9, Locations.GetCompressedDataPath(uuid), Locations.GetMasterPath(), out error, "-y -r");

                    if (!string.IsNullOrEmpty(error))
                        genomeModel.OverallStatus = "Error Compressing Data";

                    db.SaveChanges();
                }

                if (string.IsNullOrEmpty(error))
                {
                    genomeModel.OverallStatus = "Connecting to SFTP";

                    db.SaveChanges();

                    LinuxCommands.ConnectSFTP(client, Locations.GetFtpUrl(), PUBLIC_KEY_LOCATION, out error);

                    if (!string.IsNullOrEmpty(error))
                        genomeModel.OverallStatus = "Error connecting to SFTP";

                    db.SaveChanges();
                }

                if (string.IsNullOrEmpty(error))
                {
                    genomeModel.OverallStatus = "Uploading Data to SFTP";

                    db.SaveChanges();

                    LinuxCommands.SftpUploadFile(client, Locations.GetZipFileStoragePath(), out error);

                    if (!string.IsNullOrEmpty(error))
                        genomeModel.OverallStatus = "Error uploading data to SFTP";

                    if (string.IsNullOrEmpty(error))
                        genomeModel.DownloadLink = Locations.GetDataDownloadLink(genomeModel.CreatedBy, uuid);

                    db.SaveChanges();
                }
            }
        }

        #endregion
    }
}