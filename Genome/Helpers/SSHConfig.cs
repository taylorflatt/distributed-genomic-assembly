using System;
using System.Collections.Generic;
using Renci.SshNet;
using System.Threading;

namespace Genome.Helpers
{
    public class SSHConfig
    {
        private string configUrl = "";
        private string schedulerUrl = "";
        private string dataUrl = "";
        private static string COMPUTENODE1 = "compute-0-24";
        private static string COMPUTENODE2 = "compute-0-25";

        public SSHConfig(string configUrl, string schedulerUrl, string dataUrl)
        {
            this.configUrl = configUrl;
            this.schedulerUrl = schedulerUrl;
            this.dataUrl = dataUrl;
        }

        public void CreateConnectionThread()
        {
            //ThreadStart job = new ThreadStart();
            Thread thread = new Thread(() => CreateConnection(configUrl, schedulerUrl, dataUrl));
            //thread.IsBackground = true;
            thread.Start();
        }

        private void CreateConnection(List<string> configUrl, string schedulerUrl, List<string> dataUrl)
        {
            using (var sshClient = new SshClient("ip", "username", "password"))
            {
                sshClient.Connect();
                using (var cmd = sshClient.CreateCommand("mkdir -p /tmp/uploadtest && chmod +rw /tmp/uploadtest"))
                {
                    cmd.Execute();
                    Console.WriteLine("Command>" + cmd.CommandText);
                    Console.WriteLine("Return Value = {0}", cmd.ExitStatus);
                }

                // May not be needed once scheduler is running
                sshClient.CreateCommand("ssh " + COMPUTENODE1);

                foreach (string config in configUrl)
                {
                    sshClient.CreateCommand("wget \"" + config + "\"").Execute();
                }

                //Create working directory
                //Need to make a directory: mkdir //share/GenomeAssembly/{username}/{job#}
                sshClient.CreateCommand();

                for (int i = 0; i < dataUrl.Count; i++)
                {
                    // Last command sent starts the assembler specifc commands
                    if (i == dataUrl.Count - 1)
                        sshClient.CreateCommand("wget \"" + dataUrl[i] + "\" && ").Execute();

                    else
                        sshClient.CreateCommand("wget \"" + dataUrl[i] + "\"").Execute();
                }

                sshClient.CreateCommand("wget " + schedulerUrl).Execute();

                sshClient.Disconnect();
            }
        }
}

    Config File
    Data
    Scheduler config
    Execute assembler specific commands