# Software Services Platform - Backend API

A clean-architecture, production-grade Web API built with **ASP.NET Core (.NET 10 LTS)** and **PostgreSQL**.

---

## 🏗️ Architecture & Features

- **Clean Architecture & DDD**:
  - `SoftwareServices.Domain`: 20 Relational entities, enums, soft delete base entity.
  - `SoftwareServices.Application`: Use cases, DTOs, FluentValidation, domain services.
  - `SoftwareServices.Infrastructure`: EF Core DbContext, Npgsql PostgreSQL provider, QuestPDF commercial generator, MailKit SMTP client with Transactional Outbox.
  - `SoftwareServices.API`: RESTful controllers, JWT Bearer authentication, centralized ExceptionHandlingMiddleware, Swagger / OpenAPI.
  - `SoftwareServices.Tests`: xUnit, Moq, FluentAssertions test suite.
- **Database Scripts**:
  - `Database/01_CreateDatabase.sql`: Database initialization.
  - `Database/02_CreateTables.sql`: 19 tables, PKs, FKs, EF migration history sync.
  - `Database/03_SeedData.sql`: SuperAdmin, initial service catalog, portfolio showcases, blog categories, testimonials, website settings.
  - `Database/04_IndexesAndConstraints.sql`: Unique, composite, and partial filter indexes.
  - `Database/README.md`: Complete pgAdmin execution guide.

---

## ⚡ Quick Start

### 1. Database Setup
Execute the scripts in `Database/` via **pgAdmin 4** or `psql` in order (`01` -> `02` -> `03` -> `04`).

### 2. Configure Connection String
Open `SoftwareServices.API/appsettings.json` and set your PostgreSQL password:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=software_services_db;Username=postgres;Password=YOUR_PASSWORD"
}
```

### 3. Run Web API
```powershell
dotnet run --project SoftwareServices.API
```
- API & Swagger UI: `http://localhost:5000/swagger`

### 4. Default Admin Credentials
- **Username / Email**: `admin` or `admin@softwareservices.com`
- **Password**: `Admin@123456`
