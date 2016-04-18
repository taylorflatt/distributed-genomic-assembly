using Renci.SshNet;
using System;

namespace Genome.Helpers
{
    public class LinuxCommands
    {
        // Returns the node load or -1 if the command was unsuccessful.
        protected internal static int GetNodeLoad(SshClient client, string node, out string error)
        {
            // We only want to get the load average of the specific node. 
            using (var cmd = client.CreateCommand("qstat -f | grep " + node + " | awk '{print $4;}'"))
            {
                cmd.Execute();

                if(LinuxErrorHandling.CommandError(cmd, out error) == false)
                    return Convert.ToInt32(cmd.Result);

                else
                    return -1;
            }
        }

        protected internal static void ChangeDirectory(SshClient client, string newDirectory, out string error, string parameters = "")
        {
            // USAGE: cd /opt/testDirectory
            using (var cmd = client.CreateCommand("cd " + newDirectory + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static void ChangeNode(SshClient client, string node, out string error)
        {
            // USAGE: ssh compute-0-24/25
            // Changes to the specific node that we are using.
            using (var cmd = client.CreateCommand("ssh " + node))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static void CreateDirectory(SshClient client, string directoryPath, out string error, string directoryParameters = "")
        {
            // USAGE: mkdir /tmp/Genome/tflatt1029/Logs -p
            using (var cmd = client.CreateCommand("mkdir " + directoryPath + " " + directoryParameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static void DownloadFile(SshClient client, string URL, out string error, string parameters = "")
        {
            // USAGE: wget www.google.com/file1 -o logoutput.error
            using (var cmd = client.CreateCommand("wget " + URL + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        protected internal static void ChangePermissions(SshClient client, string absolutePath, string newPermissions, out string error, string parameters = "")
        {
            // USAGE: chmod 777 /tmp/test -R
            using (var cmd = client.CreateCommand("chmod " + " " + newPermissions + " " + absolutePath + " " + parameters))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }

        // Add the job to the scheduler.
        protected internal static void AddJobToScheduler(SshClient client, string logPath, string node, string jobName, out string error)
        {
            // USAGE: qsub - pe make 20 - V - e / tmp / Genome / -o / tmp / Genome / -b y - l hostname = compute - 0 - 24 - N taylor1./ HelloWorld
            using (var cmd = client.CreateCommand("qsub -pe make 20 -V -e " + logPath + " -o " + logPath + " -b y -l hostname=" + node + "-N " + jobName + "./assemble.sh"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }
    }
}