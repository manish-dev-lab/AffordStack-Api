using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;
using SoftwareServices.Domain.Enums;
using System.Text.Json;

namespace SoftwareServices.Infrastructure.Services;

public class DatabaseSeeder
{
    public static async Task SeedAsync(IApplicationDbContext context, ILogger logger)
    {
        try
        {
            // 0. Ensure critical tables exist (idempotent fallback if migrations have not yet run)
            if (context is DbContext efDbContext)
            {
                try
                {
                    const string schemaSql = @"
CREATE TABLE IF NOT EXISTS app_configurations (
    ""Id"" SERIAL PRIMARY KEY,
    ""ConfigKey"" VARCHAR(100) NOT NULL,
    ""ConfigValue"" TEXT NOT NULL,
    ""Description"" TEXT,
    ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS users (
    ""Id"" SERIAL PRIMARY KEY,
    ""FullName"" VARCHAR(150) NOT NULL,
    ""Email"" VARCHAR(150) NOT NULL,
    ""PhoneNumber"" VARCHAR(50) NOT NULL,
    ""CompanyName"" VARCHAR(150),
    ""PasswordHash"" VARCHAR(255) NOT NULL,
    ""IsEmailVerified"" BOOLEAN NOT NULL DEFAULT FALSE,
    ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
    ""LastLoginAt"" TIMESTAMP WITHOUT TIME ZONE,
    ""CustomerId"" INTEGER,
    ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
);

DO $$ 
BEGIN 
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='enquiries' AND column_name='UserId') THEN
        ALTER TABLE enquiries ADD COLUMN ""UserId"" INTEGER;
    END IF;
END $$;
";
                    await efDbContext.Database.ExecuteSqlRawAsync(schemaSql);
                }
                catch (Exception ddlEx)
                {
                    logger.LogWarning("Notice during idempotent DDL schema check: {Message}", ddlEx.Message);
                }
            }

            // 1. Seed or Verify Default Admin User
            var defaultAdmin = await context.AdminUsers.FirstOrDefaultAsync(u => u.Username.ToLower() == "admin");
            if (defaultAdmin == null)
            {
                logger.LogInformation("Seeding default Administrator...");
                defaultAdmin = new AdminUser
                {
                    Username = "admin",
                    Email = "admin@affordstack.com",
                    FullName = "System Administrator",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                    Role = AdminRole.SuperAdmin,
                    IsActive = true
                };
                context.AdminUsers.Add(defaultAdmin);
                await context.SaveChangesAsync();
            }
            else if (!defaultAdmin.IsActive)
            {
                defaultAdmin.IsActive = true;
                await context.SaveChangesAsync();
            }

            // 2. Seed Service Categories & Services
            if (!await context.ServiceCategories.AnyAsync())
            {
                logger.LogInformation("Seeding Service Categories & Services...");

                var catStudent = new ServiceCategory
                {
                    Name = "Student Project Guidance",
                    Slug = "student-project-guidance",
                    Description = "Comprehensive academic project mentoring for MCA, BCA, B.Tech and M.Tech students. Complete guidance from architecture and database design to execution, code walkthrough and viva defense.",
                    IconName = "GraduationCap",
                    DisplayOrder = 1,
                    IsActive = true
                };

                var catSupport = new ServiceCategory
                {
                    Name = "Developer Technical Support",
                    Slug = "developer-technical-support",
                    Description = "On-demand expert debugging, troubleshooting, architecture review, and performance tuning for professional software developers and engineering teams.",
                    IconName = "Wrench",
                    DisplayOrder = 2,
                    IsActive = true
                };

                var catEnterprise = new ServiceCategory
                {
                    Name = "Business & Enterprise Software Development",
                    Slug = "business-software-development",
                    Description = "End-to-end bespoke web, ERP, CRM, and SaaS software engineering built with modern cloud architectures, robust security, and scalable databases.",
                    IconName = "Building2",
                    DisplayOrder = 3,
                    IsActive = true
                };

                context.ServiceCategories.AddRange(catStudent, catSupport, catEnterprise);
                await context.SaveChangesAsync();

                // Services for Student Project Guidance
                var studentServices = new List<ServiceItem>
                {
                    new()
                    {
                        ServiceCategoryId = catStudent.Id,
                        Title = ".NET & ASP.NET Core Project Guidance",
                        Slug = "dotnet-student-projects",
                        ShortDescription = "Major and minor project mentoring in modern C#, ASP.NET Core Web API, Entity Framework, and SQL.",
                        FullDescription = "We provide end-to-end guidance for university academic projects using the modern .NET ecosystem. Students learn clean architecture, REST API design, database modeling, and frontend integration while building real-world applications.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Clean Architecture & Repository Pattern", "Entity Framework Core & PostgreSQL/SQL Server", "JWT Authentication & Role Management", "RESTful API Implementation", "Comprehensive Documentation & Viva Prep" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { ".NET 10", "C#", "ASP.NET Core", "EF Core", "PostgreSQL", "SQL Server" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "1-on-1 code walkthrough", "Complete understanding of technical concepts", "High grades in project evaluation", "Confidence in answering external viva questions" }),
                        IconName = "Code2",
                        DisplayOrder = 1,
                        IsFeatured = true
                    },
                    new()
                    {
                        ServiceCategoryId = catStudent.Id,
                        Title = "Java & Spring Boot Full-Stack Projects",
                        Slug = "java-spring-boot-student-projects",
                        ShortDescription = "Production-grade project mentoring covering Spring Boot, Hibernate, REST APIs, and React/Angular.",
                        FullDescription = "Master enterprise Java software engineering through your final-year college project. We mentor students on Spring Boot microservices, Spring Security, JPA/Hibernate, and responsive UI integration.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Spring Boot 3.x with Spring Security", "REST API Contract Design with Swagger", "PostgreSQL / MySQL Schema Architecture", "React or Angular Frontend Integration", "Docker Containerization Setup" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "Java 21", "Spring Boot", "Hibernate/JPA", "PostgreSQL", "React", "Docker" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Interview-ready architectural knowledge", "Industry-standard coding conventions", "Complete documentation & synopsis assistance" }),
                        IconName = "Coffee",
                        DisplayOrder = 2,
                        IsFeatured = true
                    },
                    new()
                    {
                        ServiceCategoryId = catStudent.Id,
                        Title = "React & Full-Stack Modern Web Projects",
                        Slug = "react-fullstack-student-projects",
                        ShortDescription = "Modern full-stack web applications mentoring combining React, TypeScript, Node.js/C#, and modern databases.",
                        FullDescription = "Learn how modern tech companies build applications. Build single page applications using React, TypeScript, Tailwind CSS, coupled with robust backend APIs and live deployment.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "React 19 & TypeScript Development", "State Management & React Hook Form", "Responsive Tailwind CSS UI", "JWT Authentication Flow", "Cloud Deployment on Vercel / Render" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "React", "TypeScript", "Tailwind CSS", "Vite", "Node.js / .NET", "PostgreSQL" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Portfolio-worthy live project URL", "Modern frontend design principles", "Complete viva explanation" }),
                        IconName = "Layout",
                        DisplayOrder = 3,
                        IsFeatured = true
                    },
                    new()
                    {
                        ServiceCategoryId = catStudent.Id,
                        Title = "Python & Django Web / Analytics Projects",
                        Slug = "python-django-student-projects",
                        ShortDescription = "Major college projects using Python, Django, REST Framework, and data analytics dashboards.",
                        FullDescription = "Ideal for B.Tech and MCA students building algorithmic, analytics, or workflow-heavy applications using Django ORM, authentication, and REST APIs.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Django MVC Architecture", "Django REST Framework APIs", "PostgreSQL / SQLite Integration", "Interactive Analytics & Charts", "Comprehensive Project Synopsis & Report" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "Python", "Django", "DRF", "PostgreSQL", "Pandas", "Chart.js" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Clear understanding of Python backend concepts", "Step-by-step code explanation", "Full documentation support" }),
                        IconName = "Terminal",
                        DisplayOrder = 4,
                        IsFeatured = false
                    },
                    new()
                    {
                        ServiceCategoryId = catStudent.Id,
                        Title = "Project Demonstration & Viva Preparation",
                        Slug = "project-viva-demonstration-prep",
                        ShortDescription = "Rigorous mock viva sessions, source code explanation, architecture diagrams, and slide deck guidance.",
                        FullDescription = "Having a running project is only half the battle. We prepare students for university external examiners through mock viva sessions, code explanation, and question prediction.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Deep dive into project source code", "System architecture & ER diagram explanation", "Top 50 expected viva questions & answers", "PowerPoint presentation coaching", "Local setup & emergency troubleshooting" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "UML", "ER Modeling", "Code Review", "Presentation Coaching" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Zero stage fear during viva", "Clear articulation of design choices", "Guaranteed technical confidence" }),
                        IconName = "Award",
                        DisplayOrder = 5,
                        IsFeatured = true
                    }
                };

                // Services for Developer Technical Support
                var supportServices = new List<ServiceItem>
                {
                    new()
                    {
                        ServiceCategoryId = catSupport.Id,
                        Title = ".NET & C# Bug Fixing and Web API Development",
                        Slug = "dotnet-csharp-bug-fixing",
                        ShortDescription = "Expert debugging, asynchronous deadlocks, EF Core query issues, and memory leak resolution.",
                        FullDescription = "Are you stuck on a stubborn bug in your production ASP.NET Core API or enterprise C# codebase? Our senior engineers isolate and resolve root causes rapidly.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Async/await concurrency and deadlock analysis", "EF Core N+1 query and change tracking fixes", "Dependency injection lifecycle issues", "Custom middleware and filters debugging", "Unit & integration test fixes" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { ".NET 8 / 9 / 10", "C#", "ASP.NET Core", "EF Core", "xUnit" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Rapid turnaround time", "Root cause explanation", "Prevention recommendations" }),
                        IconName = "Bug",
                        DisplayOrder = 1,
                        IsFeatured = true
                    },
                    new()
                    {
                        ServiceCategoryId = catSupport.Id,
                        Title = "React Frontend Troubleshooting & State Management",
                        Slug = "react-frontend-troubleshooting",
                        ShortDescription = "Troubleshooting complex React rendering issues, hooks dependencies, CORS, and API integrations.",
                        FullDescription = "Resolve unexpected re-renders, useEffect loops, broken authentication state, hydration errors, or CORS issues with senior React developer assistance.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Component re-render optimization", "Axios interceptor & JWT token refresh debugging", "React Router dynamic routing fixes", "React Hook Form & Zod schema validation", "Cross-browser UI responsiveness fixes" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "React", "TypeScript", "Redux Toolkit", "Zustand", "Axios", "Tailwind" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Clean, maintainable component hierarchy", "Zero layout shift", "Proper error boundaries" }),
                        IconName = "Layers",
                        DisplayOrder = 2,
                        IsFeatured = true
                    },
                    new()
                    {
                        ServiceCategoryId = catSupport.Id,
                        Title = "Database Optimization & Complex Queries (PostgreSQL / SQL Server)",
                        Slug = "database-optimization-tuning",
                        ShortDescription = "Execution plan analysis, index tuning, slow query rewriting, and deadlocks resolution.",
                        FullDescription = "Transform slow-loading dashboards and API timeouts into millisecond queries. We profile execution plans, construct optimal composite indexes, and optimize schemas.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "EXPLAIN ANALYZE query profiling", "B-tree, GIN & partial index strategy", "Connection pooling and transaction tuning", "Partitioning & normalization guidance", "Migration scripting & data integrity" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "PostgreSQL", "SQL Server", "MySQL", "pgAdmin", "DBeaver" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Up to 10x query performance improvement", "Reduced server CPU & memory usage", "Prevention of database connection exhaustion" }),
                        IconName = "Database",
                        DisplayOrder = 3,
                        IsFeatured = true
                    },
                    new()
                    {
                        ServiceCategoryId = catSupport.Id,
                        Title = "Authentication & Authorization Architecture",
                        Slug = "auth-troubleshooting-support",
                        ShortDescription = "Fixing broken JWT bearer tokens, OAuth2 flows, role permissions, and CORS configurations.",
                        FullDescription = "Secure your APIs and applications correctly. We assist in implementing and debugging JWT, refresh tokens, role-based authorization, and HTTPS compliance.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "JWT validation, signature, and expiration fixes", "Refresh token rotation and secure storage", "Role and claim-based authorization policies", "CORS policy resolution across environments", "Security header hardening" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "JWT", "OAuth 2.0", "ASP.NET Identity", "Spring Security", "BCrypt" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Enterprise-grade security", "Elimination of cryptic 401/403 errors", "Clear security audit documentation" }),
                        IconName = "ShieldCheck",
                        DisplayOrder = 4,
                        IsFeatured = false
                    }
                };

                // Services for Business & Enterprise Software Development
                var enterpriseServices = new List<ServiceItem>
                {
                    new()
                    {
                        ServiceCategoryId = catEnterprise.Id,
                        Title = "Custom Business & Enterprise Web Applications",
                        Slug = "custom-business-web-applications",
                        ShortDescription = "Tailored, secure, high-performance web systems engineered specifically for your company's workflows.",
                        FullDescription = "Off-the-shelf software often fails to fit your unique business operations. We build tailored web systems that automate operations, integrate with your tools, and scale seamlessly.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Bespoke operational workflows and dashboards", "Role-based access control & audit logging", "High-performance REST API backend", "Interactive data visualizations and reporting", "Mobile-responsive executive interfaces" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "ASP.NET Core", "React", "PostgreSQL", "Tailwind CSS", "Docker" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "100% intellectual property ownership", "Zero per-seat licensing fees", "Tailored to your exact business rules" }),
                        IconName = "Briefcase",
                        DisplayOrder = 1,
                        IsFeatured = true
                    },
                    new()
                    {
                        ServiceCategoryId = catEnterprise.Id,
                        Title = "Scalable E-Commerce & Inventory Management",
                        Slug = "ecommerce-inventory-systems",
                        ShortDescription = "Modern digital storefronts, real-time inventory tracking, multi-warehouse support, and order processing.",
                        FullDescription = "Empower your retail or wholesale business with robust catalog management, cart flows, automated invoice generation, inventory alerts, and payment integrations.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Fast product search and filtering", "Multi-tier pricing and discount rules", "Real-time stock reservation and deduction", "Automated PDF invoices & quotation generator", "Payment gateway readiness (Stripe / Razorpay)" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "React", "ASP.NET Core", "PostgreSQL", "Redis", "QuestPDF" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Never oversell stock again", "Instant automated billing & receipt emails", "Intuitive merchant admin dashboard" }),
                        IconName = "ShoppingCart",
                        DisplayOrder = 2,
                        IsFeatured = true
                    },
                    new()
                    {
                        ServiceCategoryId = catEnterprise.Id,
                        Title = "CRM & Workflow Automation Systems",
                        Slug = "crm-workflow-automation",
                        ShortDescription = "Lead pipeline tracking, customer interactions history, quotation management, and automated email follow-ups.",
                        FullDescription = "Capture incoming leads, manage customer interactions, generate quotes in minutes, and track project status from initial contact to final payment.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Lead capture and pipeline stages", "Quotation builder with PDF generation", "Email outbox with delivery tracking", "Activity timeline and customer history", "Revenue analytics and forecasting" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { ".NET 10", "React", "PostgreSQL", "MimeKit", "Recharts" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Accelerate lead-to-quote conversion", "Centralized customer communication history", "Comprehensive managerial insights" }),
                        IconName = "Users",
                        DisplayOrder = 3,
                        IsFeatured = true
                    },
                    new()
                    {
                        ServiceCategoryId = catEnterprise.Id,
                        Title = "Cloud Deployment & Continuous Maintenance",
                        Slug = "cloud-deployment-maintenance",
                        ShortDescription = "Production cloud deployment on AWS, Azure or DigitalOcean, SSL certificates, CI/CD, and monitoring.",
                        FullDescription = "We ensure your software stays fast, secure, and operational 24/7 with automated database backups, log monitoring, and ongoing feature updates.",
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Docker containerization & docker-compose", "Nginx reverse proxy & automated SSL (Certbot)", "Automated PostgreSQL backup scripts", "Centralized structured logging & monitoring", "Zero-downtime deployment pipelines" }),
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "Docker", "Linux", "Nginx", "GitHub Actions", "PostgreSQL" }),
                        BenefitsJson = JsonSerializer.Serialize(new[] { "Peace of mind with 99.9% uptime", "Automated disaster recovery", "Regular security patches" }),
                        IconName = "Cloud",
                        DisplayOrder = 4,
                        IsFeatured = false
                    }
                };

                context.Services.AddRange(studentServices);
                context.Services.AddRange(supportServices);
                context.Services.AddRange(enterpriseServices);
                await context.SaveChangesAsync();
            }

            // 3. Seed Portfolio Projects
            if (!await context.PortfolioProjects.AnyAsync())
            {
                logger.LogInformation("Seeding Portfolio Projects...");

                var portfolio = new List<PortfolioProject>
                {
                    new()
                    {
                        Title = "Enterprise ERP & Multi-Warehouse Inventory System",
                        Slug = "enterprise-erp-inventory-system",
                        ShortDescription = "A complete enterprise resource planning suite with real-time stock tracking, automated purchase orders, and PDF invoicing.",
                        DetailedDescription = "Engineered for a manufacturing client managing 4 distribution warehouses. Features role-based permissions, automated stock replenishment triggers, batch tracking, and interactive financial reporting.",
                        Category = "Business & Enterprise Software Development",
                        ThumbnailUrl = "https://images.unsplash.com/photo-1551288049-bebda4e38f71?auto=format&fit=crop&w=800&q=80",
                        LiveDemoUrl = "https://demo.softwareservices.example/erp",
                        GitHubUrl = "https://github.com/example/enterprise-erp",
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "ASP.NET Core Web API", "React 19", "PostgreSQL", "QuestPDF", "Tailwind CSS" }),
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Multi-Warehouse Stock Ledger", "Automated GST/VAT Invoicing", "Real-Time Low Stock Alerts", "Executive Analytics Dashboard" }),
                        DisplayOrder = 1,
                        IsFeatured = true,
                        IsActive = true
                    },
                    new()
                    {
                        Title = "Healthcare Telemedicine & Appointment Booking Platform",
                        Slug = "telemedicine-appointment-booking",
                        ShortDescription = "Doctor consultation scheduling, prescription management, and secure patient portal.",
                        DetailedDescription = "Built for medical clinics to manage patient records, digital consultation queues, online payment processing, and SMS/Email appointment reminders.",
                        Category = "Business & Enterprise Software Development",
                        ThumbnailUrl = "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?auto=format&fit=crop&w=800&q=80",
                        LiveDemoUrl = "https://demo.softwareservices.example/health",
                        GitHubUrl = "https://github.com/example/health-portal",
                        TechnologiesJson = JsonSerializer.Serialize(new[] { ".NET 10", "PostgreSQL", "React", "Tailwind CSS", "JWT Auth" }),
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Doctor Slot Availability Engine", "Secure Medical History Storage", "Automated Patient Notifications", "Integrated Billing & Receipts" }),
                        DisplayOrder = 2,
                        IsFeatured = true,
                        IsActive = true
                    },
                    new()
                    {
                        Title = "College Campus Placement & Student Performance Tracker (M.Tech Final Year)",
                        Slug = "campus-placement-student-tracker",
                        ShortDescription = "Comprehensive university project mentoring: automated resume parsing, eligibility filtering, and company interview tracking.",
                        DetailedDescription = "Mentored M.Tech computer science students through the full lifecycle of designing, architecting, and defending this placement management system. Implemented clean architecture and PostgreSQL database optimization.",
                        Category = "Student Project Guidance",
                        ThumbnailUrl = "https://images.unsplash.com/photo-1523240795612-9a054b0db644?auto=format&fit=crop&w=800&q=80",
                        LiveDemoUrl = "https://demo.softwareservices.example/placement",
                        GitHubUrl = "https://github.com/example/student-placement-tracker",
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "ASP.NET Core Web API", "React", "PostgreSQL", "Entity Framework Core" }),
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Automated Criteria Filtering", "Student Academic Transcript Verification", "Interview Schedule Management", "Complete Project Report & IEEE Documentation" }),
                        DisplayOrder = 3,
                        IsFeatured = true,
                        IsActive = true
                    },
                    new()
                    {
                        Title = "High-Throughput E-Commerce API & Microservices Debugging",
                        Slug = "ecommerce-api-performance-debugging",
                        ShortDescription = "Technical consulting project resolving database deadlocks and slow latency under high concurrent traffic.",
                        DetailedDescription = "Diagnosed severe API slowdowns during flash sales for an online apparel brand. Refactored EF Core queries, introduced composite PostgreSQL indexes, and optimized connection pooling, cutting latency by 78%.",
                        Category = "Developer Technical Support",
                        ThumbnailUrl = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?auto=format&fit=crop&w=800&q=80",
                        LiveDemoUrl = "https://demo.softwareservices.example/ecommerce",
                        GitHubUrl = "https://github.com/example/ecommerce-api-optimization",
                        TechnologiesJson = JsonSerializer.Serialize(new[] { "PostgreSQL Query Tuning", "ASP.NET Core", "Redis Caching", "Docker" }),
                        FeaturesJson = JsonSerializer.Serialize(new[] { "Optimized Composite B-Tree Indexes", "Resolved EF Core N+1 Query Traps", "Transaction Isolation Level Tuning", "Performance Stress Testing Benchmark" }),
                        DisplayOrder = 4,
                        IsFeatured = true,
                        IsActive = true
                    }
                };

                context.PortfolioProjects.AddRange(portfolio);
                await context.SaveChangesAsync();
            }

            // 4. Seed Blog Categories & Posts
            if (!await context.BlogCategories.AnyAsync())
            {
                logger.LogInformation("Seeding Blog Categories & Posts...");

                var catNet = new BlogCategory { Name = ".NET & Architecture", Slug = "dotnet-architecture", Description = "Deep dives into .NET, C#, and enterprise design patterns." };
                var catReact = new BlogCategory { Name = "React & Frontend", Slug = "react-frontend", Description = "Modern React patterns, performance tuning, and state management." };
                var catDb = new BlogCategory { Name = "Database Engineering", Slug = "database-engineering", Description = "PostgreSQL indexing, query optimization, and schema design." };

                context.BlogCategories.AddRange(catNet, catReact, catDb);
                await context.SaveChangesAsync();

                var blogPosts = new List<BlogPost>
                {
                    new()
                    {
                        Title = "Architecting Clean, Maintainable ASP.NET Core Web APIs in .NET 10",
                        Slug = "architecting-clean-aspnet-core-web-api-dotnet-10",
                        Summary = "Discover how separating Domain, Application, Infrastructure, and API layers eliminates spaghetti code and empowers testability.",
                        Content = @"Building robust web APIs requires more than just scaffolding controllers. In this guide, we break down Clean Architecture principles for modern ASP.NET Core applications.

### 1. The Core Domain
The domain layer holds your business entities, enums, and domain logic. It has zero external dependencies on databases or UI frameworks.

### 2. Application Layer (Use Cases)
Here live your DTOs, service interfaces, FluentValidation rules, and business workflows. The application layer depends solely on the domain.

### 3. Infrastructure Layer
PostgreSQL DbContext, MailKit email integrations, and PDF generators belong here. If you ever switch from PostgreSQL to SQL Server or change email providers, only the infrastructure layer changes.

### 4. API Layer
Controllers remain ultra-lean, acting strictly as HTTP translators that validate models, call application services, and return standard JSON responses.",
                        BlogCategoryId = catNet.Id,
                        AuthorName = "Senior Architect",
                        ThumbnailUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?auto=format&fit=crop&w=800&q=80",
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-10),
                        ViewCount = 142,
                        TagsJson = JsonSerializer.Serialize(new[] { ".NET 10", "Clean Architecture", "C#", "Web API" })
                    },
                    new()
                    {
                        Title = "PostgreSQL Indexing Strategies: Eliminating Slow Queries in Production",
                        Slug = "postgresql-indexing-strategies-eliminating-slow-queries",
                        Summary = "Learn how to use EXPLAIN ANALYZE, composite indexes, and partial indexing to speed up queries by over 90%.",
                        Content = @"Database queries are the number one bottleneck in modern web applications. Here are practical indexing strategies for PostgreSQL:

### Understand Sequential Scans
When a query lacks an index, PostgreSQL must scan every single table row. For tables with 100,000+ rows, this triggers severe disk I/O.

### Composite Indexes and Column Order
Always place high-cardinality equality columns first in composite indexes, followed by range or sort columns:
`CREATE INDEX idx_enquiries_status_createdat ON enquiries (status, created_at DESC);`

### Partial Indexes
If you frequently query only active or pending records, save disk space and RAM using a partial index:
`CREATE INDEX idx_active_quotes ON quotations (status) WHERE is_deleted = FALSE;`",
                        BlogCategoryId = catDb.Id,
                        AuthorName = "Database Specialist",
                        ThumbnailUrl = "https://images.unsplash.com/photo-1544383835-bda2bc66a55d?auto=format&fit=crop&w=800&q=80",
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-5),
                        ViewCount = 210,
                        TagsJson = JsonSerializer.Serialize(new[] { "PostgreSQL", "Database Optimization", "SQL", "Performance" })
                    },
                    new()
                    {
                        Title = "How to Ace Your University Final Year Project Viva: A Mentor's Guide",
                        Slug = "how-to-ace-university-final-year-project-viva",
                        Summary = "Top strategies for B.Tech, MCA, and M.Tech students to confidently explain code architecture, ER diagrams, and overcome examiner scrutiny.",
                        Content = @"Most students build functional code but lose marks because they cannot defend architectural choices to external examiners. Here is how to prepare:

### 1. Master Your ER Diagram
Examiners almost always ask: Why is this a 1-to-many relationship instead of many-to-many? Be ready to explain foreign keys, cascade rules, and normalization.

### 2. Walk Through the Request Pipeline
Know exactly what happens from when a user clicks 'Submit' in React: the Axios call, JWT token attachment, backend middleware, validation, EF Core transaction, and database commit.

### 3. Practice Answering 'What If' Questions
What if traffic spikes 100x? What if the database crashes during a payment? Answering with connection pooling, transaction isolation, and async processing leaves examiners thoroughly impressed.",
                        BlogCategoryId = catNet.Id,
                        AuthorName = "Academic Project Lead",
                        ThumbnailUrl = "https://images.unsplash.com/photo-1434030216411-0b793f4b4173?auto=format&fit=crop&w=800&q=80",
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-2),
                        ViewCount = 385,
                        TagsJson = JsonSerializer.Serialize(new[] { "Student Mentoring", "Project Viva", "MCA", "B.Tech" })
                    }
                };

                context.BlogPosts.AddRange(blogPosts);
                await context.SaveChangesAsync();
            }

            // 5. Seed Testimonials
            if (!await context.Testimonials.AnyAsync())
            {
                logger.LogInformation("Seeding Testimonials...");

                var testimonials = new List<Testimonial>
                {
                    new()
                    {
                        ClientName = "Rahul Sharma",
                        ClientRole = "M.Tech Computer Science Student",
                        CompanyOrInstitution = "NIT Rourkela",
                        Feedback = "The guidance on my Spring Boot and React project was outstanding. Not only did they help me structure the clean architecture, but the 1-on-1 viva coaching helped me achieve an 'A' grade with special appreciation from the external examiner!",
                        Rating = 5,
                        AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=200&q=80",
                        ProjectCategory = "Student Project Guidance",
                        IsFeatured = true,
                        IsActive = true,
                        DisplayOrder = 1
                    },
                    new()
                    {
                        ClientName = "Vikram Patel",
                        ClientRole = "Lead Backend Engineer",
                        CompanyOrInstitution = "Fintech Solutions Ltd",
                        Feedback = "We had a critical PostgreSQL locking and concurrency bug right before our major launch. Their developer support team pinpointed the uncommitted transaction issue within 2 hours. Exceptional technical depth and professionalism.",
                        Rating = 5,
                        AvatarUrl = "https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?auto=format&fit=crop&w=200&q=80",
                        ProjectCategory = "Developer Technical Support",
                        IsFeatured = true,
                        IsActive = true,
                        DisplayOrder = 2
                    },
                    new()
                    {
                        ClientName = "Priya Nambiar",
                        ClientRole = "Operations Director",
                        CompanyOrInstitution = "Apex Logistics Hub",
                        Feedback = "They developed our customized warehouse and billing system from scratch. The quotation was clear, deliverables were met ahead of schedule, and the platform has transformed our daily dispatch operations.",
                        Rating = 5,
                        AvatarUrl = "https://images.unsplash.com/photo-1580489944761-15a19d654956?auto=format&fit=crop&w=200&q=80",
                        ProjectCategory = "Business & Enterprise Software Development",
                        IsFeatured = true,
                        IsActive = true,
                        DisplayOrder = 3
                    }
                };

                context.Testimonials.AddRange(testimonials);
                await context.SaveChangesAsync();
            }

            // 6. Seed Website Settings
            if (!await context.WebsiteSettings.AnyAsync())
            {
                logger.LogInformation("Seeding Website Settings...");

                var settings = new List<WebsiteSetting>
                {
                    new() { SettingKey = "Company:Name", SettingValue = "AffordStack", SettingGroup = "General", Description = "Official trading company name" },
                    new() { SettingKey = "Company:Email", SettingValue = "manishkiran8464@gmail.com", SettingGroup = "Contact", Description = "Primary inquiry and support email" },
                    new() { SettingKey = "Company:Phone", SettingValue = "+91 8464992957", SettingGroup = "Contact", Description = "Primary phone number" },
                    new() { SettingKey = "Company:WhatsApp", SettingValue = "+91 8464992957", SettingGroup = "Contact", Description = "WhatsApp support number" },
                    new() { SettingKey = "Company:Address", SettingValue = "Rc puram Hyd", SettingGroup = "Contact", Description = "Physical office or technical lab address" },
                    new() { SettingKey = "Company:WorkingHours", SettingValue = "Mon - Sat: 9:00 AM - 8:00 PM IST", SettingGroup = "General", Description = "Working hours" },
                    new() { SettingKey = "Company:Website", SettingValue = "https://example.com", SettingGroup = "General", Description = "Company website URL" },
                    new() { SettingKey = "Finance:Currency", SettingValue = "INR (₹)", SettingGroup = "Finance", Description = "Primary currency display" },
                    new() { SettingKey = "Finance:DefaultTaxPercent", SettingValue = "18", SettingGroup = "Finance", Description = "Default GST / Tax percentage" },
                    new() { SettingKey = "Quotation:DefaultValidityDays", SettingValue = "10", SettingGroup = "Quotation", Description = "Quotation validity duration in days" },
                    new() { SettingKey = "Social:LinkedIn", SettingValue = "https://linkedin.com", SettingGroup = "Social", Description = "LinkedIn profile URL" },
                    new() { SettingKey = "Social:GitHub", SettingValue = "https://github.com", SettingGroup = "Social", Description = "GitHub organization URL" }
                };

                context.WebsiteSettings.AddRange(settings);
                await context.SaveChangesAsync();
            }

            // 7. Seed App Configurations (Dynamic JSON configurations)
            try
            {
                if (!await context.AppConfigurations.AnyAsync())
                {
                    logger.LogInformation("Seeding App Configurations (JSON key-value settings)...");

                    var appConfigs = new List<AppConfiguration>
                    {
                        new()
                        {
                            ConfigKey = "TokenConfig",
                            ConfigValue = JsonSerializer.Serialize(new
                            {
                                TokenTimeoutMinutes = 60,
                                RefreshTokenDays = 7
                            }, new JsonSerializerOptions { WriteIndented = true }),
                            Description = "JWT session authentication timeout and refresh token lifespan"
                        },
                        new()
                        {
                            ConfigKey = "EmailConfig",
                            ConfigValue = JsonSerializer.Serialize(new
                            {
                                EmailsEnabled = true,
                                SendCustomerNotifications = true,
                                SendAdminNotifications = true,
                                SmtpHost = "smtp.gmail.com",
                                SmtpPort = 587,
                                Username = "softwareservices2001@gmail.com",
                                Password = "niywncoibgfffxbd",
                                FromEmail = "softwareservices2001@gmail.com",
                                FromName = "AffordStack",
                                AdminNotificationEmail = "softwareservices2001@gmail.com",
                                EnableSsl = true
                            }, new JsonSerializerOptions { WriteIndented = true }),
                            Description = "Master email notifications switch, notification controls, and SMTP credentials"
                        },
                        new()
                        {
                            ConfigKey = "WebsiteConfig",
                            ConfigValue = JsonSerializer.Serialize(new
                            {
                                SiteName = "AffordStack Platform",
                                SupportEmail = "manishkiran8464@gmail.com",
                                SupportPhone = "+91 8464992957",
                                ContactAddress = "Rc puram Hyd",
                                MaintenanceMode = false
                            }, new JsonSerializerOptions { WriteIndented = true }),
                            Description = "General platform branding, support contact channels, and maintenance mode toggle"
                        }
                    };

                    context.AppConfigurations.AddRange(appConfigs);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception cfgEx)
            {
                logger.LogWarning("Notice during AppConfigurations seeding: {Message}", cfgEx.Message);
            }

            // 8. Seed Rich Sample Business Data (25 Enquiries, 25 Customers, Quotations, Earnings & Payments)
            await SeedSampleBusinessDataAsync(context, logger);

            logger.LogInformation("Database seed verification completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database seeding.");
        }
    }

    public static async Task SeedSampleBusinessDataAsync(IApplicationDbContext context, ILogger logger)
    {
        try
        {
            // If customers or enquiries already exist, preserve live database and do not touch business data
            if (await context.Customers.AnyAsync() || await context.Enquiries.AnyAsync())
            {
                logger.LogInformation("Database already contains customer and enquiry data. Skipping sample business data seeding.");
                return;
            }

            logger.LogInformation("Seeding fresh sample business dataset (25 customers, 25 enquiries, quotations, projects, earnings)...");

            // Retrieve service categories
            var categories = await context.ServiceCategories.ToListAsync();
            var catStudent = categories.FirstOrDefault(c => c.Slug == "student-project-guidance") ?? categories[0];
            var catSupport = categories.FirstOrDefault(c => c.Slug == "developer-technical-support") ?? categories[1];
            var catEnterprise = categories.FirstOrDefault(c => c.Slug == "business-software-development") ?? categories[2];

            // 1. Create 25 Customers
            var customerSeeds = new (string Name, string Email, string Phone, string Company)[]
            {
                ("Rahul Sharma", "rahul.sharma@technovalabs.com", "+91 98450 12345", "TechNova Labs"),
                ("Priya Patel", "priya.patel@innovatesoft.com", "+91 98230 23456", "InnovateSoft Solutions"),
                ("Amit Verma", "amit.verma@zenithcloud.com", "+91 99120 34567", "Zenith Cloud Tech"),
                ("Sneha Reddy", "sneha.reddy@apexdigital.com", "+91 94401 45678", "Apex Digital Systems"),
                ("Vikram Singh", "vikram.singh@cloudmatrix.com", "+91 97110 56789", "CloudMatrix Enterprise"),
                ("Ananya Roy", "ananya.roy@aiminds.org", "+91 98300 67890", "AI Minds Research"),
                ("Rohit Gupta", "rohit.gupta@vanguardretail.in", "+91 98100 78901", "Vanguard Retail"),
                ("Neha Joshi", "neha.joshi@pulsehealth.com", "+91 94440 89012", "Pulse Healthcare Systems"),
                ("Karan Malhotra", "karan.malhotra@logitrack.co", "+91 99990 90123", "LogiTrack Worldwide"),
                ("Divya Nair", "divya.nair@finflow.io", "+91 98470 01234", "FinFlow Payments"),
                ("Aditya Kulkarni", "aditya.kulkarni@agripulse.in", "+91 98220 11223", "AgriPulse Tech"),
                ("Pooja Deshmukh", "pooja.deshmukh@edusphere.edu", "+91 97660 22334", "EduSphere Academy"),
                ("Suresh Ramakrishnan", "suresh.r@smartcommerce.in", "+91 98410 33445", "SmartCommerce"),
                ("Meera Sundaram", "meera.sundaram@optimalog.com", "+91 94450 44556", "Optima Logistics"),
                ("Rajesh Nambiar", "rajesh.nambiar@medicore.org", "+91 94470 55667", "MediCore Clinics"),
                ("Swati Saxena", "swati.saxena@cybersafe.tech", "+91 98110 66778", "CyberSafe Defense"),
                ("Varun Kapoor", "varun.kapoor@omnistack.com", "+91 98720 77889", "OmniStack Global"),
                ("Ritu Aggarwal", "ritu.aggarwal@smarthotel.in", "+91 98290 88990", "SmartHotel Solutions"),
                ("Manoj Tiwari", "manoj.tiwari@realestatehub.in", "+91 94150 99001", "RealEstate Hub"),
                ("Sandeep Pillai", "sandeep.pillai@fleetpro.co", "+91 98420 12340", "FleetPro Transports"),
                ("Alok Das", "alok.das@infrabuild.io", "+91 94370 23451", "InfraBuild SaaS"),
                ("Kavita Menon", "kavita.menon@legaledge.in", "+91 98450 34562", "LegalEdge Technologies"),
                ("Chetan Mehta", "chetan.mehta@solarpulse.com", "+91 98250 45673", "SolarPulse Green Energy"),
                ("Sunita Sharma", "sunita.sharma@retailwave.in", "+91 98930 56784", "RetailWave Omnichannel"),
                ("Harish Kumar", "harish.kumar@codecraft.edu", "+91 94400 67895", "CodeCraft Academy")
            };

            var customers = new List<Customer>();
            foreach (var c in customerSeeds)
            {
                customers.Add(new Customer
                {
                    FullName = c.Name,
                    Email = c.Email,
                    PhoneNumber = c.Phone,
                    CompanyName = c.Company,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(10, 90))
                });
            }

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();

            // 2. Real registered users are NOT seeded; they register through client portal.
            // 3. Create 25 Enquiries with realistic details
            var enquiryConfigs = new (string Title, string Tech, string Budget, string Timeline, int CatIndex, EnquiryStatus Status, int CustIndex)[]
            {
                ("Hospital Management System with Electronic Health Records", "ASP.NET Core, React, PostgreSQL", "₹2,50,000 - ₹3,50,000", "2 - 3 Months", 2, EnquiryStatus.Closed, 7),
                ("MCA Final Year Project: Distributed E-Commerce Microservices", "Java, Spring Boot, React, Docker", "₹25,000 - ₹35,000", "1 Month", 0, EnquiryStatus.Converted, 0),
                ("PostgreSQL Query Performance Optimization & Index Tuning", "PostgreSQL, EF Core, Linux", "₹40,000 - ₹60,000", "2 Weeks", 1, EnquiryStatus.Converted, 2),
                ("B.Tech Final Year: AI Medical Image Diagnostics in Python & React", "Python, FastAPI, TensorFlow, React", "₹25,000 - ₹40,000", "1 Month", 0, EnquiryStatus.Quoted, 5),
                ("Warehouse Inventory Tracking & Barcode Scanner Mobile Web App", "Next.js, Node.js, PostgreSQL", "₹1,80,000 - ₹2,50,000", "2 Months", 2, EnquiryStatus.Quoted, 8),
                ("High-Throughput REST API Microservice in ASP.NET Core 10", ".NET 10, Redis, Docker, RabbitMQ", "₹75,000 - ₹1,20,000", "3 Weeks", 1, EnquiryStatus.Quoted, 1),
                ("Online University Examination & Automated Proctoring System", "ASP.NET Core, WebRTC, React, SQL", "₹35,000 - ₹50,000", "1 Month", 0, EnquiryStatus.Quoted, 11),
                ("Cloud-Native Multi-Tenant CRM Platform for Real Estate Brokers", "React, .NET 10, PostgreSQL, AWS", "₹3,00,000 - ₹4,50,000", "3 Months", 2, EnquiryStatus.Quoted, 18),
                ("Docker Containerization & CI/CD Pipeline Migration for .NET", "Docker, GitHub Actions, Raspberry Pi / Ubuntu", "₹30,000 - ₹50,000", "1 Week", 1, EnquiryStatus.Reviewed, 3),
                ("M.Tech Project: Blockchain-Based Supply Chain Provenance", "Solidity, Ethereum, Web3.js, React", "₹30,000 - ₹45,000", "1 Month", 0, EnquiryStatus.Reviewed, 16),
                ("Fleet Management & GPS Live Telematics Dashboard", "React, Node.js, Socket.io, Leaflet Maps", "₹2,00,000 - ₹3,00,000", "2 Months", 2, EnquiryStatus.Reviewed, 19),
                ("EF Core N+1 Query Auditing & Memory Leak Profiling", ".NET, EF Core, BenchmarkDotNet", "₹35,000 - ₹50,000", "10 Days", 1, EnquiryStatus.Reviewed, 4),
                ("IoT Smart Agriculture Sensor Analytics Web Application", "ESP32, MQTT, Python Flask, Chart.js", "₹25,000 - ₹35,000", "3 Weeks", 0, EnquiryStatus.Reviewed, 10),
                ("Bespoke ERP Software for Manufacturing & Stock Control", ".NET 10, React, PostgreSQL", "₹4,00,000 - ₹6,00,000", "4 Months", 2, EnquiryStatus.New, 20),
                ("Legacy ASP.NET MVC 5 to ASP.NET Core 10 Migration Support", "C#, ASP.NET Core, EF Core", "₹80,000 - ₹1,50,000", "1 Month", 1, EnquiryStatus.New, 14),
                ("MCA Major Project: AI-Powered Resume Screening & Matcher", "Python NLP, spaCy, React, FastAPI", "₹20,000 - ₹30,000", "3 Weeks", 0, EnquiryStatus.New, 24),
                ("Multi-Vendor Marketplace with Vendor Dashboard & Escrow", "Next.js, Node.js, Stripe, PostgreSQL", "₹2,50,000 - ₹3,80,000", "3 Months", 2, EnquiryStatus.New, 6),
                ("OAuth2 SSO & JWT Authorization Architecture Consulting", "Keycloak, ASP.NET Core, JWT, OpenIddict", "₹45,000 - ₹70,000", "2 Weeks", 1, EnquiryStatus.New, 15),
                ("College Library Management & Digital Dissertation Repository", "React, Python, Django, PostgreSQL", "₹25,000 - ₹35,000", "1 Month", 0, EnquiryStatus.New, 21),
                ("Restaurant POS & Touchscreen Table Ordering System", "React, Tailwind, Express.js, SQLite / Postgres", "₹1,20,000 - ₹1,80,000", "6 Weeks", 2, EnquiryStatus.Converted, 17),
                ("Redis Distributed Caching Setup for High-Traffic News Portal", "Redis, ASP.NET Core, Linux", "₹35,000 - ₹55,000", "10 Days", 1, EnquiryStatus.Converted, 22),
                ("B.Tech Minor Project: Smart Attendance via Facial Recognition", "OpenCV, Python, SQLite, Tkinter / Streamlit", "₹15,000 - ₹22,000", "2 Weeks", 0, EnquiryStatus.Converted, 23),
                ("B2B Wholesale Bidding & Reverse Auction Platform", "React, .NET Core, SignalR, PostgreSQL", "₹3,20,000 - ₹4,80,000", "3 Months", 2, EnquiryStatus.Closed, 12),
                ("Stripe & Razorpay Payment Gateway Webhook Reconciliation", ".NET 10, Razorpay Webhooks, Stripe SDK", "₹30,000 - ₹45,000", "1 Week", 1, EnquiryStatus.Quoted, 9),
                ("Student Code Review, Architecture Synopsis & Viva Guidance", "C#, ASP.NET Core, React, Viva Prep", "₹12,000 - ₹18,000", "1 Week", 0, EnquiryStatus.Quoted, 13)
            };

            var enquiries = new List<Enquiry>();
            for (int i = 0; i < enquiryConfigs.Length; i++)
            {
                var cfg = enquiryConfigs[i];
                var cust = customers[cfg.CustIndex];
                var cat = cfg.CatIndex == 0 ? catStudent : (cfg.CatIndex == 1 ? catSupport : catEnterprise);
                var createdDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(5, 75));

                var refNum = $"ENQ-20261121-{(1000 + i + 1)}";
                var enq = new Enquiry
                {
                    ReferenceNumber = refNum,
                    CustomerId = cust.Id,
                    ServiceCategoryId = cat.Id,
                    ProjectTitle = cfg.Title,
                    ProjectDescription = $"Project enquiry for {cfg.Title}. Requirement involves {cfg.Tech}. Estimated client target is {cfg.Budget}.",
                    RequiredTechnology = cfg.Tech,
                    EstimatedBudget = cfg.Budget,
                    Status = cfg.Status,
                    CreatedAt = createdDate,
                    UserId = null
                };

                enquiries.Add(enq);
            }

            context.Enquiries.AddRange(enquiries);
            await context.SaveChangesAsync();

            // 4. Create 15 Quotations with realistic values & statuses
            var quoteData = new (int EnqIndex, decimal SubTotal, QuotationStatus Status)[]
            {
                (0, 280000m, QuotationStatus.Accepted), // Hospital System
                (1, 28000m, QuotationStatus.Accepted),  // MCA Project
                (2, 45000m, QuotationStatus.Accepted),  // Postgres Tuning
                (3, 32000m, QuotationStatus.Sent),      // AI Diagnostics
                (4, 195000m, QuotationStatus.Viewed),   // Warehouse Barcode
                (5, 85000m, QuotationStatus.Sent),      // High-Throughput API
                (6, 42000m, QuotationStatus.Accepted),  // Online Exam
                (7, 340000m, QuotationStatus.Sent),     // Multi-Tenant CRM
                (19, 145000m, QuotationStatus.Accepted),// Restaurant POS
                (20, 48000m, QuotationStatus.Accepted), // Redis Caching
                (21, 18000m, QuotationStatus.Viewed),   // Smart Attendance
                (22, 380000m, QuotationStatus.Accepted),// B2B Auction
                (23, 35000m, QuotationStatus.Sent),     // Payment Gateway
                (24, 15000m, QuotationStatus.Rejected), // Student Code Review
                (8, 38000m, QuotationStatus.Rejected)   // Docker CI/CD
            };

            var quotations = new List<Quotation>();
            for (int i = 0; i < quoteData.Length; i++)
            {
                var qd = quoteData[i];
                var enq = enquiries[qd.EnqIndex];
                var cust = customers.First(c => c.Id == enq.CustomerId);
                var subTotal = qd.SubTotal;
                var tax = Math.Round(subTotal * 0.18m, 2);
                var total = subTotal + tax;
                var advance = Math.Round(total * 0.4m, 2);

                var q = new Quotation
                {
                    QuotationNumber = $"QUO-202611-{(2000 + i + 1)}",
                    EnquiryId = enq.Id,
                    CustomerId = cust.Id,
                    ServiceDescription = enq.ProjectTitle,
                    Deliverables = "Architectural blueprint, implementation source code, test suites, and containerized deployment handover.",
                    DevelopmentCost = subTotal,
                    TaxRate = 18m,
                    TaxAmount = tax,
                    TotalAmount = total,
                    AdvancePaymentAmount = advance,
                    RemainingPaymentAmount = total - advance,
                    Status = qd.Status,
                    ExpectedDeliveryDate = DateTime.UtcNow.AddDays(Random.Shared.Next(15, 60)),
                    ValidUntil = DateTime.UtcNow.AddDays(30),
                    SecureToken = Guid.NewGuid().ToString("N"),
                    CreatedAt = enq.CreatedAt.AddDays(2),
                    SentAt = enq.CreatedAt.AddDays(3),
                    RespondedAt = qd.Status == QuotationStatus.Accepted || qd.Status == QuotationStatus.Rejected ? enq.CreatedAt.AddDays(5) : null
                };

                // Add Quotation Items
                q.Items.Add(new QuotationItem
                {
                    Title = "Core System Architecture & Implementation",
                    Description = $"Primary engineering deliverables for {enq.ProjectTitle}",
                    Quantity = 1,
                    UnitPrice = Math.Round(subTotal * 0.7m, 2),
                    TotalPrice = Math.Round(subTotal * 0.7m, 2)
                });

                q.Items.Add(new QuotationItem
                {
                    Title = "Testing, Quality Assurance & Deployment Setup",
                    Description = "Unit testing, containerized deployment, performance benchmarking & handover",
                    Quantity = 1,
                    UnitPrice = Math.Round(subTotal * 0.3m, 2),
                    TotalPrice = Math.Round(subTotal * 0.3m, 2)
                });

                // Add Status History
                q.StatusHistory.Add(new QuotationStatusHistory
                {
                    PreviousStatus = null,
                    NewStatus = QuotationStatus.Draft,
                    ChangedBy = "System",
                    Remarks = "Initial technical proposal generated"
                });

                if (q.Status != QuotationStatus.Draft)
                {
                    q.StatusHistory.Add(new QuotationStatusHistory
                    {
                        PreviousStatus = QuotationStatus.Draft,
                        NewStatus = QuotationStatus.Sent,
                        ChangedBy = "Administrator",
                        Remarks = "Proposal emailed to client"
                    });
                }

                if (q.Status == QuotationStatus.Accepted)
                {
                    q.StatusHistory.Add(new QuotationStatusHistory
                    {
                        PreviousStatus = QuotationStatus.Sent,
                        NewStatus = QuotationStatus.Accepted,
                        ChangedBy = "Customer",
                        Remarks = "Quotation formally accepted by customer"
                    });
                }
                else if (q.Status == QuotationStatus.Rejected)
                {
                    q.StatusHistory.Add(new QuotationStatusHistory
                    {
                        PreviousStatus = QuotationStatus.Sent,
                        NewStatus = QuotationStatus.Rejected,
                        ChangedBy = "Customer",
                        Remarks = "Quotation declined by client"
                    });
                }

                quotations.Add(q);
            }

            context.Quotations.AddRange(quotations);
            await context.SaveChangesAsync();

            // 5. Create 8 Projects from Accepted Quotations
            var acceptedQuotes = quotations.Where(q => q.Status == QuotationStatus.Accepted).ToList();
            var projects = new List<Project>();
            for (int i = 0; i < acceptedQuotes.Count; i++)
            {
                var q = acceptedQuotes[i];
                var enq = enquiries.First(e => e.Id == q.EnquiryId);
                var isCompleted = i % 3 == 0;
                var progress = isCompleted ? 100 : (i == 1 ? 85 : (i == 2 ? 65 : (i == 4 ? 40 : 25)));

                var prj = new Project
                {
                    ProjectNumber = $"PRJ-2026-{(3000 + i + 1)}",
                    CustomerId = q.CustomerId,
                    QuotationId = q.Id,
                    ProjectTitle = enq.ProjectTitle,
                    Description = $"Live engagement for {enq.ProjectTitle}. Scope defined per quotation {q.QuotationNumber}.",
                    AgreedAmount = q.TotalAmount,
                    StartDate = q.CreatedAt.AddDays(5),
                    ExpectedCompletionDate = q.ExpectedDeliveryDate,
                    ActualCompletionDate = isCompleted ? q.CreatedAt.AddDays(40) : null,
                    Status = isCompleted ? ProjectStatus.Completed : ProjectStatus.InProgress,
                    ProgressPercentage = progress,
                    CreatedAt = q.CreatedAt.AddDays(5)
                };

                // Add milestones
                prj.Milestones.Add(new ProjectMilestone
                {
                    Title = "Milestone 1: Architectural Blueprint & Schema Setup",
                    Description = "Initial architecture, entity models, and database schema setup",
                    Amount = Math.Round(q.TotalAmount * 0.4m, 2),
                    DueDate = prj.StartDate.AddDays(14),
                    IsCompleted = true,
                    CompletedAt = prj.StartDate.AddDays(12),
                    CreatedAt = prj.CreatedAt
                });

                prj.Milestones.Add(new ProjectMilestone
                {
                    Title = "Milestone 2: Core Feature Implementation & Testing",
                    Description = "Implementation of core application features and unit testing",
                    Amount = Math.Round(q.TotalAmount * 0.4m, 2),
                    DueDate = prj.StartDate.AddDays(30),
                    IsCompleted = isCompleted,
                    CompletedAt = isCompleted ? prj.StartDate.AddDays(28) : null,
                    CreatedAt = prj.CreatedAt
                });

                prj.Milestones.Add(new ProjectMilestone
                {
                    Title = "Milestone 3: Final Deployment & Documentation Handover",
                    Description = "Production deployment, release notes, and client knowledge transfer",
                    Amount = Math.Round(q.TotalAmount * 0.2m, 2),
                    DueDate = prj.ExpectedCompletionDate,
                    IsCompleted = isCompleted,
                    CompletedAt = isCompleted ? prj.StartDate.AddDays(40) : null,
                    CreatedAt = prj.CreatedAt
                });

                projects.Add(prj);
            }

            context.Projects.AddRange(projects);
            await context.SaveChangesAsync();

            // 6. Create 12 Payments (Generates ~₹4,85,000+ recorded revenue for dashboard charts)
            var payments = new List<Payment>();
            var paymentMethods = new[] { "Bank Transfer (NEFT)", "UPI - GPay / PhonePe", "Razorpay Payment Gateway", "Stripe Online", "IMPS" };

            int payIndex = 1;
            foreach (var prj in projects)
            {
                // Advance payment for each project
                var advanceAmount = Math.Round(prj.AgreedAmount * 0.4m, 2);
                payments.Add(new Payment
                {
                    CustomerId = prj.CustomerId,
                    ProjectId = prj.Id,
                    PaymentType = PaymentType.Advance,
                    Amount = advanceAmount,
                    PaymentMethod = paymentMethods[payIndex % paymentMethods.Length],
                    TransactionReference = $"TXN-202610-{(8000 + payIndex)}",
                    PaymentDate = prj.StartDate.AddDays(1),
                    Status = PaymentStatus.Received,
                    Notes = $"Advance Milestone 1 payment for {prj.ProjectNumber}",
                    CreatedAt = prj.StartDate.AddDays(1)
                });
                payIndex++;

                // If completed or high progress, add milestone 2 payment
                if (prj.ProgressPercentage >= 65)
                {
                    var m2Amount = Math.Round(prj.AgreedAmount * 0.35m, 2);
                    payments.Add(new Payment
                    {
                        CustomerId = prj.CustomerId,
                        ProjectId = prj.Id,
                        PaymentType = PaymentType.Milestone,
                        Amount = m2Amount,
                        PaymentMethod = paymentMethods[payIndex % paymentMethods.Length],
                        TransactionReference = $"TXN-202611-{(8000 + payIndex)}",
                        PaymentDate = prj.StartDate.AddDays(20),
                        Status = PaymentStatus.Received,
                        Notes = $"Milestone 2 development payment for {prj.ProjectNumber}",
                        CreatedAt = prj.StartDate.AddDays(20)
                    });
                    payIndex++;
                }
            }

            context.Payments.AddRange(payments);
            await context.SaveChangesAsync();

            logger.LogInformation("Successfully populated sample business dataset with {C} customers, {E} enquiries, {Q} quotations, {P} projects, and {Pay} payments.",
                customers.Count, enquiries.Count, quotations.Count, projects.Count, payments.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while seeding sample business dataset: {Message}", ex.Message);
        }
    }
}
