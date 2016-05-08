using System;
using System.Linq;
using Genome.Models;
using System.ComponentModel.DataAnnotations;

// TODO: Implement this in reflection so it can be used generically with different types.

namespace Genome.Helpers
{
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
                        return new ValidationResult(ErrorMessageString);
                }

                return ValidationResult.Success;
            }
        }
    }
}