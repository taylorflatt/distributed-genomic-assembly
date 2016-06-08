using System;
using System.Linq;
using Genome.Models;
using System.ComponentModel.DataAnnotations;

// TODO: Implement this in reflection so it can be used generically with different types.

namespace Genome.Helpers
{
    /// <summary>
    /// Check the dawgtag entered on the Registration page to make sure that it is unique.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class UniqueDawgTag : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            using (GenomeAssemblyDbContext context = new GenomeAssemblyDbContext())
            {
                // Grab all the dawgtags.
                var userList = context.Users
                    .Select(u => new { DawgTag = u.DawgTag }).ToList();

                // Compare the dawgtags against the entered dawgtag.
                foreach (var u in userList)
                {
                    if (u.DawgTag == Convert.ToInt32(value))
                        return new ValidationResult("That dawgtag is already associated with a user.");
                }

                return ValidationResult.Success;
            }
        }
    }

    /// <summary>
    /// Check the email address entered on the Registration page to make sure that it is (1) a valid email address, 
    /// (2) an SIU email address, and (3) not already registered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class UniqueEmailAddress : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // First check if the email address is a valid email address.
            EmailAddressAttribute emailFormat = new EmailAddressAttribute();
            if (emailFormat.IsValid(value))
            {
                // Check if the email has @siu.edu at the end of the email.
                if (Convert.ToString(value).Split('@')[1].Equals("siu.edu"))
                {
                    // Check that the email is currently not in the database.
                    using (GenomeAssemblyDbContext context = new GenomeAssemblyDbContext())
                    {
                        // Grab all the emails.
                        var userList = context.Users
                            .Select(u => new { Email = u.Email }).ToList();

                        // Compare the emails against the entered email.
                        foreach (var u in userList)
                        {
                            if (u.Email.Equals(value))
                                return new ValidationResult("That email is already associated with a user.");
                        }

                        return ValidationResult.Success;
                    }
                }

                else
                    return new ValidationResult("The email address must be a valid SIU email address ending wiht @siu.edu.");
            }

            else
                return new ValidationResult(ErrorMessage);
        }
    }
}