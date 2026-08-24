# Common-Service Architecture

This document visualizes the strict 4-Tier Architecture of the Common-Service project, focusing on the restaurant analogy and the system context.

## 1. The Architecture (Visualized by Analogy)

* **The Customer (Frontend):** Makes the request via the browser.
* **The Waiter (ServiceHub API):** Takes the request and acts as the entry point.
* **The Plated Meal (DTO):** The safely formatted data sent back to the customer.
* **The Chef (Context/DAL):** The only layer allowed to talk to the database.
* **Raw Ingredients (DMO):** The exact shape of the data in the database.
* **The Pantry (MongoDB):** The storage database.

## 2. Microservices Architectural Principles

The system adheres to the following rules for microservices communication and ownership:

1. **Ownership:** Each person owns one GitHub repository and one Docker image.
2. **Endpoints:** Each ServiceHub exposes REST API endpoints.
3. **Data Isolation:** Each ServiceHub exclusively owns its MongoDB data.
4. **Shared Infrastructure:** Package core utilities (`Common.Library`) into versioned NuGet packages with wildcard floating versioning (`1.0.*`).
5. **Service Communication:** Domain services emit async sync events (`ProductSyncClient`) to `Common-Service`.
6. **HTTP Calls:** Use typed `HttpClient` classes for service-to-service calls.
7. **Deployment:** Use Docker Compose / Azure Container Apps for hosting.
8. **Authentication:** Uses JWT authentication with role claims (Admin/User).
9. **Routing:** Independent HTTPS ingress endpoints for each microservice.
10. **Layer Constraints:** Never make DTO, DMO, or Context projects perform HTTP calls directly.

---

## 3. Code Relationships (Folder Structure)

* `Common.Frontend` -> HTTP Requests -> `Common.ServiceHub`
* `Common.ServiceHub` -> Uses `Common.DTO` for responses/requests
* `Common.ServiceHub` -> Uses `Common.Context` for data persistence
* `Common.Context` -> Queries MongoDB & uses `Common.DMO` models
