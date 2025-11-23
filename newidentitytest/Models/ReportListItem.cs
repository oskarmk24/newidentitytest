namespace newidentitytest.Models
{
    /// <summary>
    /// ViewModel for listevisning av rapporter.
    /// Brukes i ReportsController.Index(), PilotController.MyReports() og OrganizationController.Reports()
    /// for å vise en forenklet versjon av rapporter med løst avsender- og organisasjonsnavn.
    /// Data hentes via LINQ joins mellom Reports, Users og Organizations.
    /// </summary>
    public class ReportListItem
    {
        /// <summary>
        /// Primærnøkkel for rapporten.
        /// Samme som Report.Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Tidsstempel for når rapporten ble opprettet.
        /// Auto-generert av databasen.
        /// Brukes for sortering i listevisninger.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Visningsnavn for avsenderen (pilot) som sendte inn rapporten.
        /// Løses fra ApplicationUser basert på Report.UserId via join.
        /// Bruker e-postadresse hvis tilgjengelig, ellers brukernavn, eller "(unknown)" hvis brukeren ikke finnes.
        /// </summary>
        public string Sender { get; set; } = string.Empty;

        /// <summary>
        /// Navn på organisasjonen som avsenderen tilhører.
        /// Løses via join mellom Users og Organizations basert på avsenderens OrganizationId.
        /// Nullable - null hvis avsenderen ikke tilhører en organisasjon.
        /// </summary>
        public string? OrganizationName { get; set; }

        /// <summary>
        /// Type hinder som ble rapportert.
        /// Nullable - null hvis ikke spesifisert (spesielt for utkast).
        /// Maksimal lengde: 100 tegn.
        /// </summary>
        public string? ObstacleType { get; set; }

        /// <summary>
        /// Status for rapporten i arbeidsflyten.
        /// Mulige verdier: "Draft" (utkast), "Pending" (venter på behandling), "Approved" (godkjent), "Rejected" (avslått).
        /// Standardverdi er "Pending".
        /// </summary>
        public string Status { get; set; } = "Pending";
        
        /// <summary>
        /// Lokasjon til hinderet lagret som GeoJSON-streng.
        /// Kan være enten Point (punkt) eller LineString (linje).
        /// Format: JSON-streng med "type" og "coordinates" (f.eks. {"type":"Point","coordinates":[lng,lat]}).
        /// Nullable - null hvis lokasjon ikke er spesifisert.
        /// </summary>
        public string? ObstacleLocation { get; set; }
    }
}
