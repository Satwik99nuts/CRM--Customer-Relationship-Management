# CRM API

The API uses ASP.NET Core Identity cookie authentication. Obtain an anti-forgery token before login or logout, retain the returned cookie, and send the returned token in the `RequestVerificationToken` header for state-changing authentication requests. Other API writes use the authenticated cookie and role authorization.

## Authentication

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/auth/antiforgery` | Anonymous | Returns `{ "requestToken": "..." }` and sets the anti-forgery cookie. |
| POST | `/api/auth/login` | Anonymous, rate limited | JSON `{ "email": "...", "password": "..." }`; returns user identity and roles, or 401. Send the anti-forgery header. |
| POST | `/api/auth/logout` | Authenticated | Signs out the current cookie session. Send the anti-forgery header. |

## CRM endpoints

All CRM endpoints require an authenticated Admin, Manager, or Sales Executive. Sales Executives are limited to their assigned records; Admins and Managers can see all records.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/customers?search=&page=1&pageSize=25` | Search and page customers. |
| GET | `/api/customers/{id}` | Read one accessible customer. |
| POST | `/api/customers` | Create a customer. |
| PUT | `/api/customers/{id}` | Update a customer. |
| DELETE | `/api/customers/{id}` | Deactivate when history exists; otherwise delete. |
| GET | `/api/leads?search=&status=&page=1&pageSize=25` | Search and page leads. |
| GET | `/api/leads/{id}` | Read one accessible lead. |
| POST | `/api/leads` | Create a lead. `Converted` is assigned through the MVC conversion workflow. |
| PUT | `/api/leads/{id}` | Update a lead. |
| DELETE | `/api/leads/{id}` | Delete only when no history or related opportunity exists; otherwise 409. |
| GET | `/api/opportunities?search=&stage=&page=1&pageSize=25` | Search and page opportunities. |
| GET | `/api/opportunities/{id}` | Read one accessible opportunity. |
| POST | `/api/opportunities` | Create an opportunity. |
| PUT | `/api/opportunities/{id}` | Update an opportunity. |
| DELETE | `/api/opportunities/{id}` | Delete when no linked follow-ups exist; otherwise 409. |
| GET | `/api/followups?status=&fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD&page=1&pageSize=25` | Filter and page follow-ups. |
| GET | `/api/followups/{id}` | Read one accessible follow-up. |
| POST | `/api/followups` | Create a follow-up linked to exactly one customer, lead, or opportunity. |
| PUT | `/api/followups/{id}` | Update an accessible follow-up and its single linked record. |
| DELETE | `/api/followups/{id}` | Delete an accessible follow-up. |
| GET | `/api/reports/pipeline` | Opportunity count, amount, and weighted amount by stage and owner. |

POST and PUT endpoints accept JSON matching the corresponding `CustomerInput`, `LeadInput`, `OpportunityInput`, or `FollowUpInput` in `ViewModels/ApiDtos.cs`. Data annotation failures return 400 with validation details. Missing/inaccessible records return 404, unauthenticated requests return 401, insufficient roles return 403, successful creation returns 201, and successful deletion returns 204. Business-rule conflicts return 409 where applicable.

## Example

```http
POST /api/customers
Content-Type: application/json
Cookie: .AspNetCore.Identity.Application=...

{
  "name": "A. Customer",
  "email": "customer@example.com",
  "phone": "+1 555 0100",
  "company": "Example Ltd",
  "address": "1 Main Street",
  "city": "Springfield",
  "state": "CA",
  "status": "Active"
}
```
