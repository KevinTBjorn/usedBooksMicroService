# Georgia Tech Library Marketplace – System Architecture (4-Service Model)

## Overview
Denne arkitekturdokumentation beskriver den tekniske struktur for Georgia Tech Library Marketplace. Fokus er på integration via messaging, skalering, microservices og docker–baseret drift.

Systemet består af følgende hovedkomponenter:

- **API Gateway**
- **AddBookService**
- **WarehouseService**
- **SearchService**
- **OrderService**
- **RabbitMQ (Message Bus)**
- **Postgres (Warehouse DB)**
- **Redis (Search Read-Model DB)**

---

# 1. High-Level Architecture Diagram (System Context)

                        ┌──────────────────────────────┐
                        │          USERS / UI          │
                        │ (Browser, Mobile, Frontend)  │
                        └───────────────┬──────────────┘
                                        │ HTTP
                                        ▼
                             ┌────────────────────────┐
                             │      API GATEWAY       │
                             │  (Routing / Facade)    │
                             └───────┬───────┬────────┘
                                     │       │ REST
                         ┌───────────┘       └───────────┐
                         │                                │
                         ▼                                ▼
           ┌──────────────────────┐            ┌──────────────────────┐
           │   AddBookService     │            │    OrderService      │
           │  /addbook (REST)     │            │   /order  (REST)     │
           │ Publishes events     │            │ Publishes events     │
           └──────────┬───────────┘            └──────────┬──────────┘
                      │ Event: BookAddedEvent             │ Event: OrderCompletedEvent
                      ▼                                    ▼
             ┌──────────────────────────────────────────────────────────┐
             │                        RabbitMQ                           │
             │         (Topic Exchange · Queues · Routing Keys)         │
             └──────┬──────────────────────┬────────────────────────────┘
                    │                      │
     BookAddedEvent │                      │ StockUpdatedEvent
                    ▼                      ▼
      ┌──────────────────────┐     ┌──────────────────────────┐
      │  WarehouseService    │     │      SearchService       │
      │  (Inventory Logic)   │     │ (Read Model / Queries)   │
      │  Listens for events  │     │  Listens for events      │
      └───────────┬─────────┘     └───────────┬──────────────┘
                  │                           │
                  │ SQL                       │ Redis Cache
                  ▼                           ▼
      ┌──────────────────────┐     ┌──────────────────────────┐
      │      Postgres DB     │     │         Redis DB         │
      │  (Stock & Book Data) │     │   (Search Index/Cache)   │
      └──────────────────────┘     └──────────────────────────┘

---

# 2. Services Overview

## 2.1 AddBookService
- Endpoint: `POST /addbook`
- Funktion: Modtager bogdata fra brugere
- Producerer event: **BookAddedEvent**
- Gemmer ikke data selv
- Mønstre:  
  - Event Message  
  - Publish/Subscribe  

## 2.2 WarehouseService
- Lytter på:
  - `BookAddedEvent`
  - `OrderCompletedEvent`
- Producerer:  
  - `StockUpdatedEvent`
- Database: **Postgres**
- Ansvar:
  - Lagerstyring
  - Reducere stock
- Mønstre:
  - Message Queue
  - Competing Consumers
  - Eventual Consistency

## 2.3 SearchService
- Lytter på: `StockUpdatedEvent`
- Database: **Redis**
- Endpoint: `GET /search?title=xxx`
- Mønstre:
  - CQRS (Read Model)
  - Publish/Subscribe
  - Content-Based Routing

## 2.4 OrderService
- Endpoint: `POST /order`
- Producerer: `OrderCompletedEvent`
- Tjekker stock via Warehouse (REST eller cache)

---

# 3. Message Bus (RabbitMQ)

## 3.1 Exchange
- `gtlib.exchange` (type: topic)

## 3.2 Routing Keys
- `book.added`
- `stock.updated`
- `order.completed`

## 3.3 Queues

| Queue Name | Event Type | Consumed By |
|------------|------------|-------------|
| `warehouse.bookadded.queue` | BookAddedEvent | WarehouseService |
| `search.stockupdated.queue` | StockUpdatedEvent | SearchService |
| `warehouse.ordercompleted.queue` | OrderCompletedEvent | WarehouseService |

---

# 4. Databases

## Warehouse Database (Postgres)
- Tabel: Books
- Tabel: Stock

## Search Database (Redis)
- Key-value struktur for lynhurtig opslag

---

# 5. Event Flows

## 5.1 Add Book Flow
- User → API Gateway → AddBookService  
- AddBookService → BookAddedEvent → RabbitMQ  
- WarehouseService modtager → opdaterer DB → sender StockUpdatedEvent  
- SearchService modtager → opdaterer read-model

## 5.2 Order Flow
- User → API Gateway → OrderService  
- OrderService sender OrderCompletedEvent → RabbitMQ  
- WarehouseService reducerer stock → sender StockUpdatedEvent  
- SearchService opdaterer read-model

---

# 6. Docker Architecture

## Containers:

| Container | Role |
|----------|------|
| rabbitmq | Message bus |
| postgres | Warehouse database |
| redis | Search database |
| apigateway | Public entrypoint |
| addbookservice | Book ingest |
| warehouseservice | Stock management |
| searchservice | Search read-model |
| orderservice | Order handling |

Alle services kører på netværket: **gtlib-net**

---

# 7. Integration Patterns (EIP)

Systemet bruger følgende patterns fra Enterprise Integration Patterns:

- **Event Message** (BookAddedEvent, StockUpdatedEvent, OrderCompletedEvent)
- **Publish/Subscribe** (via topic exchange)
- **Message Queue** (én kø pr. consumer)
- **Message Channel**
- **Competing Consumers** (Warehouse kan skaleres horisontalt)
- **Content-Based Router** (RabbitMQ routing keys)
- **Eventual Consistency**
- **CQRS (Read Model)**

---

# 8. Why This Architecture?

Denne arkitektur understøtter:

- Løs kobling
- Skalering (horizontal)
- Decentral dataejerskab
- Eventual consistency
- Robusthed (beskeder går ikke tabt)
- Hurtig søgning (<1 sekund)
- Peak-load håndtering ved semesterstart

---

# 9. Future Extensions
- NotificationService (lyt på events)
- PricingService
- RecommendationEngine
- More advanced search (Elasticsearch)

---

# 10. Summary
Dette system leverer en fuldt event-drevet microservice-arkitektur, understøttet af RabbitMQ, Postgres, Redis og Docker.  
Designet følger moderne integrationsprincipper og sikrer både skalerbarhed, fejltolerance og fleksibilitet.

