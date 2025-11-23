namespace newidentitytest.Models
{
    /// <summary>
    /// ViewModel for feilsiden som vises når en uventet feil oppstår i applikasjonen.
    /// Brukes av HomeController.Error() for å vise feilinformasjon til brukeren.
    /// RequestId brukes for feilsøking og kan spores i logger.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Unik identifikator for forespørselen som forårsaket feilen.
        /// Settes til Activity.Current?.Id eller HttpContext.TraceIdentifier.
        /// Brukes for feilsøking ved å spore feilen i logger.
        /// Nullable for å tillate at den kan være tom hvis ikke tilgjengelig.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Beregnet egenskap som indikerer om RequestId skal vises i feilvisningen.
        /// Returnerer true hvis RequestId har en verdi, ellers false.
        /// Brukes i Error.cshtml for å betinget vise RequestId-informasjon.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
