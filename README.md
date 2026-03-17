# IncidentManagementApi

Backend-focused ASP.NET Core Web API for managing incidents.

This project demonstrates practical backend design in C#/.NET using a layered structure, domain modelling, explicit incident state handling, optimistic concurrency, ETag support, and API-level testing.

## Overview

The API is designed around an incident management domain rather than simple CRUD. The goal is to model real backend concerns such as state, concurrency, response contracts, and separation of responsibilities across layers.

The solution is organised into separate projects for:

- **API** – HTTP endpoints, middleware, and API concerns
- **Application** – DTOs, interfaces, services, and application logic
- **Domain** – core entities and enums
- **Infrastructure** – persistence, EF Core configuration, and database concerns

## Tech stack

- C#
- .NET / ASP.NET Core Web API
- Entity Framework Core
- SQL database / EF persistence layer
- xUnit integration/API tests

## Architecture

Request flow:

`HTTP request -> Controller -> Application service/query layer -> DbContext -> Database`

### Structure highlights

- **Controllers** handle HTTP concerns and response behaviour
- **Application layer** contains DTOs, service logic, and abstractions
- **Domain layer** models incidents and domain concepts such as severity and status
- **Infrastructure layer** handles persistence and EF Core configuration

## Features

This project demonstrates:

- layered backend design across API, Application, Domain, and Infrastructure
- incident domain modelling with explicit status and severity concepts
- optimistic concurrency support
- ETag generation and conditional GET handling
- API testing with a dedicated test project
- DTO-based response shaping

## Example implemented behaviour

### Get incident by id
`GET /api/incidents/{id}`

The controller:
- returns `404 Not Found` when the incident does not exist
- returns `200 OK` with an incident response DTO when found
- generates an ETag from the incident row version
- supports conditional requests using `If-None-Match`
- returns `304 Not Modified` when appropriate

## Project structure

```text
IncidentManagementApi.sln
├── src/
│   ├── API/
│   │   ├── Controllers/
│   │   ├── Http/
│   │   ├── Middleware/
│   │   └── Program.cs
│   ├── Application/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   └── Validators/
│   ├── Domain/
│   │   ├── Entities/
│   │   └── Enums/
│   └── Infrastructure/
│       ├── Configurations/
│       ├── Migrations/
│       └── Persistence/
└── tests/
    └── API.Tests/
````

## What this project demonstrates

This project helped me practise and understand:

* designing a backend beyond simple CRUD
* structuring a multi-project solution with clear separation of concerns
* working with domain concepts such as incident severity, status, and transitions
* handling optimistic concurrency in a web API
* using ETags and conditional requests for efficient API responses
* writing API-level tests around endpoint behaviour

## Next improvements

Areas to extend next:

* add more incident commands and update workflows
* document allowed state transitions clearly
* add filtering, sorting, and pagination
* expand validation and error-handling middleware
* add more end-to-end tests for command scenarios
* document setup and local run instructions in more detail

## Purpose

This repository is part of my transition into backend development, with a focus on C#/.NET, backend design, API behaviour, and production-relevant concerns such as concurrency and caching.
