# Comman-Service Architecture

This document visualizes the strict 4-Tier Architecture of the Comman-Service project, focusing on the restaurant analogy and the system context.

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
4. **DTO Distribution:** Publish each DTO class library as a versioned NuGet package.
5. **Service Communication:** ServiceHubs install other modules’ DTO packages when calling their APIs.
6. **HTTP Calls:** Use typed `HttpClient` classes for service-to-service calls.
7. **Deployment:** Create one integration Compose repository to run all modules.
8. **Authentication:** Use one shared JWT issuer.
9. **Routing:** Put one HTTPS gateway in front of the three APIs.
10. **Layer Constraints:** Never make DTO, DMO, or Context projects perform HTTP calls directly.

---

## 3. Code Relationships (Folder Structure)

* `Comman.Frontend` -> HTTP Requests -> `Comman.ServiceHub`
* `Comman.ServiceHub` -> Uses `Comman.DTO` for responses/requests
* `Comman.ServiceHub` -> Uses `Comman.Context` for data persistence
* `Comman.Context` -> Queries MongoDB & uses `Comman.DMO` models
