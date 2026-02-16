# 📦 OrderFlow

Minimal event-driven order processing system built with .NET 10, RabbitMQ, PostgreSQL, MongoDB and React.

This project demonstrates:

- Clean Architecture
- JWT Authentication
- Event-driven processing with RabbitMQ
- Worker Service
- Structured logging with MongoDB
- Dockerized full-stack environment
- Unit tests with xUnit, Moq, FluentAssertions and Bogus

---

# 🏗 Architecture

## High-level flow

1. User registers or logs in
2. API creates order (PostgreSQL)
3. API publishes `OrderCreated.v1` event (RabbitMQ)
4. Worker consumes event
5. Worker processes order and updates status
6. Logs are written to MongoDB

## Main components

- `OrderFlow.Api`
- `OrderFlow.Worker`
- `OrderFlow.Application`
- `OrderFlow.Infrastructure`
- `OrderFlow.Domain`
- `orderflow-web` (React frontend)

---

# 🧰 Tech Stack

## Backend
- .NET 10
- ASP.NET Core
- PostgreSQL
- RabbitMQ
- MongoDB
- Clean Architecture
- JWT Authentication

## Frontend
- React + Vite
- Fetch API
- Dark theme UI

## Testing
- xUnit
- Moq
- FluentAssertions
- Bogus

## Infrastructure
- Docker
- Docker Compose

---

# 🚀 Running the Project

## Requirements

- Docker
- Docker Compose

## Run Everything

From the root folder:

```bash
docker compose up -d --build
```

This starts:

- PostgreSQL
- MongoDB
- RabbitMQ (with management UI)
- API
- Worker

---

# 🌐 Access

## API
```
http://localhost:5000
```

## Swagger
```
http://localhost:5000/swagger
```

## RabbitMQ Management
```
http://localhost:15672
```

User: `orderflow`  
Password: `orderflow`

## Frontend

Open:

```
http://localhost:5173
```

---

# 🔐 Authentication

You can register directly from the frontend.

Or via Swagger:

```
POST /api/auth/register
POST /api/auth/login
```

JWT is required for protected endpoints.

---

# 📦 Order Flow

1. Create order via frontend or Swagger
2. API returns `202 Accepted`
3. Worker processes asynchronously
4. Order status transitions:
   - 1 = Pending
   - 2 = Processing
   - 3 = Completed
   - 4 = Failed

---

# 🧪 Running Tests

## Application Tests

From solution root:

```bash
dotnet test OrderFlow.Application.Tests
```

Covers:

- CreateOrderUseCase
- RegisterUserUseCase
- Validation rules
- Event publishing behavior

---

# 📊 Logging

MongoDB collections:

- `processing_logs`
- `event_history`

You can inspect logs using MongoDB Compass:

```
mongodb://localhost:27017
```

Database:

```
orderflow
```

---

# 📁 Project Structure

```
OrderFlow.Domain
OrderFlow.Application
OrderFlow.Infrastructure
OrderFlow.Api
OrderFlow.Worker
OrderFlow.Api.IntegrationTests
OrderFlow.Application.Tests
orderflow-web
docker-compose.yml
```

---

# 🎯 Design Goals

This project demonstrates:

- Event-driven architecture
- Asynchronous processing
- Clean separation of concerns
- Dockerized full-stack setup
- Testing with mocks and data generation
- JWT security

---

# 🔮 Possible Improvements

- Integration tests with Testcontainers
- CI pipeline
- OpenTelemetry tracing
- Rate limiting
- API versioning strategy
- Production-ready deployment
- Frontend improvements (state management, routing)

---

# 👨‍💻 Author

Gustavo Mariano  
Senior .NET Developer  
Brazil
