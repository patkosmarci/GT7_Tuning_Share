# GT7 Tuning Share

A web application for **Gran Turismo 7** players to publish, browse, rate, and discuss
their car tuning setups.

Built as a semester project on [Orchard Core](https://orchardcore.net/) (ASP.NET Core
CMS), with a custom module that adds GT7-specific content types, a public-facing UI,
ratings, comments, and a personal activity dashboard.

---

## What it does

- **Publish a setup**: pick a car from the in-game catalog (575 cars seeded from a
  community dataset), fill in any of the ~50 tunable parameters across nine sections
  (Body, Tyres, Aerodynamics, Suspension, Differential, Transmission, Brakes, Driving
  aids, Notes), optionally pick an engine swap, attach a description and a recommended
  track, and publish.
- **Browse setups**: a chrome-less dark UI mirroring the GT7 in-game tuning sheet,
  with a filterable + sortable list (by date, by rating count, by comment count) and a
  rich detail view per setup.
- **Rate and discuss**: registered users can rate any setup with a 5-star widget
  (AJAX, no page reload), and post comments. Comments support pasted images
  (clipboard → upload → inline render).
- **Track your activity**: every signed-in user has a `/my` page listing setups they
  have created, rated, or commented on, filterable by section, with delete actions
  per item.

---

## Running it locally

### Prerequisites
- **.NET SDK 8.0** (or newer)
- A local HTTPS dev cert: `dotnet dev-certs https --trust` (one-time, requires admin)

### First run
```bash
dotnet run --project src/GT7TuningShare
```

Then open `https://localhost:5001` (or `http://localhost:5000`) in your browser.

### Initial setup wizard
On first run Orchard Core shows a setup wizard. Recommended choices:

| Field        | Value                |
| ------------ | -------------------- |
| Recipe       | **Blog**             |
| Database     | **SQLite**           |
| Database name| `OrchardCore.db`     |
| Site name    | anything             |
| Admin user   | choose any username  |

After setup completes, do **two more one-time steps in the admin panel**
(`/Admin`):

1. **Configuration → Features** — enable **GT7 Tuning Share** (this triggers the cars
   catalog seed; takes ~10–30 s).
2. **Configuration → Features** — enable **Users Registration** (so non-admin users
   can sign up).
3. **Content → Content Items** — delete the seeded **Blog** content item (it's set as
   homepage by the Blog recipe; deleting it lets `/` route to our setups list).

After this you're done — visit `/` for the setups list.

---

## Public URLs

| URL                          | What it does                                           |
| ---------------------------- | ------------------------------------------------------ |
| `/`                          | Setups list (search, filter by car, 4 sort options)    |
| `/setups/{id}`               | Setup details — full read-only tuning sheet, ratings, comments |
| `/setups/create`             | Form to publish a new setup (auth required)            |
| `/my`                        | The signed-in user's own setups, ratings, comments     |
| `/Login`, `/Register`        | Built-in Orchard Core auth pages                       |
| `/Admin`                     | Orchard Core admin (admin role required)               |

---

## Architecture

### Solution layout

```
GT7_Tuning_Share/
├── GT7TuningShare.sln
├── src/
│   └── GT7TuningShare/                    
│       ├── Program.cs                     
│       ├── appsettings.json               DB connection
│       └── NLog.config                    Logging configuration
└── Modules/
    └── GT7TuningShare.Module/             Custom Orchard Core module
        ├── Manifest.cs                    Module metadata
        ├── Startup.cs                     DI registrations: parts, drivers, services, indexes
        ├── Migrations.cs                  Content-type definitions + index tables
        ├── CarsSeeder.cs                  IModularTenantEvents — seeds cars on first activation
        ├── Models/                        
        │   ├── CarPart.cs                 Catalog entry per car
        │   ├── CarSetupPart.cs            tunable parameters
        │   ├── RatingPart.cs              Aggregate rating cache (avg + count)
        │   ├── RatingRecord.cs            Per-user rating row
        │   └── CommentRecord.cs           Per-user comment row
        ├── Indexes/                       
        │   ├── RatingIndex.cs
        │   └── CommentIndex.cs
        ├── Drivers/                       Display drivers (admin editor + display)
        │   ├── CarPartDisplayDriver.cs
        │   └── CarSetupPartDisplayDriver.cs
        ├── Services/                      Business logic
        │   ├── RatingService.cs           Upsert + recompute average
        │   ├── CommentService.cs          Add, list, delete
        │   └── EngineSwapCatalog.cs       Loads engineswaps.csv at startup
        ├── Controllers/
        │   └── SetupsController.cs        Public MVC — list, details, create, /my
        ├── Api/                           AJAX endpoints (auth required, no antiforgery)
        │   ├── RatingsApiController.cs    POST /setups/rate
        │   ├── CommentsApiController.cs   POST /setups/comment, GET /setups/comment
        │   └── CommentImageApiController.cs  POST /setups/comment-image
        ├── ViewModels/
        ├── Views/
        │   ├── _ViewImports.cshtml        
        │   ├── CarPart.{Edit,}.cshtml     Admin-only car editor + display
        │   ├── CarSetupPart.{Edit,}.cshtml  Admin-only setup editor + display
        │   └── Setups/                    
        │       ├── Index.cshtml           Setups list
        │       ├── Details.cshtml         Setup detail page
        │       ├── Create.cshtml          New-setup form
        │       └── MyActivity.cshtml      Signed-in user's dashboard
        ├── Assets/                        Embedded resources (CSV data)
        │   ├── cars.csv                   575 cars
        │   ├── maker.csv                  82 manufacturers
        │   └── engineswaps.csv            217 engine-swap options
        └── wwwroot/
            └── car_thumbnails/            565 PNG car thumbnails (~21 MB, served at /GT7TuningShare.Module/car_thumbnails/)
```

### Key architectural decisions

- **Content items vs. plain records**:
  - **Setups, Cars** → Orchard Core *content items*
  - **Ratings, Comments** → plain *YesSql records* with map indexes
- **Aggregate rating cached on the setup** (`RatingPart.AverageRating` + `RatingCount`)
  so listing pages don't need to recompute averages from raw rating rows on every render.
- **Cars seeded once** on feature activation, idempotent (`CarsSeeder` checks for any
  existing Car content item and skips if present).
- **AJAX endpoints are *not* under `/api/`**: Orchard Core's authentication pipeline
  treats `/api/` routes as Bearer-token protected by default, which would block our
  cookie-authenticated users. So rating/comment endpoints live at `/setups/rate` etc.

### Content type schema (managed by `Migrations.cs`)

```
Car          : TitlePart + CarPart                          (seeded catalog)
CarSetup     : TitlePart + CarSetupPart + RatingPart        (user-published setups)
```

Migrations evolve the schema across versions:
- **v1** — define `CarPart` and `Car` type
- **v2** — define `CarSetupPart` and `CarSetup` type
- **v3** — define `RatingPart`, attach to `CarSetup`, create `RatingIndex` table
- **v4** — create `CommentIndex` table

---

## Data sources & credits

- **Car catalog & engine swaps**: [ddm999/gt7info](https://github.com/ddm999/gt7info)
  (`cars.csv`, `maker.csv`, `engineswaps.csv`).
- **Car thumbnail images**
  `https://www.gran-turismo.com/common/dist/gt7/carlist/car_thumbnails/car{ID}.png`.