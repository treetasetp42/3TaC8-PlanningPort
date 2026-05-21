# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 2026-05-21
### Added
- **Gemini 2.5 Flash Controller**: Added new `ChatController` to serve as the unified AI conversational gateway.
- **Strict Polite Tone Directives**: Programmed the controller to structure `systemInstruction` parameters forcing a strict polite female Thai persona (actively forbidding generic/mixed or male hara-style responses).
- **Page Context Adaptation**: Modified request bindings to accept a `CurrentPath` DTO parameter. If provided, it appends active routing metadata directly to the prompt instruction, yielding hyper-focused contextual assistance (e.g. watchlist guidelines, cash deposit advice).
- **Environment Key Fallback**: Programmed the controller to search for `GEMINI_API_KEY` environment variables as fallback, to prevent missing secret configurations on raw hosting containers.
## [1.0.1] - 2026-05-16
### Added
- **Runtime Upgrade**: Migrated from .NET 8.0 to **.NET 10.0 (LTS)** for long-term Azure support.
- **Health Check Endpoint**: Added `HealthController` for server pre-warming and monitoring.
- **Infrastructure**: Configured Zip Deploy and SCM Basic Auth for reliable Azure deployment.

### Changed
- **Dependencies**: Upgraded EF Core, JwtBearer, and Swashbuckle to version 10.0.0.
- **Security**: Implemented null-safety checks in `Program.cs` and `UserController.cs` to handle missing environment variables gracefully.
- **Refactoring**: Resolved several null-reference warnings and strict mode compliance issues in `AdminController.cs`.
+
## [1.0.0] - 2026-04-26
### Added
- Startup logging/checkpoints to troubleshoot server initialization.
- Detailed professional README.md with bilingual support.

### Fixed
- Azure SQL connectivity issue by adding `EnableRetryOnFailure` to the database configuration.
- CORS configuration to support Vercel production domains.
- Hardcoded localhost link in password reset email (now uses dynamic `FrontendUrl` configuration).

## [0.4.0] - 2026-04-22
### Added
- Integrated Finnhub API service for fetching stock profiles and real-time quotes.
- Added `StockService` to handle external data fetching.

## [0.3.0] - 2026-04-18
### Added
- Google OAuth access token validation logic.
- Refresh Token support for maintaining persistent user sessions.

## [0.2.0] - 2026-04-14
### Added
- ASP.NET Core Identity integration with Entity Framework Core.
- Automated data seeding for roles, permissions, and default admin account.

## [0.1.0] - 2026-04-10
### Added
- Initial Web API project setup with .NET 8.
- Base database schema and EF Core Migrations.
- Standard controllers for basic CRUD operations.
