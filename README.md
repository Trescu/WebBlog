# WebBlog

Egyszerű blogplatform webprogramozás projektfeladathoz.  
Az alkalmazás lehetővé teszi a bejegyzések listázását, egy konkrét bejegyzés megtekintését, valamint kommentek hozzáadását a bejegyzésekhez.

## A projekt célja

A projekt célja egy egyszerű, több rétegből álló webalkalmazás megvalósítása, amely REST API-t, relációs adatbázist, kliensoldali megjelenítést és automatizált teszteket használ.

A jelenlegi megvalósítás az alábbi fő funkciókat tartalmazza:

- bejegyzések listázása
- egy bejegyzés részletes megjelenítése
- kommentek listázása
- új komment beküldése bejegyzéshez

## Használt technológiák

### Backend
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Swagger / OpenAPI

### Frontend
- HTML
- CSS
- JavaScript

### Tesztelés
- xUnit
- ASP.NET Core integration testing
- SQLite in-memory tesztkörnyezet

### Verziókezelés
- Git
- GitHub

## Projektstruktúra

```text
webblog-project/
│
├── backend/
│   ├── WebBlog.Api/
│   │   ├── Data/
│   │   ├── Models/
│   │   ├── wwwroot/
│   │   ├── Program.cs
│   │   └── WebBlog.Api.csproj
│   │
│   └── WebBlog.Tests/
│       ├── CustomWebApplicationFactory.cs
│       ├── PostsApiTests.cs
│       └── WebBlog.Tests.csproj
│
├── README.md
└── .gitignore