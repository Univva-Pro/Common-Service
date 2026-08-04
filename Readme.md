# 🏗️ The Comman-Service Project Guide

Think of your entire project as a highly organized Restaurant.

---

## The 4-Tier Architecture

When you ask for a "strict 4-tier architecture", Every folder has a very strict job, and they are only allowed to talk to specific people.

### 1. `Comman.DMO` (Data Model Objects) 🍅
**Analogy:** The Raw Ingredients.
- **What it does:** This folder just holds simple definitions of what your data looks like *exactly* as it is stored in the database.
- **The Code:** `CommanItem.cs` and `User.cs`. They list properties like `Name`, `Category`, `StockQuantity`, and `Price`.
- **Rules:** It doesn't know about any other folders. It just exists to define shapes.

### 2. `Comman.Context` (Data Access Layer) 👨‍🍳
**Analogy:** The Chef in the Kitchen.
- **What it does:** This is the ONLY folder that is allowed to talk to the Database (MongoDB). 
- **The Code:** `CommanRepository.cs` and `UserRepository.cs`. 
- **Rules:** If anyone wants to save a new item or look up a user, they *must* ask `Comman.Context`. The Context reaches into the database, grabs the raw ingredients (`DMO`), and passes them back.

### 3. `Comman.DTO` (Data Transfer Objects) 🍽️
**Analogy:** The Plated Meal.
- **What it does:** When a customer orders food, you don't bring them a raw egg and flour. You bring them a baked cake. The DTO defines what the data looks like *after* we prepare it for the outside world.
- **The Code:** `CommanItemResponse.cs`.
- **Rules:** If an Admin asks for data, we give them a plate with everything (including stock numbers). If a normal User asks, we give them a restricted view.

### 4. `Comman.ServiceHub` (The Core Web API) 🤵
**Analogy:** The Waiter & The Front Doors.
- **What it does:** This is the actual application that runs. It listens for requests from the internet (like someone logging in from the website).
- **The Code:** `Program.cs` and web application configuration.
- **Rules:** The Waiter (`Program.cs`) gets a request from the user. The Waiter runs back to the Chef (`Context`), asks for the raw data (`DMO`), plates it nicely into a safe format (`DTO`), and carries it back out to the customer.

---

## How it All Connects (The Flow)

1. **The Website (`Comman.Frontend`)**
   - The Frontend sends an HTTP request to the Waiter (`ServiceHub`).
2. **The Waiter (`Comman.ServiceHub/Program.cs`)**
   - The Waiter receives the request at the endpoint.
   - The Waiter goes to the Chef (`CommanRepository` in `Context`).
3. **The Chef (`Comman.Context/CommanRepository.cs`)**
   - The Chef connects to MongoDB, searches the collection, and hands the raw ingredient (`CommanItem DMO`) back.
4. **The Waiter (`Comman.ServiceHub/Program.cs`)**
   - The Waiter converts DMOs into DTO responses and sends them back over HTTP.
