-- ============================================================================
-- SCRIPT 01: CREATE DATABASE
-- Platform: PostgreSQL 14+ (Compatible with pgAdmin 4)
-- Project: Software Services Business Platform
-- ============================================================================

-- If executing through pgAdmin Query Tool, execute this statement while 
-- connected to the default 'postgres' database.
-- Note: 'CREATE DATABASE' cannot run inside a multi-statement transaction block.

CREATE DATABASE software_services_db
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'English_United States.1252'
    LC_CTYPE = 'English_United States.1252'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

COMMENT ON DATABASE software_services_db
    IS 'Production database for Software Services Platform (.NET 10 LTS + PostgreSQL + React)';
