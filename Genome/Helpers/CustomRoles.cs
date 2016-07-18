using System.Collections.Generic;

namespace Genome.Helpers
{
    public class CustomRoles
    {
        public const string Administrator = "Admin";
        public const string Verified = "Verified";
        public const string Unverified = "Unverified";

        /// <summary>
        /// List of roles for all users.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, string> Roles()
        {
            Dictionary<int, string> roles = new Dictionary<int, string>();

            roles.Add(0, Administrator);
            roles.Add(1, Verified);
            roles.Add(2, Unverified);

            return roles;
        }
    }
}