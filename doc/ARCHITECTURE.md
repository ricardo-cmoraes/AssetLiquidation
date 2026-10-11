# System Architecture - Asset Liquidation Platform

This document details the software architecture, design patterns, and persistence flow of the asset liquidation platform.

---

## 1. Context Overview (C4 Model - Level 1)

The diagram below illustrates the general system context, the actors interacting with it, and the underlying infrastructure dependencies.

```mermaid
C4Context
    title Context Diagram - Asset Liquidation Platform

    Person(client, "Client / Consumer System", "Corporate applications and securitization\nsystems requesting asset liquidation.")
    System(asset_system, "Asset Liquidation Platform", "Manages the asset life cycle,\nliquidations, and audit ledger.")
    SystemDb(postgres, "PostgreSQL", "Relational persistence for\nassets and occurrences.")

    Rel(client, asset_system, "Consumes REST endpoints", "HTTP/JSON")
    Rel(asset_system, postgres, "Reads and writes assets/occurrences", "EF Core / TCP 5432")
```

---

## 2. Container Architecture (C4 Model - Level 2)

Details the layer separation, domain boundaries, and data persistence flow.

```mermaid
graph TD
    subgraph ClientLayer [API Layer]
        API[AssetLiquidation.Api - Web API]
    end

    subgraph CoreDomain [Domain & Persistence]
        CORE[AssetLiquidation.Core - DDD Domain]
        PG[(PostgreSQL Database)]
    end

    API -->|1. Processes & Validates Request| CORE
    CORE -->|2. Saves Asset & Occurrence| PG
```

---

## 3. Sequence Diagram - Synchronous Liquidation Flow

Demonstrates the execution sequence over time when a client triggers a payment or refund event:

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client (HTTP)
    participant API as Web API
    participant DB as PostgreSQL

    Client->>API: POST /api/asset/payment (AssetId, Amount)
    API->>DB: Fetches Asset by ContractId (11 digits)
    API->>API: Executes asset.ProcessPayment(amount)
    API->>DB: Saves updated Asset + Occurrence
    API-->>Client: 200 OK / 201 Created (Updated Asset & Occurrence payload)
```

---

## 4. Architecture Decision Records (ADRs)

* **DDD (Domain-Driven Design):** Rich domain entities with encapsulation and explicit business rule enforcement (`Asset` and `Occurrence`).
* **Primary Key vs. Business Key:**
  * `Guid` as Primary Key (Surrogate Key) for relational integrity and foreign key indexing.
  * `string` of 11 numeric digits as Business Key for operational contract lookups.
* **Audit Trail:**
  * Synchronous generation of `Occurrence` entities linked via Foreign Key to track all financial operations and lifecycle changes.