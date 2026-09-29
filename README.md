# BloodLine — Blood Bank Management

<p align="center"><img src="src/BloodLine.Web/wwwroot/images/logoWithTitle.png" alt="BloodLine" width="280" /></p>

<p align="center"><strong>ASP.NET Core MVC · C# · Entity Framework Core · SQLite</strong><br/>A workspace for blood banks, staff and administrators.</p>

An ASP.NET Core MVC conversion of the BloodLine desktop course project. The supplied original contains a **Flutter/Dart desktop interface and a Flask/Python API**. This version replaces both with C# controllers, Razor views, Entity Framework Core and ASP.NET Core Identity.

Developed from a project completed during a private course. The conversion was prepared with AI assistance. Original BloodLine branding is retained; the web layout adapts the desktop screens for a browser.

## Features

- **Admin:** overview, review bank-registration requests, approve/reject applications, invite managers, create/edit/delete FAQs, edit profile and password.
- **Manager:** bank overview, invite/remove staff, update bank contacts, coordinates and opening hours, edit profile and password.
- **Staff:** donor search and editing, volunteer list, appointment booking and filtering, Pending → Open → Complete/Canceled workflow, donation observations/history, inventory by blood group and expiry, stock issues, events and blood needs.
- Role-based authorization and bank-scoped operations, server-side form validation, anti-forgery protection, hashed passwords, login lockout, password reset links and rate limiting for public forms.
- Donation completion and inventory updates share one database transaction. Stock issues use earliest-expiring unexpired batches and are recorded in an audit table.

## Run on Windows

1. Install the **.NET 10 SDK** from https://dotnet.microsoft.com/download/dotnet/10.0.
2. Extract this ZIP, open the `BloodLine.Mvc` folder, and open PowerShell there.
3. Run:

```powershell
# Choose your own password: 12+ characters, uppercase, lowercase, number, symbol.
$env:Seed__Password = Read-Host 'Choose a local demo password'
$env:Seed__DemoData = 'true'
dotnet restore BloodLine.sln
dotnet run --project src/BloodLine.Web
```

4. Open **http://localhost:5080**.
5. Sign in with `admin@example.test`, `manager@example.test`, or `staff@example.test`, using the password you chose.

The seed password and demo switch are needed only to initialize a new empty database. The seed uses fictional data and runs only once. Do not reuse a personal password. Demo accounts are created only in Development when `Seed__DemoData=true`; otherwise the first run creates only the administrator. Set `Seed__AdminEmail` before the first run to choose another admin email.

You can also open `BloodLine.sln` in a Visual Studio version supporting .NET 10 with the **ASP.NET and web development** workload. Set BloodLine.Web as the startup project. Use environment variables or user secrets for `Seed:Password` before the first run.

### macOS / Linux

```bash
export Seed__Password='replace-with-your-own-strong-password'
export Seed__DemoData=true
dotnet run --project src/BloodLine.Web
```

## Try the application

1. Sign in as staff. Open today's appointment, record a donation, and check that O+ inventory increases once.
2. Open the donor's history. Check the recorded observations.
3. Issue stock to a fictional recipient organization. Check the issue history and remaining units.
4. Sign out. Submit a bank-registration request with a different test email.
5. Sign in as admin, approve it, and inspect the generated invitation in `src/BloodLine.Web/App_Data/mail/`.
6. Open the invitation's link, set the manager password, then invite a staff member from the manager account.

## Project organization

| Folder | Purpose |
| --- | --- |
| `src/BloodLine.Web/Controllers` | MVC actions and role/bank authorization |
| `src/BloodLine.Web/Models` | Domain entities and validation |
| `src/BloodLine.Web/ViewModels` | Restricted form input models |
| `src/BloodLine.Web/Views` | Razor pages for each controller |
| `src/BloodLine.Web/Data` | EF Core context and fictional seed data |
| `src/BloodLine.Web/Services` | Donation/inventory transactions, bank scope, email |
| `src/BloodLine.Web/wwwroot` | CSS and original BloodLine logo assets |
| `tests/BloodLine.Tests` | Executable regression tests for donation/inventory rules |
| `docs` | Conversion scope, implementation notes and verification results |
| `.github/workflows` | Build and regression-test workflow |

## Database and email

SQLite creates `bloodline.db` in the web project's working directory. This portfolio version uses `EnsureCreated` for a fresh schema. It **does not import or modify the original Flask database**, and it does not migrate existing data when the entity schema changes. Back up data before schema changes; use proper EF migrations for a maintained deployment. Never delete a database you need to keep.

When SMTP is absent in Development, emails are written to `App_Data/mail/`. This folder contains private password-setting links and is excluded from Git. Production requires SMTP configuration; there is no production file-outbox fallback.

Set configuration through environment variables or .NET user secrets:

```text
Mail__Host
Mail__Port                # default: 587
Mail__EnableSsl           # default: true
Mail__Username
Mail__Password
Mail__From
PublicBaseUrl            # externally reachable origin for password links
ConnectionStrings__DefaultConnection
AllowedHosts             # deployment hostname(s)
```

Reset links use `PublicBaseUrl`, not an untrusted request Host header. Do not commit credentials, live databases, or account emails. Original `.env` files, user data, Flutter caches and binaries are not included.

## Build and test

```bash
dotnet build BloodLine.sln --configuration Release
dotnet run --project tests/BloodLine.Tests --configuration Release
```

The test project is a small executable regression suite (not an xUnit project); a failed assertion exits with a nonzero status. GitHub Actions runs the same commands.

## Conversion scope

See [the conversion map](docs/CONVERSION.md). This is the **desktop staff/admin/manager scope**, not a replacement for the mobile donor app or a compatible implementation of its original API. Coordinates replace the embedded map widget; reset links replace numeric email codes. No real donor records or original credentials are bundled. Date/time inputs use the server's local time, while registration and stock-issue audit timestamps use UTC. Configure the server timezone appropriately.

This is a course/portfolio application, not a clinically validated blood-bank system. Expiry dates are entered by staff; no medical eligibility or compatibility decisions are automated.

## GitHub

Upload the contents of this folder as source files, not the ZIP itself. Keep `.gitignore` and `docs/CONVERSION.md`. The included workflow builds and tests every push. No GitHub repository is created merely by downloading or running this project.

## Attribution

Original source and logo assets: the supplied BloodLine private-course project. Preserve any co-author or course attribution that applies. No new open-source license is assigned to the original material; choose one only if you hold the necessary rights. Framework dependencies retain their own licenses.
