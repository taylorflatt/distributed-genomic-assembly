using System.Collections.Generic;

namespace Genome.Helpers
{
    public class CustomRoles
    {
        public const string Administrator = "Admin";
        public const string Verified = "Verified";
        public const string Unverified = "Unverified";

        // Start at 0 to mimic the database indexing of roles. Likewise, when we want Count, we call Count - 1.
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