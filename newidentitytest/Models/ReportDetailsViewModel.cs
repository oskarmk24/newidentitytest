namespace newidentitytest.Models
{
    public class ReportDetailsViewModel
    {
        // Full database entity for the selected report
        public Report Report { get; set; } = null!;
        // Sender display text (resolved from Identity user)
        public string Sender { get; set; } = string.Empty;
    }
}
