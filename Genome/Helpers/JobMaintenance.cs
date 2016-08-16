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

        /// <summary>
        /// Updates the status of a single job. But it does not perform the upload if that needs to happen.
        /// </summary>
        /// <param name="genomeModel">The model of the particular job.</param>
        protected internal static void UpdateStatus(GenomeModel genomeModel)
        {
            using (var client = new SshClient(CreatePrivateKeyConnectionInfo()))
            {
                try
                {
                    client.Connect();

                    /// TODO: Modify the code to skip entire sections if they have already been completed. This will be based off the CURRENT STEP stored in the model data.
                    using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
                    {
                        #region Checking how many assemblers chosen

                        // Get the step lists.
                        Hashtable overallStepList = StepDescriptions.GenerateOverallStepList(genomeModel.OverallStepSize);
                        HashSet<Assembler> masurcaStepList = StepDescriptions.GetMasurcaStepList();

                        #endregion

                        #region Check if the job is currently running

                        /////////////////////////////////
                        /// DEBUGGING WITHOUT RUNNING AN ASSEMBLER - NEED TO SET IT TO RUNNING TO MAKE IT DO THE CORRECT CHECKS.
                        /////////////////////////////////
                        StepDescriptions.SetAssemblersRunningStep(genomeModel);
                        /////////////////////////////////
                        /// END DEBUG
                        /////////////////////////////////

                        // Checks to see if the job is running.
                        if (LinuxCommands.JobRunning(client, Convert.ToInt32(genomeModel.SGEJobId)))
                        {
                            StepDescriptions.SetAssemblersRunningStep(genomeModel);

                            if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                            {
                                #region Check if Masurca is running

                                if (LinuxCommands.DirectoryHasFiles(client, Accessors.GetMasurcaOutputPath(genomeModel.Seed)))
                                {
                                    // Now we need to check if it has completed those assembly jobs.
                                    int masurcaCurrentStep = LinuxCommands.GetCurrentStep(client, Accessors.GetMasurcaOutputPath(genomeModel.Seed), StepDescriptions.GetMasurcaStepList());

                                    // Now we need to set the step/status values for masurca.
                                    if (masurcaCurrentStep != -1)
                                    {
                                        genomeModel.MasurcaCurrentStep = masurcaCurrentStep;
                                        genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), masurcaCurrentStep);

                                        StepDescriptions.NextOverallStep(genomeModel);
                                    }

                                    else
                                        StepDescriptions.NextOverallStep(genomeModel, true);
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
                            if (LinuxCommands.AssemblerSuccess(client, Accessors.GetMasurcaSuccessLogPath(genomeModel.Seed)))
                            {
                                // If masurca isn't finished, we need to check it.
                                if (!genomeModel.MasurcaCurrentStep.Equals(StepDescriptions.GetMasurcaStepList().Count))
                                {
                                    int masurcaCurrentStep = StepDescriptions.GetMasurcaStepList().Last().step;

                                    genomeModel.MasurcaCurrentStep = masurcaCurrentStep;
                                    genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), masurcaCurrentStep);
                                }

                                /////////////////////////////////
                                /// DEBUGGING WITHOUT RUNNING AN ASSEMBLER - NEED TO SIGNIFY THAT WE HAVE COMPLETED THE ASSEMBLER.
                                /////////////////////////////////
                                StepDescriptions.NextOverallStep(genomeModel);
                                /////////////////////////////////
                                /// END DEBUG
                                /////////////////////////////////
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
                                }

                            }

                            /// NOW CHECK SGA.

                            /// NOW CHECK WGS.

                            /////////////////////////////////
                            /// DEBUGGING WITHOUT RUNNING AN ASSEMBLER - NEED TO RESET THE ERROR SO THAT IT DOESN'T ERROR CHECKING FOR DIRECTORY.
                            /////////////////////////////////
                            LinuxErrorHandling.error = "";
                            /////////////////////////////////
                            /// END DEBUG
                            /////////////////////////////////

                            // Need to be careful here. I need to make sure that when there are multiple assemblers that I update them accordingly. NextOverallStep increments the 
                            // number. So I ought to call this upon each assembler SUCCESS or cross-reference it with each assembler's currentStep list.
                            if (!string.IsNullOrEmpty(LinuxErrorHandling.error))
                                StepDescriptions.NextOverallStep(genomeModel, true);
                            else
                                StepDescriptions.NextOverallStep(genomeModel);

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
        protected internal static void UploadData(GenomeModel genomeModel)
        {
            using (var client = new SshClient(CreatePrivateKeyConnectionInfo()))
            {
                try
                {
                    client.Connect();

                    using (GenomeAssemblyDbContext db = new GenomeAssemblyDbContext())
                    {
                        // Grab the unique list of steps for this particular model.
                        //Hashtable overallStepList = StepDescriptions.GenerateOverallStepList(genomeModel.OverallStepSize);

                        #region Compress Data
                        if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                        {
                            LinuxCommands.ZipFiles(client, 9, Accessors.USER_ROOT_JOB_DIRECTORY, Accessors.GetCompressedDataPath(genomeModel.Seed) 
                                , Accessors.GetRelativeJobDirectory(genomeModel.Seed), "yr");

                            if (!string.IsNullOrEmpty(LinuxErrorHandling.error))
                                StepDescriptions.NextOverallStep(genomeModel, true);
                            else
                                StepDescriptions.NextOverallStep(genomeModel);
                        }

                        #endregion

                        #region Connect to SFTP
                        if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                        {
                            LinuxCommands.ConnectSftpToFtp(client, Accessors.FTP_URL, Accessors.PUBLIC_KEY_PATH);

                            if (!string.IsNullOrEmpty(LinuxErrorHandling.error))
                                StepDescriptions.NextOverallStep(genomeModel, true);
                            else
                                StepDescriptions.NextOverallStep(genomeModel);
                        }

                        #endregion

                        #region Upload Data and Set Download Link

                        if (string.IsNullOrEmpty(LinuxErrorHandling.error))
                        {
                            LinuxCommands.SftpUploadFile(client, Accessors.ZIP_STORAGE_PATH, true);

                            if (!string.IsNullOrEmpty(LinuxErrorHandling.error))
                                StepDescriptions.NextOverallStep(genomeModel, true);
                            else
                                StepDescriptions.NextOverallStep(genomeModel);
                        }

                        #endregion
                    }

                }

                // SSH Connection couldn't be established.
                catch (SocketException e)
                {
                    LinuxErrorHandling.error = "The SSH connection couldn't be established. " + e.Message;
                }

                // The SSH connection was dropped.
                catch (SshConnectionException e)
                {
                    LinuxErrorHandling.error = "The connection was terminated unexpectedly. " + e.Message;
                }
            }
            
        }

        #endregion
    }
}