using Renci.SshNet;
using Genome.Models;

/// <summary>
/// TODO: May need to change the CWD to a default folder and run the scripts from a special folder rather than the user's folder.
/// TODO: Output ANY error information to a log file with the user's details and session data. Should probably be done in the controller.
/// </summary>
namespace Genome.Helpers
{
    public class SSHConfig
    {
        private int errorCount = 0;
        private string errorOutput = "";

        private static string COMPUTENODE1 = "compute-0-24";
        private static string COMPUTENODE2 = "compute-0-25";

        public SSHConfig() { }

        // Will return TRUE if successful connection and commands all run or FALSE if ANY error is encountered.
        public bool CreateConnection(string ip, GenomeModel genomeModel, string path, out string error)
        {
            // Need to create a directory here that is unique to the user if it doesn't already exist. 
            // For instance: WORKINGDIRECTORY/Taylor/Job1 and /WORKINGDIRECTORY/Taylor/Job2 and so on.

            // Then we wget all the scripts and store them in the WORKINGDIRECTORY/Taylor/Job1/Scripts.

            // Then we call the WORKINGDIRECTORY/Taylor/Job1/Scripts/Scheduler.sh which will launch the init.sh script.

            // The init.sh script will contain all the basic logic to download the data and initiate the job on the assembler(s).
            using (var sshClient = new SshClient(ip, genomeModel.SSHUser, genomeModel.SSHPass))
            {
                string localPath = "Job" + genomeModel.uuid;
                string initName = "init" + genomeModel.uuid;
                string masurcaName = "masurca" + genomeModel.uuid;
                string schedulerName = "scheduler" + genomeModel.uuid;

                sshClient.Connect();

                if (errorCount == 0) { CreateDirectories(sshClient, localPath); }

                if (errorCount == 0) { UploadInitScript(sshClient, path, initName); }
                if (errorCount == 0) { UploadMasurcaScript(sshClient, path, masurcaName); }
                if (errorCount == 0) { UploadSchedulerScript(sshClient, path, schedulerName); }

                if (errorCount == 0) { ChangePermissions(sshClient, localPath); }

                if (errorCount == 0) { AddJobToScheduler(sshClient); }

                sshClient.Disconnect();
            }

            // There were no errors.
            if (errorCount == 0)
            {
                error = errorOutput;
                return true;
            }

            // There was at least one error, return false as well as the error itself.
            else
            {
                error = errorOutput;
                return false;
            }
        }

        // Create the job directory.
        private void CreateDirectories(SshClient client, string localPath)
        {
            using (var cmd = client.CreateCommand("mkdir -p" + localPath))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
        }

        // Upload the init script (initializes all other assembler scripts and grabs data at runtime).
        private void UploadInitScript(SshClient client, string path, string initName)
        {
            using (var cmd = client.CreateCommand("wget " + path + initName))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
        }

        // Upload the Masurca assembler script.
        private void UploadMasurcaScript(SshClient client, string path, string masurcaName)
        {
            using (var cmd = client.CreateCommand("wget " + path + masurcaName))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
        }

        // Upload the scheduler script.
        private void UploadSchedulerScript(SshClient client, string path, string schedulerName)
        {
            using (var cmd = client.CreateCommand("wget " + path + schedulerName))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
        }

        // Change the entire directory's contents to R/W/X.
        private void ChangePermissions(SshClient client, string localPath)
        {
            using (var cmd = client.CreateCommand("chmod -R +rwx " + localPath))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
        }

        // Add the job to the scheduler.
        private void AddJobToScheduler(SshClient client)
        {
            using (var cmd = client.CreateCommand("./scheduler.sh"))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
        }

        private void CatchError(SshCommand cmd, out string error)
        {
            error = "";

            // There is an error...return the error so we can display it to the user.
            if (!string.IsNullOrEmpty(cmd.Error))
            {
                errorCount++;
                error = cmd.Error;
            }
        }
    }
}