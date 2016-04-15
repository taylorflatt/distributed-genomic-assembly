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

        private string ip;
        private GenomeModel genomeModel;
        private string path;
        private string error;

        public SSHConfig(string ip, GenomeModel genomeModel, string path, out string error)
        {
            error = "";

            this.ip = ip;
            this.genomeModel = genomeModel;
            this.path = path;
            this.error = error;
        }

        // Here we will use our account (or if we have to...the user's or we can curl) to grab the status of the job.
        public bool UpdateJobStatus()
        {
            // We want to cat <job_name>.o<job_number> | grep "Status" and see where we are.
            using (var sshClient = new SshClient(ip, genomeModel.SSHUser, genomeModel.SSHPass))
            {

            }
        }

        // Will return TRUE if successful connection and commands all run or FALSE if ANY error is encountered.
        public bool CreateConnection()
        {
            // Need to create a directory here that is unique to the user if it doesn't already exist. 
            // For instance: WORKINGDIRECTORY/Taylor/Job1 and /WORKINGDIRECTORY/Taylor/Job2 and so on.

            // Then we wget all the scripts and store them in the WORKINGDIRECTORY/Taylor/Job1/Scripts.

            // Then we call the WORKINGDIRECTORY/Taylor/Job1/Scripts/Scheduler.sh which will launch the init.sh script.

            // The init.sh script will contain all the basic logic to download the data and initiate the job on the assembler(s).
            using (var sshClient = new SshClient(ip, genomeModel.SSHUser, genomeModel.SSHPass))
            {
                string localPath = "Job" + genomeModel.uuid;
                string dataPath = localPath + "/Data"; // Added dataPath variable - MG
                string configPath = localPath + "/Config"; // Added configPath variable - MG
                string outputPath = localPath + "/Output"; // Added outputPath variable - MG
                string initName = "init" + genomeModel.uuid;
                string masurcaName = "masurca" + genomeModel.uuid;
                string schedulerName = "scheduler" + genomeModel.uuid;

                sshClient.Connect();

                if (errorCount == 0) { CreateDirectories(sshClient, localPath, dataPath, configPath, outputPath); }

                if (errorCount == 0) { UploadInitScript(sshClient, path, initName, configPath); }
                if (errorCount == 0) { UploadMasurcaScript(sshClient, path, masurcaName, configPath); }
                if (errorCount == 0) { UploadSchedulerScript(sshClient, path, schedulerName, configPath); }

                if (errorCount == 0) { ChangePermissions(sshClient, localPath); }

                if (errorCount == 0) { AddJobToScheduler(sshClient); }

                if (errorCount == 0)
                {
                    //RunDemo(sshClient);
                }
                //sshClient.Disconnect();
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
        // Added 3 new parameters -MG
        private void CreateDirectories(SshClient client, string localPath, string dataPath, string configPath, string outputPath)
        {
            using (var cmd = client.CreateCommand("mkdir -p " + localPath))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }

            // Added following commands -MG
            using (var cmd = client.CreateCommand("mkdir -p " + dataPath))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }

            using (var cmd = client.CreateCommand("mkdir -p " + configPath))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }

            using (var cmd = client.CreateCommand("mkdir -p " + outputPath))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
        }

        // Upload the init script (initializes all other assembler scripts and grabs data at runtime).
        private void UploadInitScript(SshClient client, string path, string initName, string configPath)
        {
            using (var cmd = client.CreateCommand("wget -O " + configPath + "/" + initName + ".sh " + path))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
        }

        // Upload the Masurca assembler script.
        private void UploadMasurcaScript(SshClient client, string path, string masurcaName, string configPath)
        {
            using (var cmd = client.CreateCommand("wget -O " + configPath + "/" + masurcaName + ".txt " + path))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
        }

        // Upload the scheduler script.
        private void UploadSchedulerScript(SshClient client, string path, string schedulerName, string configPath)
        {
            using (var cmd = client.CreateCommand("wget -0 " + configPath + "/" + schedulerName + ".sh " + path))
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

        // Run init script, since scheduler is not implemented currently work in progress
        private void RunInitScript(SshClient client, string initName, string configPath)
        {
            using (var cmd = client.CreateCommand("./scheduler.sh"))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
        }

        // Cooked method for demo
        private void RunDemo(SshClient client)
        {
            using (var cmd = client.CreateCommand("cd ~/Demo/Output && /share/bio/masurca/bin/masurca ~/Demo/Config/config.txt"))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
            using (var cmd = client.CreateCommand("cd ~/Demo/Output && ./test.sh"))
            {
                cmd.Execute();
                CatchError(cmd, out errorOutput);
            }
            client.Disconnect();
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