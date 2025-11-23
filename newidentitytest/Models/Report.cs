using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace newidentitytest.Models
{
    /// <summary>
    /// Modell for hinderrapporter sendt inn av piloter.
    /// Rapporter kan ha status: "Draft" (utkast), "Pending" (venter på behandling), "Approved" (godkjent) eller "Rejected" (avslått).
    /// Kan tilordnes til en registerfører for behandling.
    /// Lagrer hinderinformasjon inkludert type, høyde, beskrivelse og lokasjon (GeoJSON-format).
    /// </summary>
    [Table("reports")]
    public class Report
    {
        /// <summary>
        /// Primærnøkkel for rapporten.
        /// Auto-generert av databasen.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Høyden på hinderet i meter.
        /// Nullable for å tillate rapporter uten spesifisert høyde (spesielt for utkast).
        /// Lagres som heltall (rundes av fra decimal ved opprettelse).
        /// </summary>
        public int? ObstacleHeight { get; set; }

        /// <summary>
        /// Beskrivelse av hinderet.
        /// Lagres som TEXT i databasen.
        /// Nullable for å tillate rapporter uten beskrivelse (spesielt for utkast).
        /// </summary>
        [Column(TypeName = "text")]
        public string? ObstacleDescription { get; set; }

        /// <summary>
        /// Lokasjon til hinderet lagret som GeoJSON-streng.
        /// Kan være enten Point (punkt) eller LineString (linje) basert på brukerens valg på kartet.
        /// Lagres som LONGTEXT i databasen.
        /// Påkrevd for alle rapporter (inkludert utkast).
        /// Format: JSON-streng med "type" og "coordinates" (f.eks. {"type":"Point","coordinates":[lng,lat]}).
        /// </summary>
        [Column(TypeName = "longtext")]
        public string? ObstacleLocation { get; set; }

        /// <summary>
        /// Type hinder (valgt via knapper i skjemaet).
        /// Maksimal lengde: 100 tegn.
        /// Nullable for å tillate rapporter uten type (spesielt for utkast).
        /// </summary>
        [MaxLength(100)]
        public string? ObstacleType { get; set; }
        
        /// <summary>
        /// ID til piloten som sendte inn rapporten.
        /// Refererer til ApplicationUser.Id (Identity-bruker).
        /// Maksimal lengde: 255 tegn.
        /// Nullable, men settes alltid ved opprettelse av nye rapporter.
        /// </summary>
        [MaxLength(255)]
        public string? UserId { get; set; }
        
        /// <summary>
        /// ID til registerføreren som er tildelt for å behandle rapporten.
        /// Refererer til ApplicationUser.Id for en bruker med Registrar-rolle.
        /// Valgfritt - kan være null hvis rapporten ikke er tildelt en registerfører ennå.
        /// Maksimal lengde: 255 tegn.
        /// </summary>
        [MaxLength(255)]
        public string? AssignedRegistrarId { get; set; }

        /// <summary>
        /// Tidsstempel for når rapporten ble opprettet.
        /// Auto-generert av databasen med CURRENT_TIMESTAMP som standardverdi.
        /// Brukes for å sortere rapporter (nyeste først).
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Status for rapporten i arbeidsflyten.
        /// Mulige verdier: "Draft" (utkast), "Pending" (venter på behandling), "Approved" (godkjent), "Rejected" (avslått).
        /// Standardverdi er "Pending" for nye rapporter, "Draft" for utkast.
        /// Maksimal lengde: 50 tegn.
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Begrunnelse for avslag av rapporten.
        /// Kun brukt når Status er "Rejected".
        /// Lagres som TEXT i databasen.
        /// Nullable - null for alle andre statuser.
        /// Nullstilles når rapporten godkjennes (Status endres til "Approved").
        /// </summary>
        [Column(TypeName = "text")]
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Tidsstempel for når rapporten ble behandlet (godkjent eller avslått).
        /// Settes til nåværende tid (UTC) når Status endres til "Approved" eller "Rejected".
        /// Nullable - null for "Draft" og "Pending" statuser.
        /// Brukes for å spore når behandlingen skjedde.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }
    }
}
