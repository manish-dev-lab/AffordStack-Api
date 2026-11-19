# Local Setup & Installation Guide
**Software Services Business Platform**

Follow this step-by-step guide to run the platform locally on Windows, macOS, or Linux.

---

## 1. Prerequisites

Ensure you have the following installed on your machine:
- **.NET 10 SDK** (or .NET 8/9 LTS compatible): Verify with `dotnet --version`
- **Node.js** (v18.x, v20.x, or v22.x) and **npm**: Verify with `node -v` and `npm -v`
- **PostgreSQL 15+** and **pgAdmin 4** (or `psql` CLI)
- A modern code editor (VS Code, Visual Studio 2022/2025, or JetBrains Rider)

---

## 2. Database Initialization (PostgreSQL)

You can initialize the database using either **Option A (pgAdmin 4)** or **Option B (psql command line)**.

### Option A: Using pgAdmin 4
1. Open **pgAdmin 4** and connect to your local PostgreSQL server.
2. In the Object Explorer, right-click **Databases** &rarr; **Query Tool**.
3. Open and execute `Database/01_CreateDatabase.sql` to create `software_services_db`.
4. Disconnect and connect to the newly created `software_services_db` database.
5. Open a Query Tool for `software_services_db` and execute the remaining scripts **in sequential order**:
   - `Database/02_CreateTables.sql` (Creates all 19 tables, PKs, FKs, EF migration history)
   - `Database/03_SeedData.sql` (Inserts admin user, services, portfolio, blog categories)
   - `Database/04_IndexesAndConstraints.sql` (Applies performance, partial, and unique indexes)

### Option B: Using psql Command Line
```powershell
# 1. Connect as postgres user and create database
psql -U postgres -f Database/01_CreateDatabase.sql

# 2. Execute table creation, seeds and indexes on the new database
psql -U postgres -d software_services_db -f Database/02_CreateTables.sql
psql -U postgres -d software_services_db -f Database/03_SeedData.sql
psql -U postgres -d software_services_db -f Database/04_IndexesAndConstraints.sql
```

---

## 3. Backend Setup & Startup

1. Open `Backend/SoftwareServices.API/appsettings.json` and verify the PostgreSQL connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=software_services_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
}
```

2. Build and run the backend Web API:
```powershell
cd Backend
dotnet build SoftwareServices.sln
dotnet run --project SoftwareServices.API
```

3. The backend starts on:
   - **API Root**: `http://localhost:5000`
   - **Swagger UI**: `http://localhost:5000/swagger`

---

## 4. Frontend Setup & Startup

1. Open a new terminal window, navigate to the frontend directory:
```powershell
cd Frontend/software-services-ui
```

2. Install dependencies (if not already installed):
```powershell
npm install
```

3. Launch the Vite development server:
```powershell
npm run dev
```

4. The frontend runs at:
   - **Public Website**: `http://localhost:5173/`
   - **Admin Console**: `http://localhost:5173/admin/login`

> **Note**: Vite is preconfigured with a development proxy (`/api` &rarr; `http://localhost:5000`). All API requests route seamlessly without CORS friction.

---

## 5. Default Administrator Credentials

Use these credentials to log in to the Admin Dashboard:
- **Login URL**: `http://localhost:5173/admin/login`
- **Username / Email**: `admin` or `admin@softwareservices.com`
- **Default Password**: `Admin@123456`

> **Changing Credentials**: Once logged in, navigate to **Admin Profile** (`/admin/profile`) to update your password, or update `AdminSettings` in the console.

---

## 6. Running Automated Tests

To run the unit test suite:
```powershell
dotnet test Backend/SoftwareServices.sln
```
All unit tests in `SoftwareServices.Tests` validate enquiry processing, reference number generation, quotation calculations, and tax math.
