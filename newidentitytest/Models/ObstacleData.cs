using System.ComponentModel.DataAnnotations;

namespace newidentitytest.Models
{
    /// <summary>
    /// Modell som brukes til å lagre data om et hinder.
    /// Dette er koblet til skjemaet brukeren fyller ut.
    /// </summary>
    public class ObstacleData
    {
        /// <summary>
        /// Navnet på hinderet.
        /// Må fylles ut og kan være maks 100 tegn.
        /// </summary>
        [Required(ErrorMessage = "Obstacle name is required.")]
        [MaxLength(100)]
        public string ObstacleName { get; set; }

        /// <summary>
        /// Høyden på hinderet i meter.
        /// Må fylles ut og være mellom 0 og 200.
        /// </summary>
        [Required(ErrorMessage = "Obstacle height is required.")]
        [Range(0, 200, ErrorMessage = "Height must be between {1} and {2} meters.")]
        public decimal ObstacleHeight { get; set; }

        /// <summary>
        /// En kort beskrivelse av hinderet.
        /// Må fylles ut og kan være maks 1000 tegn.
        /// </summary>
        [Required(ErrorMessage = "Obstacle Description is required.")]
        [MaxLength(1000)]
        public string ObstacleDescription { get; set; }

        /// <summary>
        /// Lokasjon (sted) til hinderet.
        /// Lagres som tekst (for eksempel lat/lng fra kartet).
        /// Må fylles ut og kan være maks 1000 tegn.
        /// </summary>
        [Required(ErrorMessage = "Map position is required.")]
        [MaxLength(1000)]
        public string ObstacleLocation { get; set; }
    }
}
