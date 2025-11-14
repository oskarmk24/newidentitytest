namespace newidentitytest.Models
{
    public class ReportListItem
    {
        // Primary key of the report row
        public int Id { get; set; }
        // Timestamp when the report was created (auto-filled by DB)
        public DateTime CreatedAt { get; set; }
        // Human-friendly sender label (email or username)
        public string Sender { get; set; } = string.Empty;
        // Selected obstacle type (optional legacy fallback)
        public string? ObstacleType { get; set; }
        // Workflow status as maintained by registrar (Pending/Approved/Rejected)
        public string Status { get; set; } = "Pending";
        
        // Stored GeoJSON-like location string (e.g., { type: "Point", coordinates: [lng, lat] })
        public string? ObstacleLocation { get; set; }
    }
}
