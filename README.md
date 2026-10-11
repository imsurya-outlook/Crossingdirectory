# Crossing Directory - MVP starter (ASP.NET Core MVC, .NET 10)

A working local advertising / Yellow Pages style portal for Crossing Republic: public discovery, advertiser
self-service, admin moderation, multi-image uploads, admin-verified businesses, user service ratings, view /
details-click analytics, and Google sign-in. UI and API live in one MVC project so it deploys to a single
Azure App Service.

## Database providers

| Environment | Provider | Configured by |
|---|---|---|
| Development | SQLite | Database:Provider = Sqlite, Data Source=advertisingportal.db |
| Production  | PostgreSQL | Database:Provider = Postgres, Npgsql connection string |

SQL Server is no longer referenced anywhere in the solution. The same EF Core model drives both providers, so
no controller or service changes between environments - only appsettings or App Service settings.

In production the app does **not** create or migrate the schema; it only verifies connectivity, because the
schema is applied from the scripts below. In development it still calls EnsureCreated.

## Google sign-in

Sign in and sign up both offer **Continue with Google**. The button renders only when credentials are present,
so a fresh checkout still works with email/password alone.

Configure:

    Authentication:Google:ClientId
    Authentication:Google:ClientSecret

Create the OAuth client in Google Cloud Console -> APIs & Services -> Credentials -> OAuth client ID -> Web
application, and register the redirect URIs:

    https://YOUR-DOMAIN/signin-google
    https://localhost:7189/signin-google

Account behaviour:

- **First Google sign-in** creates the account with no password (so the password form cannot be used for it),
  marks the email confirmed, assigns the **Advertiser** role, and signs the user in.
- **Existing email/password account** signing in with Google gets the Google login *linked* to it - but only
  when Google reports the email as verified, so an unverified Google address cannot take over an account.
- **Admin accounts** may sign in with Google too; the role is respected and the user lands in the admin area.

## PostgreSQL schema scripts

    database/postgres/001_schema.sql    all 13 tables, indexes, foreign keys, check constraints
    database/postgres/002_seed.sql      roles, Crossing Republic, all 13 categories (idempotent)
    database/postgres/003_verify.sql    read-only post-deploy verification

Apply in order against an empty database:

    psql "host=YOUR-PG-HOST port=5432 dbname=crossing_directory user=YOUR-USER sslmode=require" -f database/postgres/001_schema.sql
    psql "host=YOUR-PG-HOST port=5432 dbname=crossing_directory user=YOUR-USER sslmode=require" -f database/postgres/002_seed.sql
    psql "host=YOUR-PG-HOST port=5432 dbname=crossing_directory user=YOUR-USER sslmode=require" -f database/postgres/003_verify.sql

The DDL matches the EF Core model: Identity table and column names with the same lengths, timestamps as
timestamp without time zone (the app writes UTC), booleans as boolean, and the Status enum as text with a CHECK
constraint whose values match the C# AdvertisementStatus names.

### Creating the production admin

002_seed.sql deliberately inserts no user. Either run the app once with SeedAdmin:Email / SeedAdmin:Password
set - the startup seeder creates the admin and assigns the role - or register through the site and promote the
account using the snippet at the top of 002_seed.sql.

## Run it locally (DEV)

    cd src/AdvertisingPortal.Web
    dotnet restore
    dotnet run

Open the HTTPS URL shown in the console (default https://localhost:7189). First start creates the SQLite
database and seeds roles, the admin user, Crossing Republic, and the 13 categories.

    Admin:  admin@localads.com / Admin@12345   (change before any deployment)

## Deploying to production

1. Create the PostgreSQL database and a login that owns it.
2. Run 001_schema.sql, then 002_seed.sql.
3. In App Service configuration set:

       Database__Provider                     Postgres
       ConnectionStrings__DefaultConnection   Host=...;Database=...;Username=...;Password=...;SSL Mode=Require
       Authentication__Google__ClientId
       Authentication__Google__ClientSecret
       Storage__Provider                      AzureBlob
       Storage__ConnectionString              ...
       SeedAdmin__Email / SeedAdmin__Password (first run only, then remove)

4. Publish, then confirm /Account/Login shows the Google button.

## Optional: regenerate the scripts from EF

If the model changes later, regenerate rather than hand-editing:

    dotnet ef migrations add InitialCreate --project ../AdvertisingPortal.Infrastructure --startup-project .
    dotnet ef migrations script --idempotent --output ../../database/postgres/generated.sql

Npgsql emits PostgreSQL DDL. Review the output before applying - the hand-tuned indexes and CHECK constraints
in 001_schema.sql are already correct, so treat the generated file as a diff to compare against.

## Ordering rules

    Recent ads      verified first, then highest average rating, then most ratings, then newest
    Search results  featured first, then verified, then highest rated, then newest
    Featured band   verified first, then highest rated, then newest

## Projects

- AdvertisingPortal.Domain - entities, enums, slug helper, seed constants, provider names
- AdvertisingPortal.Application - DTOs, service interfaces, upload limits, paging
- AdvertisingPortal.Infrastructure - EF Core DbContext, identity mapping, seeding, services, storage
- AdvertisingPortal.Web - MVC controllers, areas (Admin, Advertiser), Razor views, theme
