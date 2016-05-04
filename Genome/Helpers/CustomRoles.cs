using System.Collections.Generic;

namespace Genome.Helpers
{
    public class CustomRoles
    {
        public const string Administrator = "Admin";
        public const string Verified = "Verified";
        public const string Unverified = "Unverified";

        public static Dictionary<int, string> Roles()
        {
            Dictionary<int, string> roles = new Dictionary<int, string>();

            roles.Add(1, Administrator);
            roles.Add(2, Verified);
            roles.Add(3, Unverified);

            return roles;
        }
    }
}