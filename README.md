# WebBlog

Egy egyszerű, de már több szerepkört és admin funkciókat is támogató blogplatform ASP.NET Core alapokon.  
A projektben a felhasználók bejegyzéseket olvashatnak, kommentelhetnek, regisztrálhatnak, bejelentkezhetnek, saját bejegyzéseket kezelhetnek, az admin pedig moderálhatja a tartalmat.

## Fő funkciók

### Nyilvános funkciók
- bejegyzések listázása
- bejegyzés részletező oldal
- kommentek megjelenítése
- új komment beküldése
- keresés bejegyzések között
- szűrés szerző szerint
- rendezés több szempont alapján

### Felhasználói funkciók
- regisztráció
- bejelentkezés / kijelentkezés
- JWT alapú hitelesítés
- új bejegyzés létrehozása
- saját bejegyzések listázása
- saját bejegyzés szerkesztése
- saját bejegyzés törlése
- belépett felhasználónál a komment szerzője automatikusan kitöltődik

### Admin funkciók
- admin oldal
- összes bejegyzés listázása
- bármely bejegyzés szerkesztése
- bármely bejegyzés törlése
- összes komment listázása
- kommentek moderálása / törlése

### Kiegészítő funkciók
- konténerizáció Dockerrel
- offline mód Service Workerrel
- sticky header
- Swagger alapú API dokumentáció és tesztelés

---

## Használt technológiák

### Backend
- ASP.NET Core Minimal API
- Entity Framework Core
- SQLite
- JWT Bearer Authentication
- Swagger / OpenAPI

### Frontend
- HTML
- CSS
- JavaScript

### Egyéb
- xUnit
- Docker
- Service Worker

---

## Projektstruktúra

```text
webblog-project/
├── backend/
│   ├── WebBlog.Api/
│   │   ├── Data/
│   │   ├── Models/
│   │   ├── Services/
│   │   ├── wwwroot/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── WebBlog.Api.csproj
│   └── WebBlog.Tests/
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── .gitignore
├── README.md
└── WebBlog.sln