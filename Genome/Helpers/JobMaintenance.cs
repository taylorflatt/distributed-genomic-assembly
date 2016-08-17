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
                        Hashtable overallStepList = StepDescriptions.GenerateOverallStepList(genomeModel.OverallStepSize);
                        HashSet<Assembler> masurcaStepList = StepDescriptions.GetMasurcaStepList();
                        #endregion

                        #region Check if the job is currently running on BigDog
                        // Will return true if the job is still currently IN the scheduler (queued or running).
                        if (LinuxCommands.JobRunning(client, Convert.ToInt32(genomeModel.SGEJobId)))
                        {
                            // Do nothing. It is still queued. Skip everything else.
                            if (LinuxCommands.AssemblerQueued(client, genomeModel.SGEJobId.ToString())) { }

                            else if (LinuxCommands.IsProcessRunning(client, "conversionScript.sh"))
                            {
                                // Set overall step: Data Conversion
                                StepDescriptions.NextOverallStep(genomeModel);
                            }

                            // The Assemblers are running.
                            else
                            {
                                // Set overall step: Running Assemblers
                                StepDescriptions.SetAssemblersRunningStep(genomeModel);

                                if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { UpdateMasurcaStatus(client, genomeModel); }

                                #region Check if SGA is running
                                #endregion

                                #region Check if WGS is running
                                #endregion
                            }
                        }

                        // The job isn't running so it has either completed successfully or run across an error.
                        else
                        {
                            if (string.IsNullOrEmpty(LinuxErrorHandling.error)) { UpdateMasurcaStatus(client, genomeModel); }

                            #region Check if SGA is running
                            #endregion

                            #region Check if WGS is running
                            #endregion

                            // If ALL assemblers finished running:
                            if (genomeModel.MasurcaCurrentStep.Equals(StepDescriptions.GetMasurcaStepList().Last().step))
                            {
                                // Set overall step: Finished Running All Assemblers
                                StepDescriptions.NextOverallStep(genomeModel);
                            }

                            // At least one assembler did not complete successfully.
                            else
                            {
                                // Set overall step: Error step
                                StepDescriptions.NextOverallStep(genomeModel, true);
                            }

                        }
                        #endregion

                        #region Check if data upload is complete

                        // We run this prior to setting it below so it isn't run immediately after we initiate an upload.
                        if (genomeModel.OverallStatus.Equals(StepDescriptions.UPLOAD_DATA_STEP))
                        {
                            if(!LinuxCommands.IsProcessRunning(client, "curl"))
                            {
                                // Set overall step: Completed
                                if (!string.IsNullOrEmpty(LinuxErrorHandling.error))
                                    StepDescriptions.NextOverallStep(genomeModel, true);
                                else
                                    StepDescriptions.NextOverallStep(genomeModel);
                            }
                        }
                        #endregion

                        // Only runs if the current step is the Finished Assemblers step. Otherwise we just ignore it. This should run ONCE.
                        if (string.IsNullOrEmpty(LinuxErrorHandling.error) && StepDescriptions.FinishedAssemblers(genomeModel.OverallCurrentStep, genomeModel.OverallStepSize))
                        {
                            #region Run Data Analysis
                            // Run data analysis logic

                            // Set overall step: Data Analysis
                            if (!string.IsNullOrEmpty(LinuxErrorHandling.error))
                                StepDescriptions.NextOverallStep(genomeModel, true);
                            else
                                StepDescriptions.NextOverallStep(genomeModel);
                            #endregion

                            #region Compress and Upload Data
                            if(string.IsNullOrEmpty(LinuxErrorHandling.error) && !genomeModel.OverallStatus.Equals(StepDescriptions.UPLOAD_DATA_STEP))
                            {
                                LinuxCommands.UploadJob(client, Accessors.USER_ROOT_JOB_DIRECTORY, Accessors.GetCompressedDataPath(genomeModel.Seed)
                                    , Accessors.GetRelativeJobDirectory(genomeModel.Seed), Accessors.GetDownloadLocation(genomeModel.Seed), true, "yr");

                                // Set overall step: Uploading Data to FTP
                                if (!string.IsNullOrEmpty(LinuxErrorHandling.error))
                                    StepDescriptions.NextOverallStep(genomeModel, true);
                                else
                                    StepDescriptions.NextOverallStep(genomeModel);
                            }
                            #endregion
                        }
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

        /// <summary>
        /// Updates the status of the masurca assembler as well as the job.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="genomeModel">The model of the particular job.</param>
        private static void UpdateMasurcaStatus(SshClient client, GenomeModel genomeModel)
        {
            if (string.IsNullOrEmpty(LinuxErrorHandling.error))
            {
                if (LinuxCommands.DirectoryHasFiles(client, Accessors.GetMasurcaOutputPath(genomeModel.Seed)))
                {
                    int currentMasurcaStep = LinuxCommands.GetCurrentStep(client, Accessors.GetMasurcaOutputPath(genomeModel.Seed), StepDescriptions.GetMasurcaStepList());

                    // Provided we didn't encounter an error, set the status of masurca and the job.
                    if (currentMasurcaStep != -1)
                    {
                        genomeModel.MasurcaCurrentStep = currentMasurcaStep;
                        genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), currentMasurcaStep);

                        // Set overall step: Finished Assembler (x of n)
                        StepDescriptions.NextOverallStep(genomeModel);
                    }

                    // We found an error and are aborting.
                    else
                        StepDescriptions.NextOverallStep(genomeModel, true);
                }

                // Either masurca hasn't started or it has but no files have been created yet.
                else
                {
                    genomeModel.MasurcaCurrentStep = 1;
                    genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), 1);
                }
            }
        }
    }
}