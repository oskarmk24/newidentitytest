using System.ComponentModel.DataAnnotations;

namespace newidentitytest.Models
{
    /// <summary>
    /// Represents an organization that users can belong to.
    /// </summary>
    public class Organization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property - users belonging to this organization
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}

