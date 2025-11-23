using System.ComponentModel.DataAnnotations;

namespace newidentitytest.Models
{
    /// <summary>
    /// ViewModel for skjemaet som brukes til å registrere hinder.
    /// Brukes i ObstacleController.DataForm() for å motta data fra skjemaet.
    /// Støtter både full innsending (alle felt påkrevd) og lagring som utkast (kun lokasjon påkrevd).
    /// Data konverteres til Report-entitet før lagring i databasen.
    /// </summary>
    public class ObstacleData
    {
        /// <summary>
        /// Type hinder (valgt via knapper i skjemaet).
        /// Påkrevd for full innsending, men ikke for utkast (drafts).
        /// Maksimal lengde: 100 tegn.
        /// Nullable for å tillate utkast uten type.
        /// </summary>
        [MaxLength(100)]
        public string? ObstacleType { get; set; }

        /// <summary>
        /// Høyden på hinderet i meter.
        /// Påkrevd for full innsending, men ikke for utkast (drafts).
        /// Må være mellom 0 og 200 meter hvis oppgitt.
        /// Standardverdi er 0 (brukes for utkast som ikke har spesifisert høyde).
        /// </summary>
        [Range(0, 200, ErrorMessage = "Height must be between {1} and {2} meters.")]
        public decimal ObstacleHeight { get; set; }

        /// <summary>
        /// Beskrivelse av hinderet.
        /// Påkrevd for full innsending, men ikke for utkast (drafts).
        /// Maksimal lengde: 1000 tegn.
        /// Nullable for å tillate utkast uten beskrivelse.
        /// </summary>
        [MaxLength(1000)]
        public string? ObstacleDescription { get; set; }

        /// <summary>
        /// Lokasjon til hinderet lagret som GeoJSON-streng.
        /// Kan være enten Point (punkt) eller LineString (linje) basert på brukerens valg på kartet.
        /// Påkrevd for både full innsending og utkast.
        /// Maksimal lengde: 1000 tegn (vanligvis mye kortere).
        /// Format: JSON-streng med "type" og "coordinates" (f.eks. {"type":"Point","coordinates":[lng,lat]}).
        /// </summary>
        [Required(ErrorMessage = "Map position is required.")]
        [MaxLength(1000)]
        public string ObstacleLocation { get; set; }
    }
}
