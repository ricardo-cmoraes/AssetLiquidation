# Asset Liquidation System

## Description
The **Asset Liquidation System** is a backend application designed to simulate financial asset liquidation and track contract ledger occurrences. It manages asset balances and maintains a chronologically sorted audit trail of financial events (such as payments and refunds).

## Instructions

### Prerequisites
* **.NET 8 SDK**
* **Docker Desktop** (with Docker Compose enabled)

### Setup & Execution

1. **Spin up Infrastructure (PostgreSQL and RabbitMQ)**
   ```bash
   docker-compose up -d
   ```
2. Apply Database Migrations (if not auto-applied on startup)
    ```bash
    dotnet ef database update --project src/AssetLiquidation.Core/AssetLiquidation.Core.csproj --startup-project src/AssetLiquidation.Api/AssetLiquidation.Api.csproj
    ```
3. **Run Application**

    In two different terminals, run the applications
    
    Terminal 1: To run the Api
   ```bash
    dotnet run --project src/AssetLiquidation.Api/AssetLiquidation.Api.csproj
    ```
    Terminal 2: To run the Worker
   ```bash
    dotnet run --project src/AssetLiquidation.Worker/AssetLiquidation.Wroker.csproj
    ```
4. **Access API Documentation**
    Open http://localhost:5000/swagger (or the configured local port) in your browser.
5. **Access RabbitMQ UI**
    
    Open the docker Desktop and look for the link to the RabbitMQ UI. You will see the same port that you configured in your docker-compose.yml

    ```bash
    rabbitmq:
        image: rabbitmq:3-management-alpine
        container_name: asset_rabbitmq
        ports:
            - "5672:5672"
            - "15672:15672" #port
        environment:
            RABBITMQ_DEFAULT_USER: guest
            RABBITMQ_DEFAULT_PASS: guest
    ```

    or put:
    ```bash
        http://localhost:{port}/
    ```
### API Endpoints

#### AssetController
* POST /api/asset — Create a new contract with initial balance.
* GET /api/asset/{assetId} — Fetch asset status and current balance by contract ID.
#### OccurrenceController
* POST /api/occurrence — Process a new financial occurrence (1: Payment, 2: Refund).
* GET /api/occurrence/{assetId} — Retrieve the full chronologically sorted audit ledger for a contract.

---

### Example Payloads & Usage

1. Create an Asset (`POST /api/asset`)

    ```json
    {
        "assetId": "20261004001",
        "initialAmount": 150000.00
    }

2. Process a Payment (POST /api/occurrence)

    ```json
    {
        "assetContractId": "20261004001",
        "type": 1,
        "amount": 34067.98
    }
    //Note on type: 1 = Payment, 2 = Refund

3. Fetch Contract Audit Ledger (GET /api/occurrence/20261004001)

    ```json
    [
        {
            "id": "61e55752-299a-4634-9bb3-1fbd5f1ca52a",
            "assetId": "20261004001",
            "type": 2,
            "amount": 500.98,
            "status": 2,
            "createdAt": "2026-10-05T00:48:28.549412Z",
            "reason": null
        },
        {
            "id": "238b0b9c-7760-4b93-bf44-5aa99248d761",
            "assetId": "20261004001",
            "type": 1,
            "amount": 34067.98,
            "status": 2,
            "createdAt": "2026-10-05T00:48:12.390102Z",
            "reason": null
        }
    ]
## Architecture

#### Following Clean Architecture principles and clear separation of responsibilities, the solution is structured as follows:
* AssetLiquidation.slnx: Solution container.
* AssetLiquidation.Core (Domain & Data Layer): Pure business logic, Rich Domain Entities (Asset, Occurrence), Enums, and LiquidateDbContext mappings.
* AssetLiquidation.Api (Presentation Layer): RESTful Controllers, Request DTOs, Swagger documentation, and Dependency Injection setup.
* AsserLiquidation.Worker () : Background Service, Consumers, and Connection with RabbitMq.
* Isolated Infrastructure (Docker & PostgreSQL 16):
    * docker-compose.yml: Configures PostgreSQL 16 Alpine container listening on port 5432.
    * Persistent Volume (postgres_data): Ensures data durability across container restart
    * docker-compose.yml: Configures RabbitMQ container listening on port 5672.

## Solution Structure (Project Overview)
    AssetLiquidation/
    ├── src/
    │   ├── AssetLiquidation.Core/      # Domain Entities, Enums, DbContext & Migrations
    │   ├── AssetLiquidation.Api/       # Controllers, DTOs & Swagger Configuration
    │   └── AssetLiquidation.Worker/    # Consumers, Background Service
    ├── docker-compose.yml              # PostgreSQL and RabbitMq Service & Volume Definition
    └── AssetLiquidation.slnx           # Solution File


## Next Steps
* Configurar GitHub Actions (.github/workflows/ci.yml)
* Add unit and integration test coverage (xUnit / FluentAssertions).
* Create a Dashboard to report real-time status and metrics of Assets.
* Configurar a publicação automática da API e da infraestrutura (PostgreSQL/RabbitMQ) em ambiente Cloud (Azure Container Apps / AWS ECS).
* [React with ASPNET](https://learn.microsoft.com/pt-br/visualstudio/javascript/tutorial-asp-net-core-with-react?view=visualstudio)

## Resources
* **Framework & Core:** [.NET 8 Web API & C# 12 Standards](https://learn.microsoft.com/dotnet/)
* **ORM & Database:** [Entity Framework Core (Code-First)](https://learn.microsoft.com/ef/core/) with [Npgsql PostgreSQL Driver](https://www.npgsql.org/efcore/)
* **Containerization:** [PostgreSQL 16 Alpine Docker Image](https://hub.docker.com/_/postgres)
* **Architecture References:** [Domain-Driven Design (Eric Evans)](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/) & [Clean Architecture (Robert C. Martin)](https://www.informit.com/store/clean-architecture-a-craftsmans-guide-to-software-9780134494166)
* [Tests](https://learn.microsoft.com/en-us/dotnet/core/testing/?source=docs)