# RaceDay

RaceDay is an ASP.NET Core Web API targeting .NET 8, with SQL Server storage and an xUnit test project. The implementation follows the supplied `RaceDay_Schema.sql` and `API_Endpoint_Plan.md` while preserving the requested `RaceDay.API` and `RaceDay.Tests` project names.

## Requirements

- Visual Studio 2022 with the .NET 8 SDK, or the .NET 8 SDK and an IDE such as Visual Studio Code.
- SQL Server LocalDB (included with common Visual Studio workloads) or SQL Server.
- SQL Server Management Studio (SSMS) is recommended for running the supplied database script.

## 1. Create the database

1. Open `RaceDay_Schema.sql` in SSMS.
2. Connect to your local SQL Server instance.
3. Execute the script to create `RaceDayDB`, its tables, and sample data.

**Important:** the script drops and recreates the tables when rerun. Back up any data you need before running it again. I adjusted the `Enrolments` → `Participants` foreign key to `NO ACTION` because SQL Server rejects the original cascade setup as multiple cascade paths. This means a participant with enrolments cannot be deleted until those enrolments are removed; cancelling enrolments through the API is supported. The seeded password hashes (`HASHED_PASSWORD_1`, etc.) are placeholders, not valid login hashes. Create a real account through `POST /api/auth/register` to test authentication.

If your SQL Server instance is not LocalDB, update `ConnectionStrings:RaceDayDb` in `RaceDay.API/appsettings.json` to match your server. Do not commit real passwords or production secrets to source control.

## 2. Open and run

1. Open `RaceDay.sln` in Visual Studio.
2. Allow NuGet packages to restore.
3. Confirm the `RaceDay.API` connection string points to the database you created.
4. Set `RaceDay.API` as the startup project and run it.
5. In Development, Swagger opens at `https://localhost:7184/swagger` (or use the HTTP URL shown in `Properties/launchSettings.json`).

For the command line, from the `RaceDay` directory:

```powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet test RaceDay.sln
dotnet run --project RaceDay.API
```

## 3. JWT configuration

The included JWT key is a development-only placeholder. Before sharing or deploying the application, replace it with a securely generated random key of at least 32 bytes, preferably through user-secrets or environment variables rather than a committed settings file. The API issues JWTs valid for four hours. In Swagger, click **Authorize** and enter `Bearer YOUR_TOKEN` to call protected endpoints.

## Implemented endpoint groups

- Authentication: `POST /api/auth/register`, `POST /api/auth/login`
- User profile: `GET/PUT /api/users/me`
- Events: list, detail, create, update, delete under `/api/events`
- Categories: event category listing and organiser-owned category CRUD
- Enrolments: participant enrolment/cancellation, participant history, organiser event enrolment list
- Results: organiser capture/update, participant history, public event results

Ownership checks are performed by the API for event, category, enrolment, and result operations. The test project includes initial controller tests; run the test suite in your .NET environment to verify it there.

## Notes

- The supplied schema is the source of truth for table names and relationships.
- This project uses EF Core SQL Server and the existing SQL schema; it does not automatically create the database at startup.
- No frontend was supplied, so this package contains the API and tests only.
