# REST API Documentation
**Software Services Business Platform**

---

## 1. Overview & Base URL

- **Development Base URL**: `http://localhost:5000/api`
- **Swagger Interactive Documentation**: `http://localhost:5000/swagger`
- **Response Structure**: All endpoints return standard envelope `ApiResponse<T>`:
```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": { ... },
  "errors": []
}
```

---

## 2. Authentication & Authorization

All administrative endpoints under `/api/admin/*` require JWT Bearer authentication.
Pass the token in the HTTP Authorization header:
```http
Authorization: Bearer <your_jwt_token>
```

### Auth Endpoints

#### `POST /api/auth/login`
Authenticates administrator credentials and returns a 24-hour JWT token.
- **Request Body**:
```json
{
  "usernameOrEmail": "admin",
  "password": "Admin@123456"
}
```
- **Response (200 OK)**:
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
    "expiresAt": "2026-11-19T18:00:00Z",
    "user": {
      "id": 1,
      "username": "admin",
      "email": "admin@softwareservices.com",
      "fullName": "Super Administrator",
      "role": "SuperAdmin",
      "lastLoginAt": "2026-11-18T17:00:00Z"
    }
  }
}
```

#### `GET /api/auth/profile` *(Requires Auth)*
Retrieves current authenticated administrator user identity.

#### `POST /api/auth/change-password` *(Requires Auth)*
Updates admin password using cryptographic PBKDF2 hashing.

---

## 3. Public API Endpoints (No Auth Required)

### 3.1 Content & Catalog

#### `GET /api/service-categories`
Returns all active service categories and their associated services.

#### `GET /api/services`
Query parameters: `categorySlug` (optional), `featuredOnly` (boolean).
Returns list of services with features, technologies, and benefits.

#### `GET /api/services/{slug}`
Retrieves specific service detail by URL slug.

#### `GET /api/portfolio`
Query parameters: `category` (optional), `featuredOnly` (boolean).
Returns showcase projects with live demo and GitHub links.

#### `GET /api/portfolio/{slug}`
Retrieves detailed portfolio case study.

#### `GET /api/blog`
Query parameters: `categorySlug` (optional), `page` (int), `pageSize` (int).
Returns published technical articles.

#### `GET /api/blog/{slug}`
Retrieves blog article by URL slug and increments view count.

#### `GET /api/testimonials`
Query parameter: `featuredOnly` (boolean). Returns client reviews and ratings.

#### `GET /api/settings`
Returns dictionary of public company settings (Name, Email, Phone, Address, WorkingHours).

---

### 3.2 Lead & Quotation Workflow

#### `POST /api/enquiries`
Submits a customer enquiry / quotation request with optional file attachment.
- **Request (Multipart Form or JSON)**:
  - `fullName`: string (required)
  - `email`: string (valid email, required)
  - `phoneNumber`: string (required)
  - `companyName`: string (optional)
  - `serviceCategoryId`: integer (optional)
  - `requiredTechnology`: string (required)
  - `projectTitle`: string (required)
  - `projectDescription`: string (required)
  - `estimatedBudget`: string (required)
  - `expectedCompletionDate`: ISO timestamp (optional)
  - `additionalRequirements`: string (optional)
  - `file`: binary attachment (optional, max 10MB)
- **Response (200 OK)**:
```json
{
  "success": true,
  "message": "Enquiry submitted successfully.",
  "data": {
    "referenceNumber": "ENQ-20261118-A39B",
    "customerEmail": "customer@example.com",
    "createdAt": "2026-11-18T17:30:00Z"
  }
}
```

#### `GET /api/public-quotations/{secureToken}`
Retrieves customer-facing quotation proposal without requiring login.
- **Response (200 OK)**:
  Returns itemized deliverables, development cost, tax, advance, balance, terms, and status.

#### `POST /api/public-quotations/{secureToken}/respond`
Customer approval or rejection of quotation.
- **Request Body**:
```json
{
  "isAccepted": true,
  "remarks": "Quotation approved. Ready to pay advance."
}
```
- **Behavior**: If accepted, quotation status transitions to `Accepted`, and a project is automatically provisioned.

#### `GET /api/public-quotations/{secureToken}/pdf`
Downloads the official QuestPDF commercial proposal document.

---

## 4. Admin API Endpoints (`/api/admin/*`, Requires Auth)

### 4.1 Dashboard Analytics
- `GET /api/admin/dashboard`: Returns 9 core metrics (total enquiries, new enquiries, pending quotes, accepted quotes, active projects, revenue, conversion rate, monthly charts).

### 4.2 Enquiry Management
- `GET /api/admin/enquiries`: Paginated list with filters (`status`, `search`, `serviceCategoryId`, `page`, `pageSize`).
- `GET /api/admin/enquiries/{id}`: Full enquiry detail including notes thread and uploaded attachments.
- `PUT /api/admin/enquiries/{id}/status`: Updates lead status (New, Reviewed, Quoted, Converted, Closed, Spam).
- `POST /api/admin/enquiries/{id}/notes`: Adds internal staff note.
- `GET /api/admin/enquiries/{id}/attachments/{attachmentId}/download`: Securely streams uploaded files.

### 4.3 Quotation Management
- `GET /api/admin/quotations`: Paginated list of quotations with search and status filters.
- `POST /api/admin/quotations`: Creates new itemized quotation from an enquiry.
- `POST /api/admin/quotations/{id}/send`: Sends quotation via email, generates PDF, sets status to `Sent`.
- `GET /api/admin/quotations/{id}/pdf`: Downloads quotation PDF.

### 4.4 Project Management
- `GET /api/admin/projects`: Paginated project list with progress percentage and status filters.
- `GET /api/admin/projects/{id}`: Detailed project view with milestones and financial ledger.
- `PUT /api/admin/projects/{id}/status`: Updates project delivery status (InProgress, OnHold, Completed).
- `PUT /api/admin/projects/{id}/progress`: Updates progress slider percentage (0 - 100).
- `POST /api/admin/projects/{id}/milestones`: Adds project milestone.
- `PUT /api/admin/projects/{id}/milestones/{milestoneId}/toggle`: Toggles milestone completion.

### 4.5 Financials & Payments
- `GET /api/admin/payments`: Paginated payment transactions.
- `POST /api/admin/payments`: Records project receipt (Advance, Milestone, Final) and recalculates project balances.

### 4.6 Content & Settings
- `GET /api/admin/customers`: Customer directory with lead and project statistics.
- `POST /api/admin/services`: Creates new service offering.
- `PUT /api/admin/services/{id}`: Updates service offering.
- `DELETE /api/admin/services/{id}`: Deletes service offering.
- `POST /api/admin/portfolio`: Creates portfolio case study.
- `PUT /api/admin/portfolio/{id}`: Updates portfolio case study.
- `DELETE /api/admin/portfolio/{id}`: Deletes portfolio case study.
- `POST /api/admin/blog`: Creates blog article.
- `PUT /api/admin/blog/{id}`: Updates blog article.
- `DELETE /api/admin/blog/{id}`: Deletes blog article.
- `PUT /api/admin/settings`: Updates website configuration setting.
- `POST /api/admin/settings/testimonials`: Adds client testimonial.
