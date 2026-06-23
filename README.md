# ClassMate AI — Setup Guide
## Phase 1: Google OAuth + Classroom API

---

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18.17+ | https://nodejs.org |
| PostgreSQL | 15+ | https://postgresql.org or Docker |
| Docker (optional) | any | for Postgres |

---

## Step 1 — Google Cloud Console setup

1. Go to https://console.cloud.google.com
2. Create a new project (e.g. `classmate-ai`)
3. **Enable APIs** (APIs & Services → Library):
   - Google Classroom API
   - Google Drive API
   - People API (for teacher names, optional)
4. **Create OAuth credentials** (APIs & Services → Credentials → Create Credentials → OAuth client ID):
   - Application type: **Web application**
   - Authorised JavaScript origins: `http://localhost:3000`
   - Authorised redirect URIs: `http://localhost:3000/api/auth/callback/google`
5. Copy the **Client ID** and **Client Secret** — you'll need both below.
6. **OAuth consent screen**: set to External (testing), add your Gmail as test user.

---

## Step 2 — PostgreSQL

Option A — Docker (easiest):
```bash
docker run --name classmate-pg -e POSTGRES_USER=classmate-pg -e POSTGRES_PASSWORD=secret -p 5432:5432 -d postgres:15
```

Option B — local install, then create the DB:
```sql
CREATE DATABASE classmate_db;
```

---

## Step 3 — Backend (.NET)

### 3a. File location
```
classmate-ai/
└── backend/
    └── ClassmateApi/          ← all backend files live here
        ├── ClassmateApi.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── Controllers/
        │   ├── AuthController.cs
        │   └── ClassroomController.cs
        ├── Services/
        │   ├── ClassroomService.cs
        │   └── UserService.cs
        ├── Data/
        │   └── AppDbContext.cs
        ├── DTOs/
        │   └── Dtos.cs
        └── Middleware/
            └── ErrorHandlingMiddleware.cs
```

### 3b. Fill in appsettings.json
Edit `backend/ClassmateApi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=classmate_db;Username=postgres;Password=secret"
  },
  "Jwt": {
    "Secret": "run: openssl rand -base64 32   ← paste output here",
    "Issuer": "classmate-api",
    "Audience": "classmate-frontend",
    "ExpiryHours": "24"
  },
  "Google": {
    "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  },
  "Frontend": {
    "Url": "http://localhost:3000"
  }
}
```

### 3c. Run EF migrations and start
```bash
cd backend/ClassmateApi

# Restore packages
dotnet restore

# Create first migration
dotnet ef migrations add InitialCreate

# Apply migration (or it auto-applies on startup in dev)
dotnet ef database update

# Start API (runs on http://localhost:5000)
dotnet run
```

Swagger UI is available at: http://localhost:5000/swagger

---

## Step 4 — Frontend (Next.js)

### 4a. File location
```
classmate-ai/
└── frontend/
    ├── package.json
    ├── next.config.js
    ├── tailwind.config.ts
    ├── tsconfig.json
    ├── .env.local.example     ← copy to .env.local and fill in
    └── src/
        ├── app/
        │   ├── layout.tsx
        │   ├── providers.tsx
        │   ├── globals.css
        │   ├── login/
        │   │   └── page.tsx          ← /login route
        │   ├── dashboard/
        │   │   └── page.tsx          ← /dashboard route
        │   └── api/auth/
        │       └── [...nextauth]/
        │           └── route.ts      ← NextAuth handler
        ├── components/
        │   └── classroom/
        │       ├── CourseCard.tsx
        │       └── AssignmentList.tsx
        ├── lib/
        │   └── api.ts                ← typed API client
        └── types/
            └── index.ts              ← shared TypeScript types
```

### 4b. Create .env.local
```bash
cd frontend
cp .env.local.example .env.local
```

Edit `.env.local`:
```env
GOOGLE_CLIENT_ID=YOUR_CLIENT_ID.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=YOUR_CLIENT_SECRET
NEXTAUTH_SECRET=paste-your-openssl-output-here
NEXTAUTH_URL=http://localhost:3000
NEXT_PUBLIC_API_URL=http://localhost:5000
```

### 4c. Install and start
```bash
npm install
npm run dev
```

Visit http://localhost:3000 — you'll be redirected to /login.

---

## Auth flow (what happens end-to-end)

```
1. User clicks "Continue with Google" on /login
2. NextAuth redirects to Google consent screen
   → Scopes: openid, email, profile, classroom.readonly, drive.readonly
3. Google redirects back to /api/auth/callback/google
4. NextAuth jwt() callback fires:
   → Sends { idToken, accessToken, refreshToken } to POST /api/auth/google
5. .NET verifies the ID token against Google's JWKS
6. .NET upserts the User row (stores refresh token for background jobs)
7. .NET returns its own JWT
8. JWT stored in NextAuth session
9. All subsequent API calls: Authorization: Bearer <jwt>
10. .NET reads userId from JWT sub claim for every request
```

---

## API endpoints ready after Phase 1

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/google` | Exchange Google tokens → backend JWT |
| GET | `/api/classroom/me` | Current user profile + usage |
| GET | `/api/classroom/courses` | All active courses + settings |
| GET | `/api/classroom/courses/{courseId}/assignments` | Assignments for a course |
| PUT | `/api/classroom/settings` | Save auto-solve toggle + delay |

---

## What comes in Phase 2

- BullMQ job queue (needs Redis — add to Docker compose)
- Assignment text extraction from Drive files (PDF/Docs → text)
- AI orchestration (OpenAI / Gemini with 5-use free tier gate)
- Draft storage and review UI
- Classroom submission API call
- Email notification via SendGrid
- API key management + Stripe billing
