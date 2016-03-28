using Renci.SshNet;
using Genome.Models;

/// <summary>
/// TODO: May need to change the CWD to a default folder and run the scripts from a special folder rather than the user's folder.
/// </summary>
namespace Genome.Helpers
{
    public class SSHConfig
    {
        private GenomeModel genomeModel;

        private static string COMPUTENODE1 = "compute-0-24";
        private static string COMPUTENODE2 = "compute-0-25";

        public SSHConfig(string ip, GenomeModel genomeModel)
        {
            string username = genomeModel.SSHUser;
            string password = genomeModel.SSHPass;

            this.genomeModel = genomeModel;

            CreateConnection(ip, username, password);
        }

        private void CreateConnection(string ip, string username, string password)
        {
            // Need to create a directory here that is unique to the user if it doesn't already exist. 
            // For instance: WORKINGDIRECTORY/Taylor/Job1 and /WORKINGDIRECTORY/Taylor/Job2 and so on.

            // Then we wget all the scripts and store them in the WORKINGDIRECTORY/Taylor/Job1/Scripts.

            // Then we call the WORKINGDIRECTORY/Taylor/Job1/Scripts/Scheduler.sh which will launch the init.sh script.

            // The init.sh script will contain all the basic logic to download the data and initiate the job on the assembler(s).
            using (var sshClient = new SshClient(ip, username, password))
            {
                string jobID = genomeModel.uuid.ToString();
                string jobDirectory = "Job/ " + jobID;

                sshClient.Connect();

                CreateDirectories(sshClient, jobDirectory);

                UploadInitScript(sshClient, jobID);
                UploadMasurcaScript(sshClient, jobID);
                UploadSchedulerScript(sshClient, jobID);

                ChangePermissions(sshClient, jobDirectory);

                AddJobToScheduler(sshClient);

                sshClient.Disconnect();
            }
        }

        // Create the job directory.
        private void CreateDirectories(SshClient client, string jobDirectory)
        {
            using (var cmd = client.CreateCommand("mkdir -p" + jobDirectory))
            {
                cmd.Execute();
            }
        }

        // Upload the init script (initializes all other assembler scripts and grabs data at runtime).
        private void UploadInitScript(SshClient client, string jobID)
        {
            using (var cmd = client.CreateCommand("wget [DOMAIN/IP]/AssemblerConfigs/Job" + jobID + "/init" + jobID + ".sh"))
            {
                cmd.Execute();
            }
        }

        // Upload the Masurca assembler script.
        private void UploadMasurcaScript(SshClient client, string jobID)
        {
            using (var cmd = client.CreateCommand("wget [DOMAIN/IP]/AssemblerConfigs/Job" + jobID + "/masurca" + jobID + ".sh"))
            {
                cmd.Execute();
            }
        }

        // Upload the scheduler script.
        private void UploadSchedulerScript(SshClient client, string jobID)
        {
            using (var cmd = client.CreateCommand("wget [DOMAIN/IP]/AssemblerConfigs/Job" + jobID + "/scheduler" + jobID + ".sh"))
            {
                cmd.Execute();
            }
        }

        // Change the entire directory's contents to R/W/X.
        private void ChangePermissions(SshClient client, string jobDirectory)
        {
            using (var cmd = client.CreateCommand("chmod -R +rwx " + jobDirectory))
            {
                cmd.Execute();
            }
        }

        // Add the job to the scheduler.
        private void AddJobToScheduler(SshClient client)
        {
            using (var cmd = client.CreateCommand("./scheduler.sh"))
            {
                cmd.Execute();
            }
        }
    }
}