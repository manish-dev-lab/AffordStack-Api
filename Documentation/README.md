# Documentation Index
**Software Services Business Platform**

Welcome to the comprehensive technical documentation for the Software Services Business Platform.

---

## Document Directory

1. **[Project Architecture & System Design](file:///f:/Projects/SoftwareServicesPlatform/Documentation/ProjectArchitecture.md)**
   - High-level multi-tier architecture diagram.
   - Clean Architecture layer breakdown (`Domain`, `Application`, `Infrastructure`, `API`).
   - Transactional Outbox pattern and QuestPDF integration.
   - Authentication, Authorization, and Security principles.

2. **[Database Design & Relational Schema](file:///f:/Projects/SoftwareServicesPlatform/Documentation/DatabaseDesign.md)**
   - Complete Mermaid Entity-Relationship (ER) diagram.
   - 19 PostgreSQL relational tables catalog and data dictionaries.
   - Foreign key constraints, unique indexes, and audit logs.

3. **[REST API Documentation](file:///f:/Projects/SoftwareServicesPlatform/Documentation/API_Documentation.md)**
   - Complete catalog of public and administrative REST endpoints.
   - Request and response schemas with JSON examples.
   - JWT Bearer authentication headers.
   - Swagger / OpenAPI endpoint information.

4. **[Local Setup & Installation Guide](file:///f:/Projects/SoftwareServicesPlatform/Documentation/SetupInstructions.md)**
   - Prerequisites (.NET 10, Node.js, PostgreSQL).
   - Step-by-step pgAdmin 4 database execution instructions.
   - Backend Web API launch commands.
   - React 19 Frontend launch commands.
   - Default administrator credentials (`admin` / `Admin@123456`).

5. **[Production Deployment & Hosting Guide](file:///f:/Projects/SoftwareServicesPlatform/Documentation/DeploymentGuide.md)**
   - Dockerfile and Docker Compose orchestration.
   - Nginx reverse proxy configuration with SSL termination.
   - Production security checklist.
