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
| **Farmer**          | Register crop applications, manage crop seasons, submit batches |
| **Business Manager**| Plan procurement, manage contracts, confirm registrations     |
| **Business Staff**  | Handle warehouse inbound/outbound, oversee stock levels       |
| **Expert**          | Evaluate crop progress and processing batches                 |
| **Delivery Staff**  | Update shipment delivery status                               |
| **Admin**           | Verify accounts and monitor platform usage                    |

---

## 🔁 Business Flows Implemented (Activity Modules)

1. **Procurement Planning & Cultivation Registration**  
   → Business creates seasonal procurement plans. Farmers apply for participation and commit production.  
   → System tracks registration details and confirmed farming commitments.

2. **Crop Season Monitoring & Expert Consultation**  
   → Farmers update crop season progress and report issues. Experts provide technical diagnosis and support.

3. **Post-Harvest Coffee Processing**  
   → Farmers log processing batches (e.g., dry, wet method). Quality and issues are reviewed by experts.

4. **Green Coffee Warehouse & Inventory Management**  
   → Businesses approve batches for storage, manage warehouse stock, and initiate outbound processes.

5. **Contract-Based B2B Sales & Delivery Tracking**  
   → Businesses list products, create B2B sales contracts, assign delivery staff. Shipment progress is tracked.

---

> 🔎 *Activity Diagrams, Use Cases, and ERDs available in `/docs/diagrams/` folder*

---

## 🏗️ Repository Purpose

This repository contains the **backend** implementation of the system, including:
- API controllers  
- Business logic layers  
- Entity models  
- Database schema and migrations  

> 🎨 Frontend (React) and potential mobile interface are handled in a separate repository.

---