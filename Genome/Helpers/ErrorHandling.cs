using Renci.SshNet;

namespace Genome.Helpers
{
    public static class ErrorHandling
    {
        public static string error { get; set; }

        /// <summary>
        /// Determines if there was an error with a particular linux command.
        /// </summary>
        /// <param name="cmd">The command that was run.</param>
        /// <returns>Returns true if there is an error otherwise false.</returns>
        /// <remarks> This doesn't seem infallable given some commands. Some return an error when there 
        /// really isn't one. Those should be checked appropriately and dealt with in their own methods.</remarks>
        public static bool CommandError(SshCommand cmd)
        {
            if (!string.IsNullOrEmpty(error = cmd.Error))
                return true;

            return false;
        }

        /// <summary>
        /// Determines if there is an error.
        /// </summary>
        /// <returns>Returns true if there is NO error, otherwise false.</returns>
        public static bool NoError()
        {
            if (string.IsNullOrEmpty(error))
                return true;
            else
                return false;
        }
    }
}