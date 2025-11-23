namespace newidentitytest.Models
{
    /// <summary>
    /// ViewModel for detaljvisning av en rapport.
    /// Brukes av ReportsController.Details() for å pakke rapportdata sammen med avsenderinformasjon.
    /// Kombinerer Report-entiteten med løst avsendernavn (e-post eller brukernavn) for visning.
    /// </summary>
    public class ReportDetailsViewModel
    {
        /// <summary>
        /// Fullstendig Report-entitet med all hinderinformasjon.
        /// Inkluderer alle felter fra databasen: type, høyde, beskrivelse, lokasjon, status, etc.
        /// </summary>
        public Report Report { get; set; } = null!;

        /// <summary>
        /// Visningsnavn for avsenderen (pilot) som sendte inn rapporten.
        /// Løses fra ApplicationUser basert på Report.UserId.
        /// Bruker e-postadresse hvis tilgjengelig, ellers brukernavn, eller "(unknown)" hvis brukeren ikke finnes.
        /// Brukes for å vise hvem som sendte inn rapporten i detaljvisningen.
        /// </summary>
        public string Sender { get; set; } = string.Empty;
    }
}
