# 🌱 DakLakCoffeeSupplyChain_BE

**Backend Service for Dak Lak Coffee Supply Chain Management and Distribution Platform**  
📅 **Duration:** May 2025 – August 2025  
🎓 **Capstone Project** – FPT University | Software Engineering  

## 🧑‍💻 Team Members

- Lê Hoàng Phúc – SE173083 *(Project Lead)*
- Nguyễn Nhật Minh – SE161013  
- Lê Hoàng Thiên Vũ – SE160593  
- Phạm Huỳnh Xuân Đăng – SE161782  
- Phạm Trường Nam – SE150442  

---

## ⚙️ Tech Stack

| Layer        | Technology              |
|--------------|--------------------------|
| Language     | C#                       |
| Backend      | ASP.NET Core             |
| Database     | SQL Server               |
| API Format   | RESTful APIs (JSON)      |
| Auth         | JWT, Role-based Access   |
| ORM          | Entity Framework Core    |
| Docs & Test  | Swagger, xUnit (optional)|

---

## 🎭 Actors

| Role                | Description                                                  |
|---------------------|--------------------------------------------------------------|
| **Farmer**          | Register crops, update seasons, submit harvests              |
| **Business Manager**| Plan procurement, manage inventory, products, confirm orders |
| **Trader**          | Search and place B2B coffee orders                           |
| **Delivery Staff**  | Update order delivery status (picked up, delivered)          |
| **Expert**          | Analyze crop issues and share advice                         |
| **Admin**           | Approve users/products, view system metrics                  |

---

## 🔁 Business Flows Implemented (Activity Modules)

1. **Procurement Planning & Cultivation Registration**  
   → Business creates seasonal coffee demand plans, approves farmer registration.

2. **Crop Season Management & Expert Consultation**  
   → Farmer tracks crop progress, experts provide technical guidance.

3. **Post-Harvest Coffee Processing**  
   → Farmers submit batches for processing, issues tracked, quality rated.

4. **Coffee Warehouse Management**  
   → Business receives inventory, generates receipts, tracks stock levels.

5. **Selling Products & B2B Order Delivery Tracking**  
   → Business lists products, handles B2B orders, delivery staff updates progress.

> 🔎 *Activity Diagrams and ERDs available in `/docs/diagrams/`*

---

## 🏗️ Repository Purpose

This repository contains the **backend** implementation of the system, including:
- API controllers  
- Business logic layers  
- Entity models  
- Database schema and migrations  

> Frontend and mobile clients are handled in separate repositories.
