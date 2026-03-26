# IncidentManagementApi

Backend-focused ASP.NET Core Web API for managing incidents with explicit lifecycle rules, JWT authentication, optimistic concurrency, and HTTP caching support.

This project is designed to demonstrate backend development beyond simple CRUD. It models incidents as a real domain entity with enforced state transitions, protects command endpoints with authentication, uses ETags for conditional requests and optimistic concurrency, and includes both domain and API integration tests.

---

## Overview

The API is built around an **incident management domain** rather than generic data access. The goal is to practise production-relevant backend concerns such as:

- domain-driven state and lifecycle rules
- separation of concerns across layers
- DTO-based API contracts
- optimistic concurrency
- conditional HTTP requests with ETags
- authenticated command endpoints
- repeatable automated tests

The solution is organised into separate projects for:

- **API** – controllers, authentication, HTTP behaviour, response mapping
- **Application** – DTOs, interfaces, services, command/query orchestration
- **Domain** – entities, enums, invariants, and lifecycle rules
- **Infrastructure** – EF Core persistence, database configuration, token service

---

## Tech stack

- C#
- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Bearer authentication
- xUnit
- SQLite (used in API integration tests)

---

## Architecture

Request flow:

`HTTP request -> Controller -> Application service/query layer -> DbContext -> Database`

### Structure highlights

- **Controllers** handle HTTP concerns such as routing, status codes, headers, authentication, and conditional request behaviour
- **Application layer** contains DTOs, service abstractions, and command/query orchestration
- **Domain layer** contains the `Incident` aggregate and enforces lifecycle invariants
- **Infrastructure layer** handles EF Core persistence, database mappings, and JWT token generation

---

## Core features

This project demonstrates:

- layered backend design across API, Application, Domain, and Infrastructure
- incident lifecycle modelling with explicit state transitions
- domain-enforced business rules instead of controller-only validation
- JWT authentication for protected command endpoints
- optimistic concurrency using ETags and `If-Match`
- conditional `GET` support using `If-None-Match`
- DTO-based response shaping
- API integration tests and domain unit tests

---

## Incident lifecycle

Incidents are not treated as simple editable records. The domain enforces valid transitions between statuses.

### Main statuses

- `Open`
- `Assigned`
- `InProgress`
- `Waiting`
- `Resolved`
- `Invalid`
- `Closed`

### Example lifecycle rules

Examples of enforced business rules include:

- an incident can be assigned from `Open`, `Assigned`, `InProgress`, or `Waiting`
- progress can only start from `Assigned` or `Waiting`
- resolving requires the incident to be `InProgress`
- closing is only allowed from `Resolved` or `Invalid`
- invalid transitions throw at the domain level and are mapped to appropriate HTTP responses

This keeps lifecycle behaviour inside the domain model instead of scattering it across controllers or persistence code.

---

## Authentication and authorization

The API includes JWT authentication for command endpoints.

### Implemented behaviour

- `POST /auth/login` issues a JWT for a known user
- `GET` incident endpoints are available anonymously
- state-changing incident endpoints require authentication
- API integration tests verify both authorized and unauthorized behaviour

This demonstrates how to separate read and write concerns while protecting mutation endpoints.

---

## Optimistic concurrency and ETags

A key focus of this project is concurrency-aware API behaviour.

### `GET /api/incidents/{id}`

The incident retrieval endpoint:

- returns `404 Not Found` when the incident does not exist
- returns `200 OK` with an incident response DTO when found
- generates an `ETag` from the incident row version
- supports `If-None-Match`
- returns `304 Not Modified` when the client's cached version is still current

### Command endpoints

State-changing endpoints such as assign, start progress, resolve, and close use `If-Match` preconditions.

Implemented API behaviour includes:

- `428 Precondition Required` when `If-Match` is missing
- `400 Bad Request` when `If-Match` is malformed
- `412 Precondition Failed` when the supplied ETag is stale
- `409 Conflict` when the requested operation violates domain lifecycle rules

This demonstrates optimistic concurrency at the HTTP API level, rather than relying only on database exceptions.

---

## Example endpoints

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

## Testing

The solution includes both **domain unit tests** and **API integration tests**.

### Domain tests cover

- incident creation guard clauses
- valid and invalid lifecycle transitions
- assignment rules
- waiting, resolving, invalidation, and close behaviour
- invariant protection on failed operations

### API integration tests cover

- authenticated and unauthenticated endpoint access
- `GET` success, `404`, and conditional `304`
- incident creation behaviour
- `If-Match` precondition handling (`428`, `400`, `412`)
- successful command execution
- invalid transitions mapped to `409 Conflict`

The goal of the test suite is not just endpoint smoke testing, but proving the business rules and HTTP contract that make the project interesting.

---

## Project structure

```text
IncidentManagementApi.sln
├── src/
│   ├── API/
│   │   ├── Controllers/
│   │   ├── Authentication/
│   │   ├── Middleware/
│   │   └── Program.cs
│   ├── Application/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   └── CommandsQueries/
│   ├── Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Users/
│   └── Infrastructure/
│       ├── Authentication/
│       ├── Configurations/
│       ├── Persistence/
│       └── Services/
└── tests/
    ├── Domain.Tests/
    └── API.Tests/
```

---

## What this project demonstrates

This project helped me practise and understand:

- designing a backend around domain behaviour rather than simple CRUD
- structuring a multi-project solution with clear separation of concerns
- modelling lifecycle rules inside an aggregate
- protecting command endpoints with JWT authentication
- handling optimistic concurrency using ETags and preconditions
- mapping domain outcomes to appropriate HTTP responses
- writing domain and API tests around meaningful backend behaviour

---

## Running locally

### 1. Start the database

From the repository root:

```bash
docker compose up -d
```

### 2. Apply EF Core migrations

If `dotnet ef` is not installed on your machine yet:

```bash
dotnet tool install --global dotnet-ef
```

Then, from the repository root:

```bash
dotnet restore
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

### 3. Run the API

```bash
dotnet run --project src/API --launch-profile https
```

By default, the API runs at:

- `https://localhost:7281`
- `http://localhost:5073`

The examples below use `https://localhost:7281`.

> Note: if your local HTTPS development certificate is not trusted, your HTTP client may reject the request. In that case, trust the ASP.NET Core development certificate or use the HTTP endpoint for local testing.

### 4. Authenticate and get a JWT

```bash
curl https://localhost:7281/auth/login \
  --request POST \
  --header 'Content-Type: application/json' \
  --data '{
    "username": "User1",
    "password": "VerySecretPassword1!"
  }'
```

Copy the returned token and use it in the next requests:

```bash
export JWT_TOKEN="<PASTE_JWT_TOKEN_HERE>"
```

### 5. Create an incident

```bash
curl https://localhost:7281/api/incidents \
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

### 6. Retrieve the incident and note the ETag

```bash
curl -i https://localhost:7281/api/incidents/$INCIDENT_ID
```

The response headers will include an `ETag`, for example:

```http
ETag: W/"1"
```

Copy that value and export it:

```bash
export ETAG='W/"1"'
```

### 7. Execute a protected command with `If-Match`

Example: assign an engineer

```bash
curl https://localhost:7281/api/incidents/$INCIDENT_ID/assign-engineer \
  --request POST \
  --header 'Content-Type: application/json' \
  --header "Authorization: Bearer $JWT_TOKEN" \
  --header "If-Match: $ETAG" \
  --data '{
    "engineerId": 1
  }'
```

### 8. Refresh the ETag before the next command

After a successful command, retrieve the incident again to get the latest `ETag` before issuing another state-changing request:

```bash
curl -i https://localhost:7281/api/incidents/$INCIDENT_ID
```

Then update the `ETAG` variable with the new value before sending the next command.

An updated `ETag` may also be returned in the response headers of a successful command.

### 9. Further example commands

To move an incident to `Resolved`, it must first be in `InProgress`.

#### Start progress

```bash
curl https://localhost:7281/api/incidents/$INCIDENT_ID/start-progress \
  --request POST \
  --header 'Content-Type: application/json' \
  --header "Authorization: Bearer $JWT_TOKEN" \
  --header "If-Match: $ETAG"
```

After a successful command, retrieve the incident again and update the `ETAG` value before issuing the next state-changing request.

#### Resolve

```bash
curl https://localhost:7281/api/incidents/$INCIDENT_ID/resolve \
  --request POST \
  --header 'Content-Type: application/json' \
  --header "Authorization: Bearer $JWT_TOKEN" \
  --header "If-Match: $ETAG" \
  --data '{
    "resolutionSummary": "Incident resolution description"
  }'
```

### 10. Stop the application and database

Stop the API with `Ctrl + C`.

Stop the database:

```bash
docker compose down
```

---

## Future improvements

Possible next steps:

- return structured `ProblemDetails` bodies for command failures
- add filtering, sorting, and pagination for incident queries
- replace hardcoded/in-memory users with persistent identity storage
- add refresh tokens and more complete auth flows
- add containerised local setup for API + database
- expand documentation with request/response examples
- add CI workflow for build and test execution

---

## Purpose

This repository is part of my transition into backend development, with a focus on C#/.NET, backend design, API behaviour, domain modelling, authentication, and production-relevant concerns such as concurrency and testing.
