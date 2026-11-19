# PostgreSQL Database Setup Guide

This folder contains the complete, production-grade PostgreSQL database scripts for the **Software Services Business Platform**. The schema is 100% synchronized with the Entity Framework Core entity model in `.NET 10 LTS`.

---

## 📁 Script Execution Order

Execute the SQL scripts in this exact sequential order:

| Step | Script File | Purpose | Database Context |
| :--- | :--- | :--- | :--- |
| **1** | `01_CreateDatabase.sql` | Creates database `software_services_db` | Connect to default `postgres` database |
| **2** | `02_CreateTables.sql` | Creates all 19 relational tables, PKs, FKs, and registers migration history | Connect to `software_services_db` |
| **3** | `03_SeedData.sql` | Populates default SuperAdmin, service categories, services, portfolio, blog, settings | Connect to `software_services_db` |
| **4** | `04_IndexesAndConstraints.sql` | Creates performance B-Tree indexes, unique constraints, and partial indexes | Connect to `software_services_db` |

---

## 🛠️ How to Execute in pgAdmin 4

1. **Open pgAdmin 4** and connect to your local PostgreSQL server (e.g. `PostgreSQL 15 / 16 / 17`).
2. **Execute Step 1 (Database Creation):**
   - In the left sidebar tree, right-click on the default `postgres` database.
   - Click **Query Tool**.
   - Open `01_CreateDatabase.sql` (or paste its content).
   - Press **F5** (or click the Execute ▶️ icon).
   - Right-click **Databases** in the left sidebar and select **Refresh**. You will see `software_services_db`.
3. **Connect to the New Database:**
   - In pgAdmin left sidebar, expand `software_services_db`.
   - Right-click on `software_services_db` and select **Query Tool**.
4. **Execute Step 2 (Table Creation):**
   - Open or paste `02_CreateTables.sql`.
   - Press **F5**. You will see `Query returned successfully`.
5. **Execute Step 3 (Seed Data):**
   - Open or paste `03_SeedData.sql`.
   - Press **F5**. All services, categories, portfolio, blog articles, and administrator accounts are populated.
6. **Execute Step 4 (Indexes & Constraints):**
   - Open or paste `04_IndexesAndConstraints.sql`.
   - Press **F5**. All performance and unique indexes are created.

---

## 🔐 Initial Administrator Credentials

The seed script creates the following default Super Administrator account:

- **Username:** `admin`
- **Email:** `admin@softwareservices.com`
- **Initial Password:** `Admin@123456`
- **Role:** `SuperAdmin`

> [!IMPORTANT]
> The initial password is encrypted using high-security BCrypt. Upon first login to the Admin Dashboard (`/admin/login`), navigate to **Admin Profile** to change your password.

---

## 🔄 Relationship with Entity Framework Core Migrations

- The tables and constraints generated in `02_CreateTables.sql` match the EF Core migration `InitialCreate` in `Backend/SoftwareServices.Infrastructure/Migrations/`.
- `02_CreateTables.sql` records the migration token into `__EFMigrationsHistory`:
  ```sql
  INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
  VALUES ('20261118164836_InitialCreate', '10.0.14')
  ON CONFLICT ("MigrationId") DO NOTHING;
  ```
- **This ensures that when you start the ASP.NET Core backend API, EF Core recognizes that the database is already fully initialized and will not attempt duplicate table creation or fail with schema conflict errors.**
- If you prefer to let EF Core create the database automatically instead of running pgAdmin scripts, simply configure your PostgreSQL connection string in `Backend/SoftwareServices.API/appsettings.json` and run:
  ```bash
  dotnet ef database update --project Backend/SoftwareServices.Infrastructure --startup-project Backend/SoftwareServices.API
  ```
