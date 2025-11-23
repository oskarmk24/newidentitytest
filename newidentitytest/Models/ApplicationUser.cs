using Microsoft.AspNetCore.Identity;

namespace newidentitytest.Models
{
    /// <summary>
    /// Tilpasset brukerklasse som utvider IdentityUser med organisasjonsstøtte.
    /// Brukes som grunnlag for ASP.NET Core Identity i applikasjonen.
    /// Gir alle standard Identity-funksjoner (autentisering, autorisering, roller) pluss mulighet til å tildele brukere til organisasjoner.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Foreign key til organisasjonen som brukeren tilhører.
        /// Nullable for å tillate brukere uten organisasjon.
        /// Hvis organisasjonen slettes, settes denne verdien til null (DeleteBehavior.SetNull).
        /// </summary>
        public int? OrganizationId { get; set; }

        /// <summary>
        /// Navigation property til brukerens organisasjon.
        /// Virtual for å støtte lazy loading i Entity Framework Core.
        /// Nullable for å reflektere at brukeren kan være uten organisasjon.
        /// Dette er den inverse siden av Organization.Users-relasjonen.
        /// </summary>
        public virtual Organization? Organization { get; set; }
    }
}