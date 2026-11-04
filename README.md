# RaceDay – PROG6212 Part 3 (MVC front end, Azure Blob Storage, Docker)

RaceDay lets **Organisers** create race events (run / walk / cycle) with categories, see enrolments and capture results, and lets **Participants** browse events, enter a category, and view their own results.
Part 3 adds the MVC front end (`RaceDay.Web`) on top of the Part 2 RESTful API (`RaceDay.API`).

| Project | What it is |
|---|---|
| `RaceDay.API` | ASP.NET Core 8 Web API, EF Core Code-First, SQL Server (LocalDB), Swagger, Azure Blob Storage, session-based auth |
| `RaceDay.Web` | ASP.NET Core 8 MVC front end that calls the API (Dockerfile included) |
| `RaceDayAPI.Tests` | xUnit tests (success, failure and role checks) |

## The two roles

**Organiser** – dashboard, create / edit / delete own events (with banner image), add / delete categories, view all enrolments for own events, capture finish times and positions, edit profile.

**Participant** – home, browse events, event details with **Enter Event**, **My Events**, **My Results** (finish time and positions), edit profile (with profile picture).

Login stores the user's id and role in the session. The menu is different for each role, pages are protected by role, unauthenticated users are redirected to Login, and Logout clears the session.

## Azure Blob Storage

Event banners and profile pictures are uploaded from the MVC app to the API (`POST /api/events/{id}/upload-banner`, `POST /api/users/upload-profile-picture`). The API stores the file in Blob Storage, saves the blob URL in the database, and the MVC pages display the image from that URL.

* Real Azure: put your storage account connection string in `RaceDay.API` → `ConnectionStrings:AzureBlobStorage` (use User Secrets, do not commit it).
* Local emulator: start Azurite and keep the default value `UseDevelopmentStorage=true`:

```
docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0 --skipApiVersionCheck
```

## Setup and run (Visual Studio)

Requirements: Visual Studio 2022 with .NET 8, SQL Server LocalDB, Docker Desktop (for Azurite / Docker).

1. Open `RaceDay.API/RaceDay.API.sln`.
2. The database is created automatically on first start (EF migrations, `DefaultConnection` in `RaceDay.API/appsettings.json`).
3. Start Azurite (or configure real Azure) as described above.
4. Right-click the solution → **Configure Startup Projects** → **Multiple startup projects** → `RaceDay.API` = Start and `RaceDay.Web` = Start. Use the **https** profile for both.
   * API (Swagger): `https://localhost:7057/swagger`
   * Web: `https://localhost:7182`
5. `RaceDay.Web/appsettings.json` → `ApiSettings:BaseUrl` must match the API address (`https://localhost:7057/`).

## Run with Docker (RaceDay.Web)

Start `RaceDay.API` first (step 4 above; it also listens on `http://localhost:5282`), then from the repository root:

```
docker build -t raceday-web -f RaceDay.Web/Dockerfile RaceDay.Web
docker run --rm -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5282/ raceday-web
```

Open `http://localhost:8080`. The container reaches the API on your PC through `host.docker.internal`.

## Unit tests

```
dotnet test RaceDayAPI.Tests/RaceDayAPI.Tests.csproj
```

## GitHub Actions CI

`.github/workflows/main.yml` restores, builds and runs all unit tests on every push.

![Green build](RaceDay.API/docs/github-actions-build.png)

## Links

* YouTube demo: https://youtu.be/_0MrGwneH58
* GitHub: https://github.com/matetemagomane/PROG_POE_PART-2.git
