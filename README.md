# EmployeeSkillPoc

Recreate the solution on a fresh machine with .NET 8, SQL Server, and the same package set.

## Prerequisites
- .NET SDK 8.x
- SQL Server (local instance) with permissions to create databases
- Git

## Clone and restore
```bash
git clone <repo-url> EmployeeSkillPoc
cd EmployeeSkillPoc
dotnet restore
```

## Solution layout
- `EmployeeSkillPoc.sln`
- Services: `ApiGateway`, `AuthService`, `EmployeeService`, `SkillService`, `SearchService`
- Shared library: `SharedModels`

## Packages (already referenced)
- Common: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `Serilog.AspNetCore`, `RabbitMQ.Client`, `FluentValidation`, `Swashbuckle.AspNetCore`, `System.IdentityModel.Tokens.Jwt`
- Gateway: `YARP.ReverseProxy`
- Auth: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Tools`, `FluentValidation.AspNetCore`

## AuthService configuration
1) Set connection string in `AuthService/appsettings.json`:
```json
"ConnectionStrings": {
  "AuthDatabase": "Server=YOUR_SQL_SERVER;Database=AuthServiceDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
}
```
Adjust for your SQL auth mode (use `User ID`/`Password` if not using Windows auth).

2) Set JWT settings (replace with a strong secret):
```json
"Jwt": {
  "Key": "CHANGE_ME_TO_A_LONG_RANDOM_SECRET",
  "Issuer": "AuthService",
  "Audience": "EmployeeSkillPoc",
  "ExpiresMinutes": 60
}
```

## Database setup (AuthService)
From the repo root:
```bash
dotnet ef database update --project AuthService --startup-project AuthService
```
This creates `AuthServiceDb` and applies `InitialCreate` (creates `Users` table with unique email).

## Run the AuthService
```bash
dotnet run --project AuthService
```
Default URLs: `https://localhost:5001` and `http://localhost:5000`.

## Test with Postman
- Register: `POST /auth/register`
```json
{
  "name": "Alice Admin",
  "email": "alice@example.com",
  "password": "Passw0rd!",
  "role": "Admin"
}
```
- Login: `POST /auth/login`
```json
{
  "email": "alice@example.com",
  "password": "Passw0rd!"
}
```
- Use returned JWT as `Authorization: Bearer <token>` on protected endpoints.

## Notes for other services
- Each microservice will use its own database; add connection strings and run migrations per service when their models are added.
- All services should trust the same JWT signing key/issuer/audience to validate tokens produced by `AuthService`.

## Time log (update as development progresses)
- 2025-12-09: Setup solution, AuthService models, JWT auth, migration applied.
- 2025-12-09: Added rebuild-from-scratch instructions and Notes.md companion guide.

