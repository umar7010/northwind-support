# Implementation Approach

## Task 1 - Defect Fix and Filtering
I investigated the ticket search flow from the React filters through the API to the database query. The main issue was that the backend was not applying the requested filters. I implemented filtering in the database query so that pagination and result counts remain consistent. I verified the changes through testing and browser requests.

## Task 2 - Escalation, Assignment and SLA
I followed the existing application architecture and extended the ticket management functionality. I implemented automatic agent selection based on availability, capacity and specialization. I added escalation history and SLA calculations, including recalculation after priority changes. I also recorded the authenticated agent responsible for an escalation.

## Task 3 - Authentication
I used ASP.NET Core Identity PasswordHasher for password storage and JWT Bearer authentication for API access. I added login and current-agent endpoints, protected API requests, and implemented login, logout and protected routes in React. I tested valid and invalid logins and unauthorized access.

## Task 4 - Docker
I did not complete API Dockerisation. The development database continues to run through Docker.

## Testing and Verification
I used backend unit tests, frontend builds, API requests and manual browser testing. The five escalation tests passed successfully.

## AI Assistance
I used ChatGPT to understand the existing codebase, plan changes, troubleshoot errors and review testing approaches. I applied the changes locally and checked their results.

## Limitations
API Dockerisation, production deployment, refresh tokens, password reset, MFA and role-based authorization were not implemented.
