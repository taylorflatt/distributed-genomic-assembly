﻿using System;
using System.Collections.Generic;
using Renci.SshNet;
using System.Threading;

namespace Genome.Helpers
{
    public class SSHConfig
    {
        private string userName = "";
        private string password = "";
        private string ip = "";

        private string configUrl = "";
        private string schedulerUrl = "";
        private string dataUrl = "";
        private static string COMPUTENODE1 = "compute-0-24";
        private static string COMPUTENODE2 = "compute-0-25";

        public SSHConfig(string ip, string userName, string password)
        {
            this.userName = userName;
            this.password = password;
            this.ip = ip;

            CreateConnection();
        }

        private void CreateConnection()
        {
            // Need to create a directory here that is unique to the user if it doesn't already exist. 
            // For instance: WORKINGDIRECTORY/Taylor/Job1 and /WORKINGDIRECTORY/Taylor/Job2 and so on.

            // Then we wget all the scripts and store them in the WORKINGDIRECTORY/Taylor/Job1/Scripts.

            // Then we call the WORKINGDIRECTORY/Taylor/Job1/Scripts/Scheduler.sh which will launch the init.sh script.

            // The init.sh script will contain all the basic logic to download the data and initiate the job on the assembler(s).
            using (var sshClient = new SshClient(ip, userName, password))
            {
                sshClient.Connect();
                using (var cmd = sshClient.CreateCommand("wget http://releases.ubuntu.com/14.04.4/ubuntu-14.04.4-desktop-amd64.iso?_ga=1.218458457.1946723420.1457237672"))
                {
                    cmd.Execute();
                    Console.WriteLine("Command>" + cmd.CommandText);
                    Console.WriteLine("Return Value = {0}", cmd.ExitStatus);
                }
                sshClient.Disconnect();
            }
        }

        //public SSHConfig(string configUrl, string schedulerUrl, string dataUrl)
        //{
        //    this.configUrl = configUrl;
        //    this.schedulerUrl = schedulerUrl;
        //    this.dataUrl = dataUrl;
        //}

        //public void CreateConnectionThread()
        //{
        //    //ThreadStart job = new ThreadStart();
        //    Thread thread = new Thread(() => CreateConnection(configUrl, schedulerUrl, dataUrl));
        //    //thread.IsBackground = true;
        //    thread.Start();
        //}

        //private void CreateConnection(List<string> configUrl, string schedulerUrl, List<string> dataUrl)
        //{
        //    using (var sshClient = new SshClient("ip", "username", "password"))
        //    {
        //        sshClient.Connect();
        //        using (var cmd = sshClient.CreateCommand("mkdir -p /tmp/uploadtest && chmod +rw /tmp/uploadtest"))
        //        {
        //            cmd.Execute();
        //            Console.WriteLine("Command>" + cmd.CommandText);
        //            Console.WriteLine("Return Value = {0}", cmd.ExitStatus);
        //        }

        //        // May not be needed once scheduler is running
        //        sshClient.CreateCommand("ssh " + COMPUTENODE1);

        //        foreach (string config in configUrl)
        //        {
        //            sshClient.CreateCommand("wget \"" + config + "\"").Execute();
        //        }

        //        //Create working directory
        //        //Need to make a directory: mkdir //share/GenomeAssembly/{username}/{job#}
        //        sshClient.CreateCommand();


        //  The following will be changed with an init script on big dog itself 
        //        for (int i = 0; i < dataUrl.Count; i++)
        //        {
        //            // Last command sent starts the assembler specifc commands
        //            if (i == dataUrl.Count - 1)
        //                sshClient.CreateCommand("wget \"" + dataUrl[i] + "\" && ").Execute();

        //            else
        //                sshClient.CreateCommand("wget \"" + dataUrl[i] + "\"").Execute();
        //        }

        //        sshClient.CreateCommand("wget " + schedulerUrl).Execute();

        //        sshClient.Disconnect();
        //    }
    }
}

    //Config File
    //Data
    //Scheduler config
    //Execute assembler specific commands