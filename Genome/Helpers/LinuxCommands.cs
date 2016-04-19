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

        // Returns true if the job is currently running or false if the job isn't running/if an error exists.
        protected internal static bool JobRunning(SshClient client, int jobId, out string error)
        {
            // USAGE: qstat -j 5120
            using (var cmd = client.CreateCommand("qstat -j " + jobId))
            {
                cmd.Execute();

                // So long as there isn't an error...
                if(LinuxErrorHandling.CommandError(cmd, out error) == false)
                {
                    if (cmd.Result.Contains("Following jobs do not exist:"))
                        return false;

                    else
                        return true;
                }

                // Need to come back to this to address better exception handling. This should be fine in the meantime.
                else
                {
                    throw new Exception();
                }
            }
        }

        protected internal static void CheckJobError(SshClient client, int jobId, string workingDirectory, out string error)
        {
            ChangeDirectory(client, workingDirectory + "/Logs", out error);

            // USAGE: ls -l | grep e2014 | cat `awk '{print $9}'`
            // This effectively finds the error log in the cwd then prints the word count to stdout.
            using (var cmd = client.CreateCommand("ls -l | grep e" + jobId + " " + "| cat `awk '{print $9}'` | wc -w")) 
            {
                cmd.Execute();

                // Provided there was no error, we now need to see if if there are any characters in that file. If ANY, we have a problem.
                if (LinuxErrorHandling.CommandError(cmd, out error) == false)
                {
                    // The error log is empty and so we had a successful run.
                    if (Convert.ToInt32(cmd.Result) > 0)
                    {
                        // We need to download the data from bigdog back to the webserver under the public FTP.
                        // We need to make the download link accessible (or at least known) to the user.
                        // We need to now notify the user that their download is complete.
                        // We need to mark the job as completed (job status = completed/finished).
                    }

                    // There is something in the error log, we had an unsuccessful run.
                    else
                    {
                        // We need to donwload the data from bigdog back to the webserver under the public FTP.
                        // We need to add a README file or at least specify that the data located therein is not complete.
                        // We need to make the download link accessible (or at least known) to the user.
                        // We need to now notify the user that their download is complete and that an error occurred.
                        // We need to mark the job as error (job status = error).
                    }
                }
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
            // USAGE: qsub -pe make 20 -V -e /tmp/Genome/ -o/tmp/Genome/ -b y -l hostname=compute-0-24 -N taylor1 ./HelloWorld
            // qsub -pe make 20 -V  -b y -l hostname=compute-0-24 -cwd -N taylor1 ./HelloWorld
            using (var cmd = client.CreateCommand("qsub -pe make 20 -V -e " + logPath + " -o " + logPath + " -b y -l hostname=" + node + "-N " + jobName + "./assemble.sh"))
            {
                cmd.Execute();

                LinuxErrorHandling.CommandError(cmd, out error);
            }
        }
    }
}