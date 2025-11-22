# Digital Obstacle Report

En ASP.NET Core MVC-applikasjon for rapportering av hinder i luftrommet. Systemet lar piloter rapportere hinder, registerførere godkjenne/avslå rapporter, og organisasjonsledere administrere sine organisasjoner.


## Innhold

- Hvordan kjøre prosjektet
- Funksjonaliteter
- Systemarkitektur
- Drift
- Testing
- Sikkerhet


## Hvordan kjøre prosjektet

- Forutsetninger:
                - Docker Desktop installert

1. Åpne terminal.
3. cd (mappen hvor compose filen ligger)
4. Kjør `docker compose up --build`
5. Åpne http://localhost:5173


- Innlogging

Admin: admin@example.com Passord: Admin123!

Pilot: 
- pilot.nla1@nla.no Passord: Pilot123!
- pilot.nla2@nla.no Passord: Pilot123!

- pilot.luftforsvaret1@mil.no Passord: Pilot123!
- pilot.luftforsvaret2@mil.no Passord: Pilot123!

- pilot.politiet1@politiet.no Passord: Pilot123!

Registerfører:
- registrar1@kartverket.no Passord: Registrar123!
- registrar2@kartverket.no Passord: Registrar123!

OrganizationManager:
- manager@kartverket.no Passord: Manager123!
- manager@nla.no Passord: Manager123!
- manager@luftforsvaret.no Passord: Manager123!
- manager@politiet.no Passord: Manager123!



## Funksjonalitet

- Identitetshåndtering: Registrering av nye brukere som lagres i database.

- Autentisering: Innlogging med rollebasert tilgang.

- Rapportering: 
                - Pilot kan sende inn rapporter som lagres i egen tabell i databasen.
                - Kartskjema støtter punkt og linje.
                - Piloter kan lagre rapporter som utkast.
- Behandling av rapporter:
                - Registerfører kan godkjenne/avslå rapporter.
                - Registerfører kan tildele rapporter til andre registerførere.
                - Rapporter har status-visning (pending, approved, rejected, draft)
- Organisasjoner:
                - Brukere kan meldes inn i organisasjoner.
                - Organisasjonsledere kan logge inn og få oversikt over rapporter meldt inn av deres besetning.
- Notifikasjoner:
                - Piloter mottar varsel på dashbord hvis innsendt rapport har blitt behandlet.
- Oversikt over hinder:
                - Piloter og registerførere kan åpne kartet for å se godkjente hinder.

## Systemarkitektur

- Backend: ASP.NET Core MVC (.NET 9.0)
- Database: MariaDB 11
- ORM: Entity Framework Core
- Autentisering: ASP.NET Core Identity
- Frontend: Razor Views med Tailwind CSS
- Container løsning: Docker + Docker Compose


### Models

- ApplicationUser: Identity user med organisasjonsstøtte.
- Organization: Organisasjoner som brukere tilhører.
- Report: Hinderrapporter med status, lokasjon, beskrivelse osv.
- Notification: Notifikasjon som blir sendt til piloter.
- ObstacleData: View model for rapporteringsskjema.

### Views

- Home: Hjemmeside/Dashboard (Rolle-spesifikk redirect)
- Obstacle: 
            - DataForm: Skjema for rapportering av hinder.
            - Drafts: Oversikt over lagret utkast.
            - Overview: Bekreftelsesside etter innmelding av hinder.
- Reports:
            - Index: Liste over alle innsendte rapporter (med sortering/søk).
            - Details: Detaljevisning av enkelt rapport.
- Pilot:
            - Index: Pilot dashboard.
            - MyReports: Oversikt over innlogget pilot sine rapporter.
- Registrar:
            - Index: Registerfører dashboard.
            - Pending: Oversikt over ubehandlet rapporter.
- Organization:
            - Index: Liste over alle organisasjoner.
            - Details: Detaljvisning av spesifik organisasjon.
            - Create: Skjema for å opprette ny organisasjon.
            - Edit: Skjema for å redigere eksisterende organisasjon.
            - Delete: Bekreftelsesside for sletting.
            - Reports: Rapporter fra organisasjonsmedlemmer.
- OrganizationManager:
            - Index: Dashboard for organisasjonsledere.
- UserManagement:
            - Index: Liste over alle brukere.
            - Details: Detaljvisning av enkelt bruker.
- Role:
            - Index: Liste over alle roller.
            - Create: Skjema for å opprette ny rolle.
            - Details: Detaljvisning av rolle.
            - Delete: Bekreftelsesside for sletting av rolle.
            - ManageUserRoles: Tildeling av rolle til bruker.
- Shared:
            - _Layout: Hovedoppsett for alle sider.
            - _LoginPartial: Innloggingsmeny
            - _ValidationScriptsPartial: Validering av scripts.
            - Error: Feilside.

### Controllers

- HomeController: Hjemmeside og feilhåndtering
- ObstacleController: Håndtering av hinderrapporter
- ReportsController: Visning og behandling av rapporter
- RegistrarController: Dashboard for registerførere
- PilotController: Dashboard for piloter
- OrganizationController: Administrasjon av organisasjoner
- OrganizationManagerController: Dashboard for organisasjoner
- UserManagementController: Brukeradministrasjon
- RoleController: Administrasjon av roller.


### Data

- ApplicationDbContext: Entity Framework DbContext.
- DbSet<Report>: Rapporter.
- DbSet<Organization>: Organisasjoner.
- DbSet<Notification>: Notifikasjoner.
- DbSet<ApplicationUser>: Brukere (via Identity).



### Database Tabeller

- reports: Hinderrapporter.
- organizations: Organisasjoner.
- notifications: Notifikasjoner.
- AspNetUsers: Brukere (Identity).
- AspNetRoles: Roller (Identity).
- AspNetUserRoles: Kobling mellom bruker og rolle (Identity).
- AspNetRoleClaims: ekstra rettigheter for roller.
- AspNetUserClaims: ekstra rettigheter for brukere.
- AspNetUserTokens: autentiseringstokens.
- AspNetUserLogins: ekstern innlogging.


### Roller og Tilgangskontroll

- **Admin**: Full systemtilgang - kan administrere brukere, roller, organisasjoner og rapporter
- **Registrar**: Kan godkjenne/avslå rapporter, tildele rapporter, administrere organisasjoner, brukere og roller
- **Pilot**: Kan opprette rapporter, se egne rapporter, lagre utkast, se godkjente rapporter på kart
- **OrganizationManager**: Kan se oversikt over rapporter meldt inn av piloter som tilhører organisasjonen



## Testing

Scenario 1: Opprettelse av rapport
Beskrivelse: Pilot logger inn, fyller ut skjema og sender inn rapport
Steg:
  1. Logg inn som pilot
  2. Naviger til rapportskjema
  3. Fyll ut alle felter (type, høyde, beskrivelse)
  4. Marker posisjon på kart
  5. Klikk "Submit"
- Forventet resultat: Rapport lagres med status "Pending", vises på oversiktsside
- Faktisk resultat: Fungerer som forventet



Scenario 2: Kartintegrasjon
- Beskrivelse: Bruker markerer punkt/linje på kartet og sender inn lokasjon
- Steg:
  1. Åpne rapportskjema
  2. Klikk på kart for å plassere markør
  3. Dra markøren for å justere posisjon
  4. Send inn rapport
- Forventet resultat: Lokasjon lagres som GeoJSON, vises korrekt på kart ved visning
- Faktisk resultat: Fungerer som forventet


Scenario 3: Responsivt design
- Beskrivelse: Applikasjonen tilpasser seg ulike skjermstørrelser
- Steg:
  1. Åpne applikasjonen på desktop
  2. Test på tablet (DevTools i nettleser)
- Forventet resultat: Layout tilpasser seg, alle funksjoner tilgjengelige
- Faktisk resultat: Fungerer som forventet


Scenario 4: Rapportbehandling
- Beskrivelse: Registerfører godkjenner/avslår rapport
- Steg:
  1. Logg inn som registerfører
  2. Se liste over pending rapporter
  3. Åpne rapportdetaljer
  4. Godkjenn eller avslå med begrunnelse
- Forventet resultat: Rapportstatus oppdateres, notifikasjon sendes til pilot
- Faktisk resultat: Fungerer som forventet


Scenario 5: Draft-funksjonalitet
- Beskrivelse: Pilot lagrer utkast til rapport
- Steg:
  1. Fyll ut deler av skjema
  2. Klikk "Save Draft"
  3. Gå til drafts-oversikt
  4. Rediger draft
- Forventet resultat: Draft lagres, kan redigeres senere
- Faktisk resultat: Fungerer som forventet


Scenario 6: Organisasjonsoppsett
- Beskrivelse: Admin oppretter organisasjon og tildeler brukere
- Steg:
  1. Logg inn som admin
  2. Opprett ny organisasjon
  3. Tildel bruker til organisasjon
- Forventet resultat: Organisasjon opprettes, brukere kan tilordnes
- Faktisk resultat: Fungerer som forventet


## Sikkerhet

### Implementerte sikkerhetstiltak

- **Autentisering**: ASP.NET Core Identity for brukerhåndtering med passordhashing
- **Autorisering**: Rollebasert tilgangskontroll med `[Authorize]` attributter
- **CSRF-beskyttelse**: `[ValidateAntiForgeryToken]` på alle POST-endepunkter
- **Input-validering**: Data annotations på modeller og server-side validering
- **SQL Injection-beskyttelse**: Entity Framework Core parameteriserte queries
- **XSS-beskyttelse**: Automatisk HTML-encoding i Razor Views
- **HTTPS**: HTTPS-redirection i produksjon med HSTS