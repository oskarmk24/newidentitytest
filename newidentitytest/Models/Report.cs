using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace newidentitytest.Models
{
    [Table("reports", Schema = "obstacledb")]
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
