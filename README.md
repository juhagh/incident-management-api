[![CI](https://github.com/juhagh/incident-management-api/actions/workflows/ci.yml/badge.svg)](https://github.com/juhagh/incident-management-api/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com)
[![JWT](https://img.shields.io/badge/Auth-JWT-black?logo=jsonwebtokens)](https://jwt.io)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

# IncidentManagementApi

ASP.NET Core Web API demonstrating domain-driven incident lifecycle rules, JWT-protected command endpoints, optimistic concurrency with ETags, and integration-tested HTTP behaviour.

This project is designed to go beyond basic CRUD. Incidents are treated as real domain entities with enforced lifecycle transitions, state-changing endpoints are protected with authentication, and the API implements conditional requests and concurrency checks using standard HTTP semantics.

---

## What This Project Demonstrates

- **Layered backend design** across API, Application, Domain, and Infrastructure
- **Domain-enforced business rules** instead of relying only on controller validation
- **JWT authentication** for protected command endpoints
- **Optimistic concurrency** using `ETag` and `If-Match`
- **Conditional GET support** using `If-None-Match`
- **DTO-based API contracts**
- **Automated testing** across both domain logic and HTTP behaviour
- **Containerised local setup** with Docker Compose and PostgreSQL

---

## Tech Stack

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Bearer authentication
- xUnit
- SQLite for API integration tests
- Docker / Docker Compose

---

## Architecture

The solution is organised into separate projects with clear responsibilities:

- **API** – controllers, authentication, HTTP concerns, response behaviour, ETag handling
- **Application** – DTOs, interfaces, service orchestration, command/query handling
- **Domain** – entities, enums, lifecycle rules, and business invariants
- **Infrastructure** – EF Core persistence, mappings, token generation, and database access

Request flow:

`HTTP request -> Controller -> Application service -> DbContext -> Database`

---

## Key Backend Behaviours

### Incident lifecycle modelling

Incidents are not treated as freely editable records. The domain model enforces valid transitions between states.

Supported statuses:

- `Open`
- `Assigned`
- `InProgress`
- `Waiting`
- `Resolved`
- `Invalid`
- `Closed`

Examples of enforced rules:

- an incident can be assigned from `Open`, `Assigned`, `InProgress`, or `Waiting`
- progress can only start from `Assigned` or `Waiting`
- resolving requires the incident to be `InProgress`
- closing is only allowed from `Resolved` or `Invalid`

Invalid transitions are rejected at the domain level and mapped to appropriate HTTP responses.

### Authentication

The API includes JWT authentication for state-changing endpoints.

Implemented behaviour:

- `POST /auth/login` issues a JWT for a known user
- `GET` endpoints are available anonymously
- command endpoints require authentication

This keeps reads simple while protecting write operations.

### Optimistic concurrency and caching

A key focus of this project is HTTP-aware concurrency behaviour.

For `GET /api/incidents/{id}`, the API:

- returns `404 Not Found` when the incident does not exist
- returns `200 OK` with an incident response DTO when found
- includes an `ETag` representing the current row version
- supports `If-None-Match`
- returns `304 Not Modified` when the client's cached version is still current

For state-changing endpoints, the API uses `If-Match` preconditions:

- `428 Precondition Required` when `If-Match` is missing
- `400 Bad Request` when `If-Match` is malformed
- `412 Precondition Failed` when the supplied ETag is stale
- `409 Conflict` when the requested operation violates domain lifecycle rules

This demonstrates optimistic concurrency at the HTTP contract level rather than relying only on database exceptions.

---

## Example Endpoints

### Authentication

- `POST /auth/login`

### Query

- `GET /api/incidents/{id}`

### Commands

- `POST /api/incidents`
- `POST /api/incidents/{id}/assign-engineer`
- `POST /api/incidents/{id}/start-progress`
- `POST /api/incidents/{id}/mark-waiting`
- `POST /api/incidents/{id}/resolve`
- `POST /api/incidents/{id}/mark-invalid`
- `POST /api/incidents/{id}/close`

---

## Project Structure

```text
IncidentManagementApi.sln
├── src
│   ├── API
│   │   ├── Controllers
│   │   ├── Http
│   │   ├── Program.cs
│   ├── Application
│   │   ├── DTOs
│   │   ├── Interfaces
│   │   └── Services
│   ├── Domain
│   │   ├── Entities
│   │   └── Enums
│   └── Infrastructure
│       ├── Auth
│       ├── Configurations
│       ├── Migrations
│       └── Persistence
└── tests
    ├── API.Tests
    └── Domain.Tests
```

---

## Running locally

### Prerequisites

- [Docker](https://www.docker.com/)

### 1. Start the API and database

From the repository root:

```bash
docker compose up -d --build
```

This starts the API and a PostgreSQL database. Database migrations run automatically on startup.

The API will be available at `http://localhost:8080`.

### 2. Authenticate and get a JWT

```bash
curl http://localhost:8080/auth/login \
  --request POST \
  --header 'Content-Type: application/json' \
  --data '{
    "username": "User1",
    "password": "VerySecretPassword1!"
  }'
```

Copy the returned token and export it:

```bash
export JWT_TOKEN="<PASTE_JWT_TOKEN_HERE>"
```

### 3. Create an incident

```bash
curl http://localhost:8080/api/incidents \
  --request POST \
  --header 'Content-Type: application/json' \
  --header "Authorization: Bearer $JWT_TOKEN" \
  --data '{
    "title": "Test Incident",
    "description": "Description of incident",
    "severity": "Critical",
    "networkElementId": 1
  }'
```

Copy the returned `id` from the response body:

```bash
export INCIDENT_ID="<PASTE_INCIDENT_ID_HERE>"
```

### 4. Retrieve the incident and note the ETag

```bash
curl -i http://localhost:8080/api/incidents/$INCIDENT_ID
```

The response headers will include an `ETag`, for example:

```http
ETag: W/"1"
```

Copy that value and export it:

```bash
export ETAG='W/"1"'
```

### 5. Execute a protected command with `If-Match`

Example: assign an engineer

```bash
curl http://localhost:8080/api/incidents/$INCIDENT_ID/assign-engineer \
  --request POST \
  --header 'Content-Type: application/json' \
  --header "Authorization: Bearer $JWT_TOKEN" \
  --header "If-Match: $ETAG" \
  --data '{
    "engineerId": 1
  }'
```

### 6. Refresh the ETag before the next command

After a successful command, retrieve the incident again to get the latest `ETag` before issuing another state-changing request:

```bash
curl -i http://localhost:8080/api/incidents/$INCIDENT_ID
```

Then update the `ETAG` variable with the new value before sending the next command.

An updated `ETag` is also returned in the response headers of a successful command.

### 7. Further example commands

To move an incident to `Resolved`, it must first be in `InProgress`.

#### Start progress

```bash
curl http://localhost:8080/api/incidents/$INCIDENT_ID/start-progress \
  --request POST \
  --header "Authorization: Bearer $JWT_TOKEN" \
  --header "If-Match: $ETAG"
```

After a successful command, retrieve the incident again and update the `ETAG` value before issuing the next state-changing request.

#### Resolve

```bash
curl http://localhost:8080/api/incidents/$INCIDENT_ID/resolve \
  --request POST \
  --header 'Content-Type: application/json' \
  --header "Authorization: Bearer $JWT_TOKEN" \
  --header "If-Match: $ETAG" \
  --data '{
    "resolutionSummary": "Incident resolution description"
  }'
```

### 8. Stop the application and database

```bash
docker compose down
```

---

## Testing

The solution includes both domain unit tests and API integration tests.

### Domain tests cover

- incident creation guard clauses
- valid and invalid lifecycle transitions
- assignment rules
- waiting, resolving, invalidation, and close behaviour
- invariant protection on failed operations

### API integration tests cover

- authenticated and unauthenticated endpoint access
- GET success, 404, and conditional 304
- incident creation behaviour
- If-Match precondition handling
- successful command execution
- invalid transitions mapped to 409 Conflict

The goal of the test suite is to verify both business rules and the HTTP contract exposed by the API.

---

## Possible Extensions

- return structured ProblemDetails bodies for command failures
- add filtering, sorting, and pagination for incident queries
- replace in-memory users with persistent identity storage
- add refresh tokens and a fuller authentication flow
- expand documentation with additional request/response examples

---

## Why I Built This

I built this project to practise backend concerns that matter in real systems: domain modelling, API design, authentication, optimistic concurrency, HTTP semantics, and automated testing.
