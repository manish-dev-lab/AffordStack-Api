-- ============================================================================
-- SCRIPT 03: INITIAL SEED DATA
-- Platform: PostgreSQL 14+ (Compatible with pgAdmin 4)
-- Project: Software Services Business Platform
-- Note: Does NOT insert fake customers or enquiries.
-- ============================================================================

START TRANSACTION;

-- 1. Default SuperAdmin (admin@softwareservices.com / Admin@123456)
-- Password hash generated via BCrypt standard cost factor
INSERT INTO admin_users ("Id", "Username", "Email", "PasswordHash", "FullName", "Role", "IsActive", "CreatedAt")
VALUES (
    1,
    'admin',
    'admin@softwareservices.com',
    '$2a$11$yzDtbk80BS8Py5x8k1SHtedDXcfUikkZPKgzUKGuvxzZCFHvyFdaG', -- Verified BCrypt hash of Admin@123456
    'System Administrator',
    1, -- SuperAdmin
    TRUE,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Id") DO NOTHING;

SELECT setval(pg_get_serial_sequence('admin_users', 'Id'), COALESCE(MAX("Id"), 1)) FROM admin_users;

-- 2. Service Categories
INSERT INTO service_categories ("Id", "Name", "Slug", "Description", "IconName", "DisplayOrder", "IsActive", "CreatedAt")
VALUES 
(
    1,
    'Student Project Guidance',
    'student-project-guidance',
    'Comprehensive academic project mentoring for MCA, BCA, B.Tech and M.Tech students. Complete guidance from architecture and database design to execution, code walkthrough and viva defense.',
    'GraduationCap',
    1,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    2,
    'Developer Technical Support',
    'developer-technical-support',
    'On-demand expert debugging, troubleshooting, architecture review, and performance tuning for professional software developers and engineering teams.',
    'Wrench',
    2,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    3,
    'Business & Enterprise Software Development',
    'business-software-development',
    'End-to-end bespoke web, ERP, CRM, and SaaS software engineering built with modern cloud architectures, robust security, and scalable databases.',
    'Building2',
    3,
    TRUE,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Id") DO NOTHING;

-- Reset sequence for service_categories
SELECT setval(pg_get_serial_sequence('service_categories', 'Id'), COALESCE(MAX("Id"), 1)) FROM service_categories;

-- 3. Services
INSERT INTO services ("Id", "ServiceCategoryId", "Title", "Slug", "ShortDescription", "FullDescription", "FeaturesJson", "TechnologiesJson", "BenefitsJson", "IconName", "DisplayOrder", "IsActive", "IsFeatured", "CreatedAt")
VALUES
(
    1,
    1,
    '.NET & ASP.NET Core Project Guidance',
    'dotnet-student-projects',
    'Major and minor project mentoring in modern C#, ASP.NET Core Web API, Entity Framework, and SQL.',
    'We provide end-to-end guidance for university academic projects using the modern .NET ecosystem. Students learn clean architecture, REST API design, database modeling, and frontend integration while building real-world applications.',
    '["Clean Architecture & Repository Pattern", "Entity Framework Core & PostgreSQL/SQL Server", "JWT Authentication & Role Management", "RESTful API Implementation", "Comprehensive Documentation & Viva Prep"]',
    '[".NET 10", "C#", "ASP.NET Core", "EF Core", "PostgreSQL", "SQL Server"]',
    '["1-on-1 code walkthrough", "Complete understanding of technical concepts", "High grades in project evaluation", "Confidence in answering external viva questions"]',
    'Code2',
    1,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    2,
    1,
    'Java & Spring Boot Full-Stack Projects',
    'java-spring-boot-student-projects',
    'Production-grade project mentoring covering Spring Boot, Hibernate, REST APIs, and React/Angular.',
    'Master enterprise Java software engineering through your final-year college project. We mentor students on Spring Boot microservices, Spring Security, JPA/Hibernate, and responsive UI integration.',
    '["Spring Boot 3.x with Spring Security", "REST API Contract Design with Swagger", "PostgreSQL / MySQL Schema Architecture", "React or Angular Frontend Integration", "Docker Containerization Setup"]',
    '["Java 21", "Spring Boot", "Hibernate/JPA", "PostgreSQL", "React", "Docker"]',
    '["Interview-ready architectural knowledge", "Industry-standard coding conventions", "Complete documentation & synopsis assistance"]',
    'Coffee',
    2,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    3,
    1,
    'React & Full-Stack Modern Web Projects',
    'react-fullstack-student-projects',
    'Modern full-stack web applications mentoring combining React, TypeScript, Node.js/C#, and modern databases.',
    'Learn how modern tech companies build applications. Build single page applications using React, TypeScript, Tailwind CSS, coupled with robust backend APIs and live deployment.',
    '["React 19 & TypeScript Development", "State Management & React Hook Form", "Responsive Tailwind CSS UI", "JWT Authentication Flow", "Cloud Deployment on Vercel / Render"]',
    '["React", "TypeScript", "Tailwind CSS", "Vite", "Node.js / .NET", "PostgreSQL"]',
    '["Portfolio-worthy live project URL", "Modern frontend design principles", "Complete viva explanation"]',
    'Layout',
    3,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    4,
    1,
    'Project Demonstration & Viva Preparation',
    'project-viva-demonstration-prep',
    'Rigorous mock viva sessions, source code explanation, architecture diagrams, and slide deck guidance.',
    'Having a running project is only half the battle. We prepare students for university external examiners through mock viva sessions, code explanation, and question prediction.',
    '["Deep dive into project source code", "System architecture & ER diagram explanation", "Top 50 expected viva questions & answers", "PowerPoint presentation coaching", "Local setup & emergency troubleshooting"]',
    '["UML", "ER Modeling", "Code Review", "Presentation Coaching"]',
    '["Zero stage fear during viva", "Clear articulation of design choices", "Guaranteed technical confidence"]',
    'Award',
    4,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    5,
    2,
    '.NET & C# Bug Fixing and Web API Development',
    'dotnet-csharp-bug-fixing',
    'Expert debugging, asynchronous deadlocks, EF Core query issues, and memory leak resolution.',
    'Are you stuck on a stubborn bug in your production ASP.NET Core API or enterprise C# codebase? Our senior engineers isolate and resolve root causes rapidly.',
    '["Async/await concurrency and deadlock analysis", "EF Core N+1 query and change tracking fixes", "Dependency injection lifecycle issues", "Custom middleware and filters debugging", "Unit & integration test fixes"]',
    '[".NET 8 / 9 / 10", "C#", "ASP.NET Core", "EF Core", "xUnit"]',
    '["Rapid turnaround time", "Root cause explanation", "Prevention recommendations"]',
    'Bug',
    5,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    6,
    2,
    'React Frontend Troubleshooting & State Management',
    'react-frontend-troubleshooting',
    'Troubleshooting complex React rendering issues, hooks dependencies, CORS, and API integrations.',
    'Resolve unexpected re-renders, useEffect loops, broken authentication state, hydration errors, or CORS issues with senior React developer assistance.',
    '["Component re-render optimization", "Axios interceptor & JWT token refresh debugging", "React Router dynamic routing fixes", "React Hook Form & Zod schema validation", "Cross-browser UI responsiveness fixes"]',
    '["React", "TypeScript", "Redux Toolkit", "Zustand", "Axios", "Tailwind"]',
    '["Clean, maintainable component hierarchy", "Zero layout shift", "Proper error boundaries"]',
    'Layers',
    6,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    7,
    2,
    'Database Optimization & Complex Queries (PostgreSQL / SQL Server)',
    'database-optimization-tuning',
    'Execution plan analysis, index tuning, slow query rewriting, and deadlocks resolution.',
    'Transform slow-loading dashboards and API timeouts into millisecond queries. We profile execution plans, construct optimal composite indexes, and optimize schemas.',
    '["EXPLAIN ANALYZE query profiling", "B-tree, GIN & partial index strategy", "Connection pooling and transaction tuning", "Partitioning & normalization guidance", "Migration scripting & data integrity"]',
    '["PostgreSQL", "SQL Server", "MySQL", "pgAdmin", "DBeaver"]',
    '["Up to 10x query performance improvement", "Reduced server CPU & memory usage", "Prevention of database connection exhaustion"]',
    'Database',
    7,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    8,
    3,
    'Custom Business & Enterprise Web Applications',
    'custom-business-web-applications',
    'Tailored, secure, high-performance web systems engineered specifically for your companys workflows.',
    'Off-the-shelf software often fails to fit your unique business operations. We build tailored web systems that automate operations, integrate with your tools, and scale seamlessly.',
    '["Bespoke operational workflows and dashboards", "Role-based access control & audit logging", "High-performance REST API backend", "Interactive data visualizations and reporting", "Mobile-responsive executive interfaces"]',
    '["ASP.NET Core", "React", "PostgreSQL", "Tailwind CSS", "Docker"]',
    '["100% intellectual property ownership", "Zero per-seat licensing fees", "Tailored to your exact business rules"]',
    'Briefcase',
    8,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    9,
    3,
    'Scalable E-Commerce & Inventory Management',
    'ecommerce-inventory-systems',
    'Modern digital storefronts, real-time inventory tracking, multi-warehouse support, and order processing.',
    'Empower your retail or wholesale business with robust catalog management, cart flows, automated invoice generation, inventory alerts, and payment integrations.',
    '["Fast product search and filtering", "Multi-tier pricing and discount rules", "Real-time stock reservation and deduction", "Automated PDF invoices & quotation generator", "Payment gateway readiness"]',
    '["React", "ASP.NET Core", "PostgreSQL", "Redis", "QuestPDF"]',
    '["Never oversell stock again", "Instant automated billing & receipt emails", "Intuitive merchant admin dashboard"]',
    'ShoppingCart',
    9,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    10,
    3,
    'CRM & Workflow Automation Systems',
    'crm-workflow-automation',
    'Lead pipeline tracking, customer interactions history, quotation management, and automated email follow-ups.',
    'Capture incoming leads, manage customer interactions, generate quotes in minutes, and track project status from initial contact to final payment.',
    '["Lead capture and pipeline stages", "Quotation builder with PDF generation", "Email outbox with delivery tracking", "Activity timeline and customer history", "Revenue analytics and forecasting"]',
    '[".NET 10", "React", "PostgreSQL", "MimeKit", "Recharts"]',
    '["Accelerate lead-to-quote conversion", "Centralized customer communication history", "Comprehensive managerial insights"]',
    'Users',
    10,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Id") DO NOTHING;

-- Reset sequence for services
SELECT setval(pg_get_serial_sequence('services', 'Id'), COALESCE(MAX("Id"), 1)) FROM services;

-- 4. Portfolio Projects
INSERT INTO portfolio_projects ("Id", "Title", "Slug", "ShortDescription", "DetailedDescription", "Category", "ThumbnailUrl", "LiveDemoUrl", "GitHubUrl", "TechnologiesJson", "FeaturesJson", "DisplayOrder", "IsFeatured", "IsActive", "CreatedAt")
VALUES
(
    1,
    'Enterprise ERP & Multi-Warehouse Inventory System',
    'enterprise-erp-inventory-system',
    'A complete enterprise resource planning suite with real-time stock tracking, automated purchase orders, and PDF invoicing.',
    'Engineered for a manufacturing client managing 4 distribution warehouses. Features role-based permissions, automated stock replenishment triggers, batch tracking, and interactive financial reporting.',
    'Business & Enterprise Software Development',
    'https://images.unsplash.com/photo-1551288049-bebda4e38f71?auto=format&fit=crop&w=800&q=80',
    'https://demo.softwareservices.example/erp',
    'https://github.com/example/enterprise-erp',
    '["ASP.NET Core Web API", "React 19", "PostgreSQL", "QuestPDF", "Tailwind CSS"]',
    '["Multi-Warehouse Stock Ledger", "Automated GST/VAT Invoicing", "Real-Time Low Stock Alerts", "Executive Analytics Dashboard"]',
    1,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    2,
    'Healthcare Telemedicine & Appointment Booking Platform',
    'telemedicine-appointment-booking',
    'Doctor consultation scheduling, prescription management, and secure patient portal.',
    'Built for medical clinics to manage patient records, digital consultation queues, online payment processing, and SMS/Email appointment reminders.',
    'Business & Enterprise Software Development',
    'https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?auto=format&fit=crop&w=800&q=80',
    'https://demo.softwareservices.example/health',
    'https://github.com/example/health-portal',
    '[".NET 10", "PostgreSQL", "React", "Tailwind CSS", "JWT Auth"]',
    '["Doctor Slot Availability Engine", "Secure Medical History Storage", "Automated Patient Notifications", "Integrated Billing & Receipts"]',
    2,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    3,
    'College Campus Placement & Student Performance Tracker (M.Tech Final Year)',
    'campus-placement-student-tracker',
    'Comprehensive university project mentoring: automated resume parsing, eligibility filtering, and company interview tracking.',
    'Mentored M.Tech computer science students through the full lifecycle of designing, architecting, and defending this placement management system. Implemented clean architecture and PostgreSQL database optimization.',
    'Student Project Guidance',
    'https://images.unsplash.com/photo-1523240795612-9a054b0db644?auto=format&fit=crop&w=800&q=80',
    'https://demo.softwareservices.example/placement',
    'https://github.com/example/student-placement-tracker',
    '["ASP.NET Core Web API", "React", "PostgreSQL", "Entity Framework Core"]',
    '["Automated Criteria Filtering", "Student Academic Transcript Verification", "Interview Schedule Management", "Complete Project Report & IEEE Documentation"]',
    3,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
),
(
    4,
    'High-Throughput E-Commerce API & Microservices Debugging',
    'ecommerce-api-performance-debugging',
    'Technical consulting project resolving database deadlocks and slow latency under high concurrent traffic.',
    'Diagnosed severe API slowdowns during flash sales for an online apparel brand. Refactored EF Core queries, introduced composite PostgreSQL indexes, and optimized connection pooling, cutting latency by 78%.',
    'Developer Technical Support',
    'https://images.unsplash.com/photo-1460925895917-afdab827c52f?auto=format&fit=crop&w=800&q=80',
    'https://demo.softwareservices.example/ecommerce',
    'https://github.com/example/ecommerce-api-optimization',
    '["PostgreSQL Query Tuning", "ASP.NET Core", "Redis Caching", "Docker"]',
    '["Optimized Composite B-Tree Indexes", "Resolved EF Core N+1 Query Traps", "Transaction Isolation Level Tuning", "Performance Stress Testing Benchmark"]',
    4,
    TRUE,
    TRUE,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Id") DO NOTHING;

SELECT setval(pg_get_serial_sequence('portfolio_projects', 'Id'), COALESCE(MAX("Id"), 1)) FROM portfolio_projects;

-- 5. Blog Categories
INSERT INTO blog_categories ("Id", "Name", "Slug", "Description", "CreatedAt")
VALUES
(1, '.NET & Architecture', 'dotnet-architecture', 'Deep dives into .NET, C#, and enterprise design patterns.', CURRENT_TIMESTAMP),
(2, 'React & Frontend', 'react-frontend', 'Modern React patterns, performance tuning, and state management.', CURRENT_TIMESTAMP),
(3, 'Database Engineering', 'database-engineering', 'PostgreSQL indexing, query optimization, and schema design.', CURRENT_TIMESTAMP)
ON CONFLICT ("Id") DO NOTHING;

SELECT setval(pg_get_serial_sequence('blog_categories', 'Id'), COALESCE(MAX("Id"), 1)) FROM blog_categories;

-- 6. Blog Posts
INSERT INTO blog_posts ("Id", "Title", "Slug", "Summary", "Content", "BlogCategoryId", "ThumbnailUrl", "AuthorName", "IsPublished", "PublishedAt", "ViewCount", "TagsJson", "CreatedAt")
VALUES
(
    1,
    'Architecting Clean, Maintainable ASP.NET Core Web APIs in .NET 10',
    'architecting-clean-aspnet-core-web-api-dotnet-10',
    'Discover how separating Domain, Application, Infrastructure, and API layers eliminates spaghetti code and empowers testability.',
    'Building robust web APIs requires more than just scaffolding controllers. In this guide, we break down Clean Architecture principles for modern ASP.NET Core applications.',
    1,
    'https://images.unsplash.com/photo-1517694712202-14dd9538aa97?auto=format&fit=crop&w=800&q=80',
    'Senior Architect',
    TRUE,
    CURRENT_TIMESTAMP,
    142,
    '[".NET 10", "Clean Architecture", "C#", "Web API"]',
    CURRENT_TIMESTAMP
),
(
    2,
    'PostgreSQL Indexing Strategies: Eliminating Slow Queries in Production',
    'postgresql-indexing-strategies-eliminating-slow-queries',
    'Learn how to use EXPLAIN ANALYZE, composite indexes, and partial indexing to speed up queries by over 90%.',
    'Database queries are the number one bottleneck in modern web applications. We explore B-Tree, partial indexes, and execution plan optimization.',
    3,
    'https://images.unsplash.com/photo-1544383835-bda2bc66a55d?auto=format&fit=crop&w=800&q=80',
    'Database Specialist',
    TRUE,
    CURRENT_TIMESTAMP,
    210,
    '["PostgreSQL", "Database Optimization", "SQL", "Performance"]',
    CURRENT_TIMESTAMP
),
(
    3,
    'How to Ace Your University Final Year Project Viva: A Mentors Guide',
    'how-to-ace-university-final-year-project-viva',
    'Top strategies for B.Tech, MCA, and M.Tech students to confidently explain code architecture, ER diagrams, and overcome examiner scrutiny.',
    'Most students build functional code but lose marks because they cannot defend architectural choices to external examiners. Here is how to prepare and succeed.',
    1,
    'https://images.unsplash.com/photo-1434030216411-0b793f4b4173?auto=format&fit=crop&w=800&q=80',
    'Academic Project Lead',
    TRUE,
    CURRENT_TIMESTAMP,
    385,
    '["Student Mentoring", "Project Viva", "MCA", "B.Tech"]',
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Id") DO NOTHING;

SELECT setval(pg_get_serial_sequence('blog_posts', 'Id'), COALESCE(MAX("Id"), 1)) FROM blog_posts;

-- 7. Testimonials
INSERT INTO testimonials ("Id", "ClientName", "ClientRole", "CompanyOrInstitution", "Feedback", "Rating", "AvatarUrl", "ProjectCategory", "IsFeatured", "IsActive", "DisplayOrder", "CreatedAt")
VALUES
(
    1,
    'Rahul Sharma',
    'M.Tech Computer Science Student',
    'NIT Rourkela',
    'The guidance on my Spring Boot and React project was outstanding. Not only did they help me structure the clean architecture, but the 1-on-1 viva coaching helped me achieve an A grade with special appreciation from the external examiner!',
    5,
    'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=200&q=80',
    'Student Project Guidance',
    TRUE,
    TRUE,
    1,
    CURRENT_TIMESTAMP
),
(
    2,
    'Vikram Patel',
    'Lead Backend Engineer',
    'Fintech Solutions Ltd',
    'We had a critical PostgreSQL locking and concurrency bug right before our major launch. Their developer support team pinpointed the uncommitted transaction issue within 2 hours. Exceptional technical depth and professionalism.',
    5,
    'https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?auto=format&fit=crop&w=200&q=80',
    'Developer Technical Support',
    TRUE,
    TRUE,
    2,
    CURRENT_TIMESTAMP
),
(
    3,
    'Priya Nambiar',
    'Operations Director',
    'Apex Logistics Hub',
    'They developed our customized warehouse and billing system from scratch. The quotation was clear, deliverables were met ahead of schedule, and the platform has transformed our daily dispatch operations.',
    5,
    'https://images.unsplash.com/photo-1580489944761-15a19d654956?auto=format&fit=crop&w=200&q=80',
    'Business & Enterprise Software Development',
    TRUE,
    TRUE,
    3,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("Id") DO NOTHING;

SELECT setval(pg_get_serial_sequence('testimonials', 'Id'), COALESCE(MAX("Id"), 1)) FROM testimonials;

-- 8. Website Settings
INSERT INTO website_settings ("Id", "SettingKey", "SettingValue", "SettingGroup", "Description", "CreatedAt")
VALUES
(1, 'Business:Name', 'Your Business Name', 'General', 'Official trading company name', CURRENT_TIMESTAMP),
(2, 'Business:Email', 'your-email@example.com', 'Contact', 'Primary inquiry and support email', CURRENT_TIMESTAMP),
(3, 'Business:Phone', '+91XXXXXXXXXX', 'Contact', 'Primary phone number', CURRENT_TIMESTAMP),
(4, 'Business:Address', 'Your Business Address', 'Contact', 'Physical office or technical lab address', CURRENT_TIMESTAMP),
(5, 'Business:Website', 'https://example.com', 'General', 'Company website URL', CURRENT_TIMESTAMP),
(6, 'Business:Hours', 'Mon - Sat: 9:00 AM - 7:00 PM IST', 'General', 'Working hours', CURRENT_TIMESTAMP),
(7, 'Social:LinkedIn', 'https://linkedin.com', 'Social', 'LinkedIn profile URL', CURRENT_TIMESTAMP),
(8, 'Social:GitHub', 'https://github.com', 'Social', 'GitHub organization URL', CURRENT_TIMESTAMP)
ON CONFLICT ("Id") DO NOTHING;

SELECT setval(pg_get_serial_sequence('website_settings', 'Id'), COALESCE(MAX("Id"), 1)) FROM website_settings;

COMMIT;
