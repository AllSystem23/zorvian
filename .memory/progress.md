# Progress

## 2026-01-06 Initial setup and deployment

### Done
- Created Nexora project (Flutter + .NET 9)
- Set up Firebase project (`nexora-hr`)
- Implemented Auth flow: Firebase Auth client-side → backend JWT exchange
- Implemented multi-tenancy with TenantId query filters
- Created Super Admin seeding logic (SeedService + SeedController)
- Deployed backend to Render (`nexora-9yal.onrender.com`) — Render hosts the .NET app, database is Neon PostgreSQL
  - Dockerfile multi-stage for linux-x64
  - Firebase credentials via Render Secret File
  - JWT secret, DB connection string as env vars
- Deployed frontend web to Firebase Hosting (`nexora-hr.web.app`)
  - Flutter web build with `--dart-define=API_URL`
- Super Admin `davilapicador@gmail.com` already exists in Neon DB (created prior to this session)
- Built Android APK (`app-release.apk`)
- Fixed GoRouter redirect after login (`router.refresh()`)
- Committed and pushed to GitHub (`master`)

### Pending
- iOS build and deployment (TestFlight/App Store)
- CI/CD pipeline (GitHub Actions)
- Custom domain for API and/or frontend
- Production hardening (rate limiting, IP restrictions on seed endpoint)
- Email/password reset flow
- Company onboarding flow completion
- Employee management features end-to-end
- Attendance, payroll, reports features
