using System.ComponentModel.DataAnnotations;

namespace newidentitytest.Models
{
    /// <summary>
    /// Modell for organisasjoner som brukere kan tilhøre.
    /// Støtter en-til-mange-relasjon med ApplicationUser (en organisasjon kan ha mange brukere).
    /// Hvis organisasjonen slettes, settes OrganizationId til null for tilknyttede brukere (DeleteBehavior.SetNull).
    /// Brukes for å gruppere brukere og rapporter etter organisasjon.
    /// </summary>
    public class Organization
    {
        /// <summary>
        /// Primærnøkkel for organisasjonen.
        /// Auto-generert av databasen.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Navn på organisasjonen.
        /// Påkrevd felt med maksimal lengde på 200 tegn.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Beskrivelse av organisasjonen.
        /// Valgfritt felt med maksimal lengde på 500 tegn.
        /// Nullable for å tillate organisasjoner uten beskrivelse.
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Tidsstempel for når organisasjonen ble opprettet.
        /// Auto-generert av databasen med CURRENT_TIMESTAMP som standardverdi.
        /// Brukes for å spore når organisasjonen ble lagt til i systemet.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property til alle brukere som tilhører denne organisasjonen.
        /// Virtual for å støtte lazy loading i Entity Framework Core.
        /// Dette er den inverse siden av ApplicationUser.Organization-relasjonen.
        /// En-til-mange-relasjon: en organisasjon kan ha mange brukere.
        /// </summary>
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}

