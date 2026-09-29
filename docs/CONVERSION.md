# BloodLine conversion map

## Inputs inspected

The uploaded `New folder.zip` contained a Flutter desktop archive, a Flask server archive and four small Flutter FAQ/contact prototypes. The desktop scope was selected, as requested. The mobile application was not included in the conversion.

The main source references were `blood_line_desktop/lib/pages`, `lib/widgets`, `lib/services`, `lib/theme/app_theme.dart`, and the Flask server's `app.py`. The original code defines three desktop roles: Admin, Manager and Staff. Its brand red is `#BC1F34`.

## Feature mapping

| Original desktop feature | MVC implementation |
| --- | --- |
| Login and role-specific main screens | `AccountController`, Identity cookie authentication, role-aware navigation |
| Admin home | `HomeController`: pending requests, registered banks and account totals |
| Registration request form | `RegistrationController.Create`: organization, manager, location and operating hours |
| Approve/reject registration | `RegistrationController.Review`: approval creates a bank and manager inside a transaction |
| FAQ add/list/delete and edit prototype | `FaqController`: admin create/edit/delete; authenticated users can read |
| Manager staff list/add/delete | `StaffController`, bank-scoped Identity accounts and invitation links |
| Manager contact information | `BankController.Edit`, including coordinates and operating hours |
| Staff dashboard | Bank-level donor, appointment, stock and event totals |
| Blood groups and unit withdrawal | `InventoryController`, batches by group/expiry, earliest-expiry issue and issue history |
| Today's appointments | `AppointmentsController.Index`, date/status filters |
| Open and cancel appointment | `AppointmentsController.Transition`, validated state transitions |
| Complete donation | `DonationService.Complete`, observations, donor group update, donation record and inventory batch in one transaction |
| Donor list | `DonorsController.Index`, search and blood-group filter |
| Donation history | `DonorsController.History`, scoped to the current bank |
| Volunteer list | Donors with `IsVolunteer=true`, scoped to the current bank |
| Events | `EventsController`: list, create, edit and delete |
| Blood need dialog | `NeedsController`: blood group, units, hospital, location and expiry |
| Profile and password change | `AccountController.Profile` and `.Password` |
| Forgotten password | Identity reset tokens, email links and custom MVC views |

## Deliberate changes

- The output is a **browser application**, not a packaged Windows desktop executable.
- Flutter widgets were rewritten as Razor views. The logo and red/white theme are retained, but this is not a pixel-for-pixel Flutter reproduction.
- Flask's JWT API is replaced by MVC forms and Identity cookies. The existing mobile client cannot connect to these controllers as a drop-in API replacement.
- SQLite/EF Core provides a fresh, self-contained local database. The original schema, IDs, password hashes and records are not imported. No original credentials or live data are included.
- Unified Identity users replace the separate admin/manager/staff authentication tables.
- Secure password-setting links replace generated passwords and numeric verification codes sent by email. Development messages go to a local ignored outbox; production requires SMTP.
- Staff can create/edit donor records and book appointments to demonstrate the desktop flow without the mobile client. These are additions to make the standalone MVC project usable.
- The original global donor relationships are simplified into **bank-owned donor records**. Cross-bank donor following, mobile registration, donor ranking changes, push notifications and donor self-service are outside this desktop conversion.
- Location coordinates are editable numeric fields; an interactive mapping provider is not integrated.
- Profile text/date fields are supported; profile-image upload is not implemented.
- Blood inventory keeps a separate expiry per donation batch, rather than combining all stock of a blood group under one expiry. Staff supply the expiry date. Expired batches are visible but cannot be issued.
- Donation observations are stored, not interpreted as clinical eligibility rules. No blood compatibility recommendation is automated.
- Public form rate limits use the directly connected IP address; configure trusted reverse-proxy forwarding if deploying behind a proxy.

## Architecture

Controllers handle requests, validation and role checks. `BankScope` resolves a bank from the signed-in account, never from a posted bank ID. Entities live in `Models`, input forms in `ViewModels`, and data access in `Data/AppDbContext`. `DonationService` owns inventory transactions. `MailService` handles SMTP or the Development-only outbox.

Appointment completion has a concurrency token and a unique donation-per-appointment index. Stock batches have concurrency tokens. These controls prevent repeated completion and detect overlapping modifications. SQLite serializes write transactions. This is a single-instance portfolio deployment design, not a high-throughput distributed system.

## Maintaining the project

- `EnsureCreated` bootstraps a fresh database. It does not evolve an existing schema; adopt EF migrations before maintaining a deployed database.
- Identity security stamps invalidate deleted/changed accounts on its periodic cookie validation; this is not instant revocation across open sessions.
- Production deployment, real SMTP delivery, Windows-specific execution and migration of the original database require separate verification.
- Tests and screenshots use fictional records only.
