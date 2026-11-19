-- ============================================================================
-- SCRIPT 04: INDEXES & PERFORMANCE OPTIMIZATION
-- Platform: PostgreSQL 14+ (Compatible with pgAdmin 4)
-- Project: Software Services Business Platform
-- ============================================================================

START TRANSACTION;

-- Unique Indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_admin_users_Username" ON admin_users ("Username");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_admin_users_Email" ON admin_users ("Email");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_customers_Email" ON customers ("Email");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_service_categories_Slug" ON service_categories ("Slug");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_services_Slug" ON services ("Slug");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_enquiries_ReferenceNumber" ON enquiries ("ReferenceNumber");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_quotations_QuotationNumber" ON quotations ("QuotationNumber");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_quotations_SecureToken" ON quotations ("SecureToken");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_projects_ProjectNumber" ON projects ("ProjectNumber");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_portfolio_projects_Slug" ON portfolio_projects ("Slug");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_blog_categories_Slug" ON blog_categories ("Slug");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_blog_posts_Slug" ON blog_posts ("Slug");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_website_settings_SettingKey" ON website_settings ("SettingKey");

-- Foreign Key & Filter Indexes
CREATE INDEX IF NOT EXISTS "IX_services_ServiceCategoryId" ON services ("ServiceCategoryId");
CREATE INDEX IF NOT EXISTS "IX_enquiries_CustomerId" ON enquiries ("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_enquiries_ServiceCategoryId" ON enquiries ("ServiceCategoryId");
CREATE INDEX IF NOT EXISTS "IX_enquiries_Status" ON enquiries ("Status");
CREATE INDEX IF NOT EXISTS "IX_enquiries_CreatedAt" ON enquiries ("CreatedAt" DESC);

CREATE INDEX IF NOT EXISTS "IX_enquiry_attachments_EnquiryId" ON enquiry_attachments ("EnquiryId");
CREATE INDEX IF NOT EXISTS "IX_enquiry_notes_EnquiryId" ON enquiry_notes ("EnquiryId");
CREATE INDEX IF NOT EXISTS "IX_enquiry_notes_AdminUserId" ON enquiry_notes ("AdminUserId");

CREATE INDEX IF NOT EXISTS "IX_quotations_CustomerId" ON quotations ("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_quotations_EnquiryId" ON quotations ("EnquiryId");
CREATE INDEX IF NOT EXISTS "IX_quotations_Status" ON quotations ("Status");
CREATE INDEX IF NOT EXISTS "IX_quotation_items_QuotationId" ON quotation_items ("QuotationId");
CREATE INDEX IF NOT EXISTS "IX_quotation_status_histories_QuotationId" ON quotation_status_histories ("QuotationId");

CREATE INDEX IF NOT EXISTS "IX_projects_CustomerId" ON projects ("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_projects_QuotationId" ON projects ("QuotationId");
CREATE INDEX IF NOT EXISTS "IX_projects_Status" ON projects ("Status");
CREATE INDEX IF NOT EXISTS "IX_project_milestones_ProjectId" ON project_milestones ("ProjectId");

CREATE INDEX IF NOT EXISTS "IX_payments_CustomerId" ON payments ("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_payments_ProjectId" ON payments ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_payments_PaymentDate" ON payments ("PaymentDate" DESC);

CREATE INDEX IF NOT EXISTS "IX_blog_posts_BlogCategoryId" ON blog_posts ("BlogCategoryId");
CREATE INDEX IF NOT EXISTS "IX_blog_posts_IsPublished" ON blog_posts ("IsPublished");

CREATE INDEX IF NOT EXISTS "IX_email_outbox_Status" ON email_outbox ("Status");
CREATE INDEX IF NOT EXISTS "IX_email_logs_SentAt" ON email_logs ("SentAt" DESC);

-- Partial Indexes for fast active record filtering
CREATE INDEX IF NOT EXISTS "IX_active_enquiries" ON enquiries ("Status", "CreatedAt" DESC) WHERE "IsDeleted" = FALSE;
CREATE INDEX IF NOT EXISTS "IX_pending_quotations" ON quotations ("Status", "ValidUntil") WHERE "IsDeleted" = FALSE AND "Status" IN (1, 2, 3);
CREATE INDEX IF NOT EXISTS "IX_active_projects" ON projects ("Status", "ExpectedCompletionDate") WHERE "IsDeleted" = FALSE AND "Status" IN (1, 2, 3);

COMMIT;
