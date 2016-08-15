using Renci.SshNet;

namespace Genome.Helpers
{
    public class LinuxErrorHandling
    {
        public static string error { get; set; }

        /// <summary>
        /// Determines if there was an error with a particular linux command.
        /// </summary>
        /// <param name="cmd">The command that was run.</param>
        /// <returns>Returns true if there is an error otherwise false.</returns>
        public static bool CommandError(SshCommand cmd)
        {
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