using Renci.SshNet;

namespace Genome.Helpers
{
    public class LinuxErrorHandling
    {
        // Returns true if there is a command error. Returns false is there isn't an error with the command.
        /// <summary>
        /// Determines if there was an error with a particular linux command.
        /// </summary>
        /// <param name="cmd">The command that was run.</param>
        /// <param name="error">A string representing an error that is being sent out.</param>
        /// <returns></returns>
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