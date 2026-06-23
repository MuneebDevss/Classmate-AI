# ClassmateApi

ClassmateApi ek ASP.NET Core Web API hai jo Google Classroom data ko read karta hai, users ko JWT-based auth deta hai, aur classroom settings ko PostgreSQL mein store karta hai. Is repo mein backend service, EF Core models, migrations, middleware, aur integration tests shamil hain.

## Project Ka Maqsad

Is project ka maqsad students ke liye Google Classroom assignments ko ek structured API ke through expose karna hai. Backend:

- Google OAuth token verify karta hai.
- Apna JWT issue karta hai taake frontend secure requests bhej sake.
- Classroom courses aur assignments fetch karta hai.
- User-specific auto-solve settings store karta hai.
- Errors ko consistent JSON response mein convert karta hai.

## File Guide

- [ClassmateApi/Program.cs](ClassmateApi/Program.cs) app startup, dependency injection, authentication, CORS, Swagger, aur middleware pipeline define karta hai.
- [ClassmateApi/ClassmateApi.csproj](ClassmateApi/ClassmateApi.csproj) target framework, package references, aur project-level settings rakhta hai.
- [ClassmateApi/Data/AppDbContext.cs](ClassmateApi/Data/AppDbContext.cs) EF Core entities aur table mapping define karta hai.
- [ClassmateApi/DTOs/DTOs.cs](ClassmateApi/DTOs/DTOs.cs) request/response contracts rakhta hai jo frontend aur backend share karte hain.
- [ClassmateApi/Exceptions/AppExceptions.cs](ClassmateApi/Exceptions/AppExceptions.cs) custom exception types define karta hai jo HTTP status codes se map hoti hain.
- [ClassmateApi/Middleware/ErrorHandelingMiddleware.cs](ClassmateApi/Middleware/ErrorHandelingMiddleware.cs) unhandled exceptions ko standard JSON error response mein convert karta hai.
- [ClassmateApi/Services/UserService.cs](ClassmateApi/Services/UserService.cs) Google login, JWT issuance, user lookup, aur classroom settings management handle karta hai.
- [ClassmateApi/Services/ClassroomService.cs](ClassmateApi/Services/ClassroomService.cs) Google Classroom se courses aur assignments fetch karta hai.
- [ClassmateApi/Migrations/20250509000001_initialcreate.cs](ClassmateApi/Migrations/20250509000001_initialcreate.cs) initial database schema migration hai.
- [ClassmateApi/Migrations/Appdbcontextmodelsnapshot.cs](ClassmateApi/Migrations/Appdbcontextmodelsnapshot.cs) current EF model ka snapshot rakhta hai.
- [ClassmateApi.Tests/AuthControllerTests.cs](ClassmateApi.Tests/AuthControllerTests.cs) auth controller ke unit tests ke liye hai.
- [ClassmateApi.Tests/AuthIntegrationTests.cs](ClassmateApi.Tests/AuthIntegrationTests.cs) end-to-end auth flow validate karta hai.
- [ClassmateApi.Tests/UserServiceTest.cs](ClassmateApi.Tests/UserServiceTest.cs) user service logic cover karta hai.
- [ClassmateApi.Tests/TestHelpers.cs](ClassmateApi.Tests/TestHelpers.cs) testing helpers aur shared setup rakhta hai.

## Requirements

- .NET 8 SDK
- PostgreSQL database
- Google OAuth client credentials
- Google Classroom API access
- Google Drive API access

## Configuration

`ClassmateApi/appSettings.json` aur `ClassmateApi/appSettings.development.json` mein usually yeh values chahiye hoti hain:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Secret`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiryHours`
- `Google:ClientId`
- `Google:ClientSecret`
- `Frontend:Url`

## Run Karne Ka Tarika

1. Backend folder mein jao.
2. Dependencies restore karo.
3. Project run karo.

Example commands:

```bash
dotnet restore ClassmateApi.sln
dotnet run --project ClassmateApi/ClassmateApi.csproj
```

Development mode mein app Swagger ko `http://localhost:5000/swagger` ya `https://localhost:5001/swagger` par expose kar sakta hai, depending on launch profile.

## Database

Development mode mein app startup par migrations apply karne ki koshish karta hai. Agar local PostgreSQL available na ho to app crash nahi karta, lekin API features jo database par depend karte hain woh work nahi karenge jab tak database connect na ho.

Manual migration command:

```bash
dotnet ef database update --project ClassmateApi/ClassmateApi.csproj
```

## Tests

```bash
dotnet test ClassmateApi.sln
```

## Notes

- Controllers workspace snapshot mein visible nahi the, is liye README unko explicitly list nahi karta.
- `ClassmateApi.Tests` test project backend behavior ko verify karne ke liye alag rakha gaya hai.