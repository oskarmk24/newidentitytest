using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace newidentitytest.Models
{
    /// <summary>
    /// Modell for in-app notifikasjoner til brukere.
    /// Brukes for å informere brukere om hendelser relatert til deres rapporter,
    /// for eksempel når en rapport er godkjent, avslått eller slettet.
    /// Notifikasjoner kan markeres som lest og har en tidsstempel for når de ble lest.
    /// </summary>
    [Table("notifications")]
    public class Notification
    {
        /// <summary>
        /// Primærnøkkel for notifikasjonen.
        /// Auto-generert av databasen.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID til brukeren som skal motta notifikasjonen.
        /// Refererer til ApplicationUser.Id (Identity-bruker).
        /// Påkrevd felt med maksimal lengde på 255 tegn.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string UserId { get; set; } = null!;

        /// <summary>
        /// ID til rapporten som notifikasjonen er relatert til.
        /// Refererer til Report.Id.
        /// Påkrevd felt. Kan referere til en slettet rapport (notifikasjonen kan eksistere etter at rapporten er slettet).
        /// </summary>
        [Required]
        public int ReportId { get; set; }

        /// <summary>
        /// Tittel på notifikasjonen.
        /// Kort beskrivelse av notifikasjonen, for eksempel "Report #123 approved".
        /// Påkrevd felt med maksimal lengde på 200 tegn.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        /// <summary>
        /// Detaljert melding for notifikasjonen.
        /// Kan inneholde lengre beskrivelse, for eksempel avslagsbegrunnelse.
        /// Lagres som TEXT i databasen. Nullable for å tillate notifikasjoner uten detaljert melding.
        /// </summary>
        [Column(TypeName = "text")]
        public string? Message { get; set; }

        /// <summary>
        /// Indikerer om notifikasjonen er lest av brukeren.
        /// Standardverdi er false (ulest).
        /// Settes til true når brukeren markerer notifikasjonen som lest.
        /// </summary>
        [Required]
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Tidsstempel for når notifikasjonen ble opprettet.
        /// Auto-generert av databasen med CURRENT_TIMESTAMP som standardverdi.
        /// Brukes for å sortere notifikasjoner (nyeste først).
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Tidsstempel for når notifikasjonen ble markert som lest.
        /// Nullable - null hvis notifikasjonen ikke er lest ennå.
        /// Settes til nåværende tid (UTC) når brukeren markerer notifikasjonen som lest.
        /// </summary>
        public DateTime? ReadAt { get; set; }
    }
}

