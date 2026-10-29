# Northwind Support Desk

Northwind Support Desk is a support ticket management application built using **ASP.NET Core (.NET 10), React with TypeScript, and SQL Server**.

I worked on this project as part of the Verdentra Software Engineering Internship technical assignment. The repository was provided with an existing application, and my task was to identify and fix issues, implement additional functionality, and improve its usability and security.

I focused on completing Tasks 1, 2, and 3. Task 4, which involves Dockerising the API, was not completed.

## 1. What I Implemented

### Task 1 — Bug Fixing and Server-Side Filtering

The first task involved fixing the existing ticket filtering functionality.

The main issue was that the frontend displayed filtering options, but the backend query did not apply the filters when retrieving tickets from the database.

I updated the ticket query logic to apply filtering on the server side rather than relying on frontend filtering.

The implementation includes filtering by ticket status, priority, category, customer, assigned agent, unassigned tickets, and search text. Sorting and pagination are also handled through the database query.

The filtered result count is calculated based on the matching records rather than the total number of tickets.

**Design decision:** I chose database-level filtering because it avoids loading unnecessary records into the frontend and allows pagination to work correctly.

**How to verify:** Open the tickets page, apply different filters, and check that the displayed tickets and result count change accordingly. The browser's Network tab can be used to confirm that filter values are included in the API request.

### Task 2 — Ticket Escalation, Assignment, and SLA Management

For the second task, I worked on improving how support tickets are assigned, escalated, and monitored.

**Automatic ticket assignment**

When a ticket is created, the application evaluates available agents and attempts to assign the ticket to a suitable agent.

The assignment considers whether an agent is active, their current workload, their maximum ticket capacity, and whether the ticket requires a specialist.

If a suitable agent is unavailable, the ticket remains unassigned with a reason recorded.

**Ticket escalation**

I implemented ticket escalation so that a ticket's priority can be increased when an issue becomes more urgent.

Escalation records contain the previous priority, new priority, reason, and timestamp. I also added `EscalatedByAgentId` to record the authenticated agent responsible for the escalation.

**SLA management**

I worked on SLA calculations based on ticket priority and customer tier.

When a ticket is escalated, its SLA deadline is recalculated. The application also evaluates ticket SLA status to help identify tickets that may be approaching or exceeding their deadlines.

**Design decision:** I kept escalation history separate from the main ticket record so previous escalations are not lost when the ticket changes.

### Task 3 — Login and Authentication

The third task involved implementing authentication for existing support agents.

I added email-and-password login using the existing agent records.

The authentication implementation includes:

- Password hashing using ASP.NET Core Identity's `PasswordHasher`.
- JWT token generation after successful login.
- JWT validation for protected API endpoints.
- Rejection of incorrect credentials and inactive agents.
- An endpoint to retrieve the currently authenticated agent.
- A React login page.
- Protected frontend routes.
- Automatic inclusion of the JWT in API requests.
- Logout functionality.
- Redirecting unauthenticated users to the login page and returning them to their intended page after login.
- Clearing the saved session when an API request returns HTTP 401.

**Design decisions**

I used ASP.NET Core Identity's password hashing functionality instead of storing passwords as plain text or implementing my own hashing algorithm.

I chose JWT authentication because it allows the API to validate requests without maintaining a server-side session.

The JWT is stored in browser local storage for this assignment. This makes it straightforward to attach the token to API requests, although it has security limitations compared with an appropriately configured HttpOnly cookie.

Tokens expire after 60 minutes. The signing key is provided through an environment variable instead of being committed to source control.

For failed login attempts, the API returns the same unauthorized response for incorrect credentials and inactive accounts.

## 2. Technologies Used

| Area | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10) |
| Frontend | React 19, TypeScript, Vite |
| Database | SQL Server |
| Data access | Entity Framework Core |
| Authentication | JWT Bearer Authentication |
| Password hashing | ASP.NET Core Identity PasswordHasher |
| Backend testing | xUnit |
| Frontend testing | Vitest |
| Development database | SQL Server in Docker |
| Version control | Git and GitHub |

## 3. Running the Project Locally

### Prerequisites

Install the following before starting:

- .NET SDK 10
- Node.js and npm
- Docker Desktop
- Git

### Step 1 — Clone the repository

```bash
git clone https://github.com/umar7010/northwind-support.git
cd northwind-support
```

### Step 2 — Install dependencies

```bash
npm install
```

### Step 3 — Start SQL Server

Make sure Docker Desktop is running.

```bash
npm run db:up
```

### Step 4 — Configure the JWT signing key

The API requires a JWT signing key for authentication.

On Windows PowerShell, generate and save a development signing key:

```powershell
$keyBytes = [byte[]]::new(64)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($keyBytes)
$env:Jwt__Key = [Convert]::ToBase64String($keyBytes)
```

This sets the key for the current PowerShell session. Keep it private and do not commit it to GitHub.

The development configuration uses:

- JWT issuer: `NorthwindSupportDesk`
- JWT audience: `NorthwindSupportDesk`
- Token expiration: 60 minutes

### Step 5 — Start the backend

In the same PowerShell terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project "apps/api/src/SupportDesk.Presentation" --no-launch-profile --urls "http://localhost:5043"
```

In Development, the API applies database migrations and seeds demonstration data.

The API will run at:

`http://localhost:5043`

### Step 6 — Start the frontend

Open a second terminal in the project directory.

```bash
npm run web
```

Open:

`http://localhost:5173`

The frontend is configured to forward API requests to the local backend.

## 4. Development Login Credentials

Use the following seeded account to test the application.

**Email:** `alex.turner@northwind-support.example`

**Password:** `NorthwindDev123!`

These credentials are intended only for local development and testing.

After signing in, the user can access the ticket management pages. Logging out clears the frontend authentication session.

## 5. Database Changes

I made database changes to support the new functionality.

These include ticket escalation history, SLA-related information, and password hashes for existing agents.

For authentication, I added a nullable `PasswordHash` field to the agent model and created an Entity Framework Core migration.

I also added `EscalatedByAgentId` to ticket escalation records to track which authenticated agent performed an escalation.

The new field is nullable so that existing escalation records can remain valid.

## 6. Testing

I used automated tests and manual testing to check the implemented features.

### Backend tests

Run:

```bash
dotnet test
```

To run the escalation tests specifically:

```bash
dotnet test apps/api/tests/SupportDesk.UnitTests/SupportDesk.UnitTests.csproj --filter "FullyQualifiedName~EscalateTicketCommandHandlerTests"
```

The escalation test results were:

**5 passed, 0 failed, 0 skipped.**

These tests cover priority increases, SLA recalculation, escalation history, invalid priority changes, and missing tickets.

The escalation history test also checks that the authenticated agent ID is recorded correctly.

### Frontend tests

From the frontend directory:

```bash
cd apps/web
npm test
```

### Manual verification

I also manually checked the following authentication scenarios:

- Logging in with valid credentials.
- Rejecting an incorrect password.
- Rejecting an inactive agent.
- Accessing protected API endpoints with a valid JWT.
- Rejecting protected API requests without a JWT.
- Redirecting unauthenticated users to the login page.
- Returning users to the originally requested page after login.
- Displaying the signed-in agent's name.
- Logging out successfully.

The frontend and backend builds were also completed successfully during development.

## 7. AI Tool Usage

I used ChatGPT as a development assistant while completing this assignment.

It helped me understand the existing project structure, identify relevant files, plan implementation steps, troubleshoot errors, and review testing approaches.

I implemented the changes in my local development environment and checked the results through builds, tests, and manual API and browser testing.

One example of AI-assisted troubleshooting was identifying that a backend build failure was caused by a running API process locking DLL files rather than a C# compilation error.

I also used AI assistance to understand the authentication flow and how the authenticated agent's identity should be obtained from a JWT instead of trusting frontend-supplied data.

## 8. Known Limitations

- Task 4, Dockerising the API, was not completed. Docker is currently used for the development SQL Server database.
- Authentication supports existing agents only. There is no registration or password reset functionality.
- JWT tokens are stored in local storage, which is a security trade-off for this assignment.
- Refresh tokens and multi-factor authentication are not implemented.
- The application does not include role-based access control.
- Production deployment and production migration procedures have not been completed.
- Additional automated authentication and frontend tests would improve coverage.
- The development credentials are intended for demonstration purposes and must not be used in production.

## 9. Final Notes

My main focus was to understand the existing application and make changes that fit its current architecture rather than rewriting large parts of the project.

I tried to keep the implementation straightforward, maintain the existing functionality, and verify changes as I progressed.

Tasks 1 and 2 were implemented, and the main authentication flow for Task 3 is working. Task 4 remains incomplete.

The repository's Git history contains the development changes made throughout the assignment.