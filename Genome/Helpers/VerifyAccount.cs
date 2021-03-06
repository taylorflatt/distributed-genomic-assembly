﻿using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Net.Sockets;

namespace Genome.Helpers
{
    public class VerifyAccount
    {
        /// <summary>
        /// Checks whether a user has the necessary permissions and quota to use the Assemblers.
        /// </summary>
        /// <param name="sshUser">The SSH username of the user.</param>
        /// <param name="sshPass">The SSH password of the user.</param>
        /// <returns>Returns true only if a user has sufficient permissions and quota.</returns>
        public static bool VerifyClusterAccount(string sshUser, string sshPass)
        {
            if (VerifyQuota(sshUser, sshPass) && VerifyPermissions(sshUser, sshPass))
                return true;

            else
                return false;
        }

        /// <summary>
        /// We need to verify that a user has sufficient permissions in order to run the assemblers. 
        /// </summary>
        /// <param name="SSHUser">The SSH username of the user.</param>
        /// <param name="SSHPass">The SSH password of the user.</param>
        /// <returns>Returns a boolean value representing whether or not they have sufficient permissions.</returns>
        public static bool VerifyPermissions(string SSHUser, string SSHPass)
        {
            using (var client = new SshClient(Accessors.BD_IP, SSHUser, SSHPass))
            {
                try
                {
                    client.Connect();

                    LinuxCommands.CreateDirectory(client, Accessors.VERIFY_PERMISSIONS_TEST_DIR);

                    if (!string.IsNullOrEmpty(ErrorHandling.error))
                    {
                        ErrorHandling.error = "You do not have sufficient permissions to write to the proper directories. Please contact the BigDog Linux team about addressing this problem.";

                        return false;
                    }

                    else
                    {
                        // We want to remove the directory we just created as a test. We recursively force the deletion.
                        LinuxCommands.RemoveFile(client, Accessors.VERIFY_PERMISSIONS_TEST_DIR, "-rf");

                        return true;
                    }
                }

                // SSH Connection couldn't be established.
                catch (SocketException e)
                {
                    ErrorHandling.error = "The SSH connection couldn't be established. " + e.Message;

                    return false;
                }

                // Authentication failure.
                catch (SshAuthenticationException e)
                {
                    ErrorHandling.error = "The credentials were entered incorrectly. " + e.Message;

                    return false;
                }

                // The SSH connection was dropped.
                catch (SshConnectionException e)
                {
                    ErrorHandling.error = "The connection was terminated unexpectedly. " + e.Message;

                    return false;
                }

                catch (Exception e)
                {
                    ErrorHandling.error = "There was an uncaught exception. " + e.Message;

                    return false;
                }
            }
        }

        /// <summary>
        /// Verifies that a particular user has sufficient quota in order to run the assemblers.
        /// </summary>
        /// <param name="SSHUser">The SSH username of the user.</param>
        /// <param name="SSHPass">The SSH password of the user.</param>
        /// <param name="quotaAmount">The amount of quota in GB that the user has. This is sent out of the method.</param>
        /// <returns>Returns a boolean value representing whether or not they have enough space.</returns>
        public static bool VerifyQuota(string SSHUser, string SSHPass)
        {
            using (var client = new SshClient(Accessors.BD_IP, SSHUser, SSHPass))
            {
                try
                {
                    client.Connect();

                    int userQuota = LinuxCommands.GetQuota(client, Accessors.MINIMUM_QUOTA);

                    // If they have less than 'minQuota' then we return an error telling them the problem and how to rectify it.
                    if (userQuota < Accessors.MINIMUM_QUOTA)
                    {
                        ErrorHandling.error = "You do not have the requisite amount of disk space (" + Accessors.MINIMUM_QUOTA + "Gb) for us to safely run a general assembly "
                            + "job. Please contact the BigDog admin team to increase your quota. You currently have " + userQuota + "Gb space to use.";

                        return false;
                    }

                    // They have at least the minimum quota.
                    else
                        return true;
                }

                // SSH Connection couldn't be established.
                catch (SocketException e)
                {
                    ErrorHandling.error = "The SSH connection couldn't be established. " + e.Message;

                    return false;
                }

                // Authentication failure.
                catch (SshAuthenticationException e)
                {
                    ErrorHandling.error = "The credentials were entered incorrectly. " + e.Message;

                    return false;
                }

                // The SSH connection was dropped.
                catch (SshConnectionException e)
                {
                    ErrorHandling.error = "The connection was terminated unexpectedly. " + e.Message;

                    return false;
                }

                catch (Exception e)
                {
                    ErrorHandling.error = "There was an uncaught exception. " + e.Message;

                    return false;
                }
            }
        }
    }
}