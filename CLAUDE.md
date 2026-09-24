# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

TheBugTracker: a multi-tenant ticket/bug tracker built with ASP.NET Core MVC (.NET 10), ASP.NET Identity, EF Core, and PostgreSQL. Single project (`TheBugTracker.csproj`); there is no test project, so `make test` / `dotnet test` currently has nothing to run.

## Commands

Local dependencies (Postgres 18 on :5432, MailCatcher SMTP :1025 / web UI :1080) come from Docker:

- `make up` / `make down` / `make down-v` (the last also drops the `pgdata` volume)
- `make run` — `dotnet run --project TheBugTracker.csproj`
- `make watch` — `dotnet watch` with hot reload
- `dotnet build`
- Migrations: `dotnet ef migrations add <Name> -o Data/Migrations` and `dotnet ef database update` (EF Tools are referenced)

## Architecture

- **Startup (`Program.cs`)**: registers `ApplicationDbContext` (Npgsql, split-query behavior), Identity with `BTUser`/`IdentityRole` (`RequireConfirmedAccount = true`, custom `BTUserClaimsPrincipleFactory`), and every service as scoped against an interface in `Services/Interfaces`. `IEmailSender` is implemented by `EmailService` (MailKit) and configured by the `MailSettings` section.
- **Startup side effects**: `DataUtils.ManageDataAsync` runs on every boot. It applies pending migrations (`MigrateAsync`), then seeds roles, companies, default and demo users, lookup tables (ticket type/status/priority, project priority), projects, and tickets. Demo credentials come from `appsettings.json` keys `Demo*Username` / `Demo*Password`.
- **Connection string**: `DataUtils.GetConnectionString` uses `ConnectionStrings:DefaultConnection`, but `DATABASE_URL` (Heroku-style URI) overrides it when set.
- **Multi-tenancy**: every `BTUser` belongs to a `Company`. The company id is added as a claim by `BTUserClaimsPrincipleFactory` and read via `Extensions/IdentityExtensions.cs`. Services and controllers scope queries by that company id, so keep that scoping when adding queries.
- **Layers**: Controllers → Services (`ProjectService`, `TicketService`, `RoleService`, `CompanyInfoService`, `InviteService`, `NotificationService`, `FileService`, `LookupService`, `TicketHistoryService`) → `ApplicationDbContext`. `TicketHistoryService.AddHistoryAsync(oldTicket, newTicket, userId)` diffs ticket property changes into `TicketHistory` rows, so ticket edits should go through it.
- **Identity UI**: scaffolded Razor Pages under `Areas/Identity`; the rest is MVC controllers and views (`Views/`, `ViewModels/`).
- **Roles**: Admin, ProjectManager, Developer, Submitter, plus demo variants. Role checks use `[Authorize(Roles = ...)]` and `RoleService`.

## Gotchas

- `TheBugTracker.csproj` excludes several controllers and views from compilation (`ProjectPriorities`, `TicketAttachments`, `TicketComments`, `TicketHistories`, `TicketPriorities`, `TicketStatus`, `TicketTypes`). The files exist on disk but are dead code. To revive one, remove its `Compile`/`Content` entries in the csproj.
- `docker-compose.yml` mounts `pgdata` at `/var/lib/postgresql` (the Postgres 18 image layout), not `/var/lib/postgresql/data`.
- `.ai/` holds planning specs and spikes (used by the `/feature`, `/bug`, `/chore` skills) and is untracked. It includes an unrelated `electric-linearlite` project with its own `node_modules`.
