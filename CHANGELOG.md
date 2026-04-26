# Changelog

All notable changes to this project will be documented in this file.

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
