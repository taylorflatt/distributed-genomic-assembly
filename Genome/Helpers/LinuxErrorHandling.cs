using Renci.SshNet;

namespace Genome.Helpers
{
    public class LinuxErrorHandling
    {
        // Returns true if there is a command error. Returns false is there isn't an error with the command.
        public static bool CommandError(SshCommand cmd, out string error)
        {
            error = "";

            // There is an error return the error so we can display it to the user.
            if (!string.IsNullOrEmpty(cmd.Error))
            {
                error = cmd.Error;

                return true;
            }

            return false;
        }
    }
}