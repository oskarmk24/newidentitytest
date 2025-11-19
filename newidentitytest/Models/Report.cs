using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace newidentitytest.Models
{
    // MariaDB/MySQL don't support EF Core schemas, so remove 'Schema = "obstacledb"'
    [Table("reports")]
    public class Report
    {
        [Key]
        public int Id { get; set; }

        public int? ObstacleHeight { get; set; }

        [Column(TypeName = "text")]
        public string? ObstacleDescription { get; set; }

        [Column(TypeName = "longtext")]
        public string? ObstacleLocation { get; set; }

        [MaxLength(100)]
        public string? ObstacleType { get; set; }
        
        [MaxLength(255)]
        public string? UserId { get; set; }
        
        // Registrar assignment (optional)
        [MaxLength(255)]
        public string? AssignedRegistrarId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }

        // Status field: Pending, Approved, Rejected
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // Reason for rejection (only used when Status = "Rejected")
        [Column(TypeName = "text")]
        public string? RejectionReason { get; set; }

        // Timestamp when status was last updated
        public DateTime? ProcessedAt { get; set; }
    }
}
