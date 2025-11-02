using Microsoft.AspNetCore.Identity;

namespace newidentitytest.Models
{
    /// <summary>
    /// Custom user class that extends IdentityUser with organization support.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Foreign key to the Organization this user belongs to.
        /// Nullable to allow users without an organization.
        /// </summary>
        public int? OrganizationId { get; set; }

        /// <summary>
        /// Navigation property to the user's organization.
        /// </summary>
        public virtual Organization? Organization { get; set; }
    }
}