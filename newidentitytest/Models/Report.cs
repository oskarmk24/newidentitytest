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

        [Required]
        [MaxLength(200)]
        public string ObstacleName { get; set; } = string.Empty;

        public int? ObstacleHeight { get; set; }

        [Column(TypeName = "text")]
        public string? ObstacleDescription { get; set; }

        [Column(TypeName = "longtext")]
        public string? ObstacleLocation { get; set; }

        [MaxLength(255)]
        public string? UserId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }
    }
}