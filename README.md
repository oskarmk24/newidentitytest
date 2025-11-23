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
                - Registerfører kan godkjenne/avslå/slette rapporter.
                - Registerfører kan tildele rapporter til andre registerførere.
                - Rapporter har status-visning (pending, approved, rejected, draft)
- Organisasjoner:
                - Brukere kan meldes inn i organisasjoner av registerørere/admin.
                - Organisasjonsledere kan logge inn og få oversikt over rapporter meldt inn av deres besetning.
- Notifikasjoner:
                - Piloter mottar varsel på dashbord når innsendt rapport har blitt behandlet og hvem som har behandlet den.
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
  4. Godkjenn/avslå med begrunnelse eller slett rapporten
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

### Sikkerhetstesting

#### Test 1: CSRF-beskyttelse
- **Beskrivelse**: Verifiserer at alle POST-endepunkter krever gyldig anti-forgery token
- **Testmetode**: 
  - Sendt POST-forespørsel via browser console uten `__RequestVerificationToken`:ascript
    fetch('/Obstacle/DataForm', {
      method: 'POST',
      body: JSON.stringify({ObstacleType: 'Point'}),
      headers: {'Content-Type': 'application/json'}
    })
    - **Forventet resultat**: Forespørsel avvises med 400 Bad Request
- **Faktisk resultat**: Fungerer som forventet - mottok `400 (Bad Request)` når token manglet


#### Test 2: XSS-beskyttelse
- **Beskrivelse**: Verifiserer at brukerinput ikke kan injisere skadelig JavaScript
- **Testmetode**: 
  - Lagt inn `<script>alert('XSS Test')</script>` i beskrivelsesfeltet ved opprettelse av rapport
  - Sendt inn rapporten og åpnet den for visning
- **Forventet resultat**: Input blir automatisk HTML-encoded av Razor Views, vises som tekst
- **Faktisk resultat**: Fungerer som forventet - script-taggen vises som ren tekst (`<script>alert('XSS Test')</script>`), ingen alert-dialog ble vist


#### Test 3: SQL Injection-beskyttelse
- **Beskrivelse**: Verifiserer at database-spørringer er parameteriserte
- **Testmetode**: 
  - Lagt inn `'; DROP TABLE reports;` i beskrivelsesfeltet ved opprettelse av rapport
  - Sendt inn rapporten og verifisert i database
- **Forventet resultat**: Entity Framework Core bruker parameteriserte queries automatisk, SQL-kode behandles som tekst
- **Faktisk resultat**: Fungerer som forventet - SQL-koden vises som ren tekst, ingen tabeller ble påvirket, alle rapporter er fortsatt til stede


#### Test 4: Autorisasjon og tilgangskontroll

**Test 4a: Rollebasert tilgang**
- **Beskrivelse**: Verifiserer at brukere kun kan aksessere ressurser de har tilgang til
- **Testmetode**: 
  - Logget inn som Pilot (`pilot.nla1@nla.no`)
  - Forsøkt å aksessere Registrar-endepunkter direkte via URL
- **Forventet resultat**: Pilot får 403 Forbidden ved forsøk på Registrar-funksjoner
- **Faktisk resultat**: Fungerer som forventet - mottok "Access denied - You do not have access to this resource"

**Test 4b: Tilgang til andres data**
- **Beskrivelse**: Verifiserer at piloter kan se andres rapporter, men ikke modifisere dem
- **Testmetode**: 
  - Logget inn som Pilot 1, opprettet rapport (ID 7)
  - Logget inn som Pilot 2, forsøkt å aksessere rapport 7
  - Forsøkt å slette rapport via direkte API-kall
- **Forventet resultat**: 
  - Pilot kan se andres rapporter (read-only)
  - Ingen delete/edit-knapper vises for piloter
  - Direkte API-kall til Delete-endepunkt gir 403 Forbidden
- **Faktisk resultat**: Fungerer som forventet - piloter kan se, men ikke modifisere andres rapporter

**Test 4c: Uautentisert tilgang**
- **Beskrivelse**: Verifiserer at uautentiserte brukere ikke kan aksessere beskyttede ressurser
- **Testmetode**: 
  - Logget ut av applikasjonen
  - Forsøkt å aksessere beskyttede sider direkte via URL:
    - `/Pilot`
    - `/Obstacle/DataForm`
    - `/Reports`
    - `/Registrar`
- **Forventet resultat**: 
  - Uautentiserte brukere omdirigeres til `/Identity/Account/Login`
  - Ingen tilgang til beskyttet innhold
- **Faktisk resultat**: Fungerer som forventet - Uautentiserte brukere omdirigeres til `/Identity/Account/Login`


#### Test 5: Input-validering
- **Beskrivelse**: Verifiserer at server-side validering fungerer korrekt
- **Testmetode**: 
  - **5a**: Prøv å sende inn tomme påkrevde felt (Obstacle Location)
  - **5b**: Prøv å legge inn for lang tekst i beskrivelsesfeltet
  - **5c**: Prøv å legge inn spesialtegn og edge cases
- **Forventet resultat**: 
  - Server-side validering avviser ugyldig input med feilmeldinger
  - Data annotations på modeller (f.eks. `[Required]`, `[MaxLength]`) validerer input
- **Faktisk resultat**: Fungerer som forventet - Server-side validering avviser ugyldig input med feilmeldinger, Data annotations på modeller (f.eks. `[Required]`, `[MaxLength]`) validerer input og alle spesialtegn vises korrekt i beskrivelsesfeltet, god Unicode-støtte


#### Test 6: Passordhåndtering
- **Beskrivelse**: Verifiserer at passord lagres sikkert
- **Testmetode**: 
  - Opprettet ny testbruker via registreringssiden med passord `TestPassword123!`
  - Inspisert database via Adminer (`http://localhost:8081`)
  - Kjørte SQL-spørring: `SELECT Id, Email, PasswordHash FROM AspNetUsers WHERE Email = 'test@email.com'`
- **Forventet resultat**: 
  - Passord lagres som hashet i databasen (ikke klartekst)
  - PasswordHash er en lang hash-streng (100+ tegn)
  - ASP.NET Core Identity håndterer passordhashing automatisk med PBKDF2
- **Faktisk resultat**: Alt fungerer som forventet.


#### Test 7: Session-håndtering
- **Beskrivelse**: Verifiserer at sessions håndteres sikkert
- **Testmetode**: 
  - Inspisert cookies i DevTools (Application tab) etter innlogging
  - Sjekket cookie-egenskaper for `.AspNetCore.Identity.Application`
  - Testet session timeout ved inaktivitet
- **Forventet resultat**: 
  - Cookies er HttpOnly (ikke tilgjengelig via JavaScript)
  - Cookies er Secure i produksjon (kun HTTPS)
  - Cookies har SameSite satt (Lax eller Strict)
  - ASP.NET Core Identity håndterer sessions automatisk
- **Faktisk resultat**: Fungerer som forventet
  - HttpOnly: ✓ (satt korrekt)
  - Secure: (ikke satt i utviklingsmiljø - forventet for localhost)
  - SameSite: `Lax` (satt korrekt)
  - Expires: `Session` (utløper ved lukking av nettleser)
  - Session timeout fungerer som forventet



  ## Brukervennlighetstesting

### Testmetode
- Selvtest gjennomført med fokus på navigasjon, skjema, responsivt design og feilhåndtering
- Testet på ulike enheter (tablet (IPad Mini, IPad), desktop) via browser DevTools

### Testscenarier

#### Scenario 1: Pilot-oppgaver
- **Beskrivelse**: Test av pilotens hovedoppgaver og brukeropplevelse
- **Testmetode**: 
  - Logget inn som pilot og gikk gjennom komplett flyt:
    - Se dashboard med oversikt over rapporter og notifikasjoner
    - Opprett ny hindrerapport med punkt/linje på kart
    - Lagre utkast og redigere det senere
    - Se egne rapporter med sortering
    - Se notifikasjoner når rapporter behandles
    - Se godkjente hinder på kart
- **Resultat**: 
  - Dashboard gir god oversikt over status
  - Rapportskjema er intuitivt med tydelige instruksjoner
  - Kartet fungerer godt på mobil med fullscreen-funksjonalitet
  - Notifikasjoner er tydelige og informative
  - "My Reports" gir god oversikt over egne rapporter

#### Scenario 2: Registerfører-oppgaver
- **Beskrivelse**: Test av registerførerens arbeidsflyt og effektivitet
- **Testmetode**: 
  - Logget inn som registerfører og testet:
    - Se dashboard med oversikt
    - Se pending rapporter med sortering/søk
    - Åpne rapportdetaljer
    - Godkjenn/avslå rapporter med begrunnelse
    - Tildele rapporter til andre registerførere
    - Slette rapporter
    - Se alle rapporter med avansert sortering
- **Resultat**: 
  - Arbeidsflyten er effektiv og logisk
  - Pending-rapporter er lett å finne
  - Rapportbehandling er enkel med tydelige knapper
  - Sortering og søk fungerer godt
  - Tildeling av rapporter er intuitivt

#### Scenario 3: Admin-oppgaver
- **Beskrivelse**: Test av admin-funksjoner og administrasjon
- **Testmetode**: 
  - Logget inn som admin og testet:
    - Se dashboard
    - Administrere brukere (liste, detaljer, tildel rolle)
    - Administrere roller (opprett, se detaljer, slett)
    - Administrere organisasjoner (CRUD-operasjoner)
    - Se alle rapporter
    - Behandle rapporter (godkjenn/avslå/slett)
- **Resultat**: 
  - Alle administrasjonsfunksjoner er tilgjengelige
  - Brukeradministrasjon er oversiktlig
  - Rolle- og organisasjonsadministrasjon er intuitivt
  - Full tilgang til alle funksjoner fungerer som forventet

#### Scenario 4: OrganizationManager-oppgaver
- **Beskrivelse**: Test av organisasjonslederens oppgaver
- **Testmetode**: 
  - Logget inn som organisasjonsleder og testet:
    - Se dashboard
    - Se rapporter fra organisasjonsmedlemmer
    - Se organisasjonsdetaljer
    - Se rapporter sortert/filtrert
- **Resultat**: 
  - Oversikt over organisasjonens rapporter er god
  - Det er enkelt å finne informasjon om organisasjonen
  - Dashboard gir relevant oversikt

#### Scenario 5: Tverrgående funksjoner
- **Beskrivelse**: Test av funksjoner som gjelder alle roller
- **Testmetode**: 
  - Testet navigasjon, responsivt design og feilhåndtering
  - Testet på tablet (iPad) og desktop
- **Resultat**: 
  - Navigasjonen er intuitiv med tydelige menyer for alle roller
  - Applikasjonen fungerer godt på alle enheter
  - Feilmeldinger er tydelige og hjelper brukeren
  - Responsivt design tilpasser seg godt til ulike skjermstørrelser

### Identifiserte forbedringsområder

- Kan vurdere å legge til ekstra tilbakeknapper i prosjektet for bedre brukervennlighet dersom det ikke viser seg å være intuitivt å bruke Digital Obstacle Report - tittel som universal "Home" knapp.

- Kan vurderes å gjøre hinder registrering mer brukervennlig med tanke på størrelsen av knapper.

- Kan vurderes å forbedre metoden for piloter å navigere kartet med oversikt over godkjente hinder ettersom det fortsatt er mulighet for piloter å markere punkt der.

### Konklusjon
Applikasjonen fremstår som brukervennlig med intuitiv navigasjon, tydelige skjemaer og god responsivt design. Alle hovedfunksjoner er lett tilgjengelige og fungerer som forventet.