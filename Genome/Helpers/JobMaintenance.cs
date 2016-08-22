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

            var methods = new List<AuthenticationMethod>();
            methods.Add(new PrivateKeyAuthenticationMethod(Accessors.UPDATE_ACCT, keyFiles));

            return new ConnectionInfo(Accessors.BD_IP, Accessors.BD_PORT, Accessors.UPDATE_ACCT, methods.ToArray());
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
                        bool continueUpdate = true;     // Determines whether we will continue checking the job status.
                        bool DEBUG_MODE = true;         // Debug mode to skip some assembler steps.
                        bool outOfRange = false;        // If the overall step is out of bounds, then we set this to true to attempt a correction.

                        while (continueUpdate && ErrorHandling.NoError())
                        {
                            // Depending on the current step, this switch will determine if the state of the job needs to change.
                            switch (genomeModel.OverallCurrentStep)
                            {
                                // Queued step
                                case 1:
                                {
                                    if (DEBUG_MODE)
                                    {
                                        genomeModel.NextStep();
                                        break;
                                    }

                                    if (LinuxCommands.IsProcessRunning(client, "conversionScript.sh"))
                                    genomeModel.NextStep();

                                    else
                                        continueUpdate = false;

                                    break;
                                }

                                // Data conversion step
                                case 2:
                                {
                                    if (DEBUG_MODE)
                                    {
                                        genomeModel.NextStep();
                                        break;
                                    }

                                    if (LinuxCommands.DirectoryHasFiles(client, Accessors.GetMasurcaOutputPath(genomeModel.Seed)))
                                        genomeModel.NextStep();

                                    else
                                        continueUpdate = false;

                                    break;
                                }

                                // Running assemblers step
                                case 3:
                                {
                                    if (DEBUG_MODE)
                                    {
                                        genomeModel.NextStep();
                                        break;
                                    }

                                    if (genomeModel.UseMasurca)
                                        CheckMasurcaStep(client, genomeModel);

                                    if (genomeModel.UseSGA) { }

                                    if (genomeModel.UseWGS) { }

                                    if (genomeModel.IsAssemblyFinished())
                                        genomeModel.NextStep();

                                    else
                                        continueUpdate = false;

                                    break;
                                }

                                // Data analysis step
                                case 4:
                                {
                                    // Until data analysis is implemented, we skip the step.
                                    genomeModel.NextStep();
                                    break;

                                    //if (LinuxCommands.IsProcessRunning(client, "dataAnalysis.sh"))
                                    //    continueUpdate = false;

                                    //else
                                    //{
                                    //    // Has it finished?
                                    //    if (LinuxCommands.FileExists(client, Accessors.GetJobOutputPath(genomeModel.Seed) + "dataAnalysisResult"))
                                    //        genomeModel.NextStep();

                                    //    else
                                    //        LinuxCommands.RunDataAnalysis(client);
                                    //}

                                    //break;
                                }

                                // TODO: Create a more robust method in checking for a completed upload. Maybe connect to the FTP and compare file sizes and see if they are close.
                                // Uploading Data step
                                case 5:
                                {
                                    //if (LinuxCommands.IsJobUploading(client, Accessors.USER_ROOT_JOB_DIRECTORY, Accessors.GetCompressedDataPath(genomeModel.Seed)))
                                    //    continueUpdate = false;

                                    //else if (LinuxCommands.FileExists(client, Accessors.GetCompressedDataPath(genomeModel.Seed)))
                                    //    genomeModel.NextStep();

                                    //else
                                    //{
                                        LinuxCommands.UploadJobData(client, Accessors.USER_ROOT_JOB_DIRECTORY, Accessors.GetCompressedDataPath(genomeModel.Seed)
                                            , Accessors.GetRelativeJobDirectory(genomeModel.Seed), Accessors.GetRemoteDownloadLocation(genomeModel.Seed), true, "yr");

                                        continueUpdate = false;
                                    //}


                                    break;
                                }

                                // Completed step
                                case 6:
                                {
                                    continueUpdate = false;

                                    break;
                                }

                                default:
                                {
                                    // If we have attempted a correction and failed, throw in the towel.
                                    if (outOfRange)
                                        throw new IndexOutOfRangeException("The current step of the program is out of bounds after an attempted correction. The current step: " 
                                            + genomeModel.OverallCurrentStep);

                                    else
                                    {
                                        outOfRange = true;
                                        
                                        // Reset the state to default and have it run through the update method again.
                                        genomeModel.OverallCurrentStep = 1;
                                        genomeModel.OverallStatus = StepDescriptions.GetOverallStepList()[genomeModel.OverallCurrentStep].ToString();
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }

                // SSH Connection couldn't be established.
                catch (SocketException e)
                {
                    ErrorHandling.error = "The SSH connection couldn't be established. " + e.Message;
                }

                // The SSH connection was dropped.
                catch (SshConnectionException e)
                {
                    ErrorHandling.error = "The connection was terminated unexpectedly. " + e.Message;
                }
            }
        }

        /// <summary>
        /// Updates the status of the masurca assembler. If it has either errored out (-1) or is completed (last step), it will not change the status.
        /// </summary>
        /// <param name="client">The current SSH client session.</param>
        /// <param name="genomeModel">The model of the particular job.</param>
        private static void CheckMasurcaStep(SshClient client, GenomeModel genomeModel)
        {
            if (string.IsNullOrEmpty(ErrorHandling.error) 
                    && !genomeModel.MasurcaCurrentStep.Equals(-1) 
                    && !genomeModel.MasurcaCurrentStep.Equals(StepDescriptions.GetMasurcaStepList().Last().step))
            {
                if (LinuxCommands.DirectoryHasFiles(client, Accessors.GetMasurcaOutputPath(genomeModel.Seed)))
                {
                    int currentMasurcaStep = LinuxCommands.GetCurrentStep(client, Accessors.GetMasurcaOutputPath(genomeModel.Seed), StepDescriptions.GetMasurcaStepList());

                    // Provided we didn't encounter an error, set the status of masurca and the job.
                    if (currentMasurcaStep != -1)
                    {
                        genomeModel.MasurcaCurrentStep = currentMasurcaStep;
                        genomeModel.MasurcaStatus = StepDescriptions.GetCurrentStepDescription(StepDescriptions.GetMasurcaStepList(), currentMasurcaStep);
                    }

                    else
                        StepDescriptions.SetMasurcaError(client, genomeModel);
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