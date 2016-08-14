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

        /// <summary>
        /// Constructor for the SSHConfig class without model data.
        /// </summary>
        /// <param name="ip">Sets the ip for connecting to an SSH session.</param>
        /// <param name="error">Any error encountered by the command.</param>
        public SSHConfig(string ip, out string error)
        {
            error = "";
            this.ip = ip;
        }

        /// <summary>
        /// Constructor for the SSHConfig class with model data.
        /// </summary>
        /// <param name="ip">Sets the ip for connecting to an SSH session.</param>
        /// <param name="genomeModel">The model data for a particular job.</param>
        /// <param name="error">Any error encountered by the command.</param>
        public SSHConfig(string ip, GenomeModel genomeModel, out string error)
        {
            error = "";
            this.ip = ip;
            this.genomeModel = genomeModel;
        }

        /// <summary>
        /// Verifies that a particular user has sufficient quota in order to run the assemblers.
        /// </summary>
        /// <param name="SSHUser">The SSH username of the user.</param>
        /// <param name="SSHPass">The SSH password of the user.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <param name="quotaAmount">The amount of quota in GB that the user has. This is sent out of the method.</param>
        /// <returns>Returns a boolean value representing whether or not they have enough space.</returns>
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

        /// <summary>
        /// We need to verify that a user has sufficient permissions in order to run the assemblers. 
        /// </summary>
        /// <param name="SSHUser">The SSH username of the user.</param>
        /// <param name="SSHPass">The SSH password of the user.</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>Returns a boolean value representing whether or not they have sufficient permissions.</returns>
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

        // IMPORTANT!!!! UNTESTED METHOD!!!!
        /// <summary>
        /// Tests whether the URLs entered by the user in the wizard are connectable. A download is attempted and if connectable, stopped and removed. (UNTESTED)
        /// </summary>
        /// <param name="error">Any error encountered by the command.</param>
        /// <param name="badUrl">A string sent out of the method representing a URL which does not work.</param>
        /// <returns>Returns false if at least one url false. Returns true only if all are downloadable.</returns>
        public bool TestJobUrls(out string error, out string badUrl)
        {
            using (var client = new SshClient(Locations.BD_IP, genomeModel.SSHUser, genomeModel.SSHPass))
            {
                badUrl = "";
                error = "";

                // Create a seed value to reduce the risk of them having already created the folder.
                Random rand = new Random();
                int seed = rand.Next(1, 50102);

                List<string> urlList = HelperMethods.ParseUrlString(genomeModel.DataSource);

                client.Connect();

                // Create temp directory for storing wget download log data.
                LinuxCommands.CreateDirectory(client, Locations.GetUrlTestDirectory(seed), out error);

                foreach (var url in urlList)
                {
                    // If there is an error downloading the file, then we break the loop.
                    if (!LinuxCommands.CheckDataAvailability(client, url, seed, out error))
                    {
                        badUrl = url;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(error)) { LinuxCommands.RemoveFile(client, Locations.GetUrlTestDirectory(seed), out error, "-rf"); }

                if (string.IsNullOrEmpty(badUrl))
                    return true;

                else
                    return false;
            }
        }

        // Will return TRUE if successful connection and commands all run or FALSE if ANY error is encountered.
        /// <summary>
        /// Creates a job by adding it to SGE (scheduler) on BigDog.
        /// </summary>
        /// <param name="initUrl">The path to the init script (URL).</param>
        /// <param name="masurcaUrl">The path to the masurca script (URL).</param>
        /// <param name="error">Any error encountered by the command.</param>
        /// <returns>Returns true only if a job gets successfully added to SGE.</returns>
        public bool CreateJob(string initUrl, string masurcaUrl, out string error)
        {
            error = "";
            // IMPORTANT!!!!! UUID is not being assigned before it hits this method so we have a problem. We need to save it to the DB prior to hitting this method but that causes other problems.......need to look into this. We can probably find a work around by checking the db and seeing the previous uuid and just incrementing the previous uuid.

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

                    // This command has NOT been tested. We may need an absolute path rather than the relative one that we reference in this method
                    //since we switch directories to the output path directory. !!!!!!COMMENTING OUT FOR DEBUG PURPOSES!!!!!!
                    //if (string.IsNullOrEmpty(error)) { LinuxCommands.AddJobToScheduler(client, Locations.GetJobLogPath(id), node, jobName, out error); }

                    //if (string.IsNullOrEmpty(error)) { genomeModel.SGEJobId = LinuxCommands.SetJobNumber(client, genomeModel.SSHUser, jobName, out error); }

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
    }
}