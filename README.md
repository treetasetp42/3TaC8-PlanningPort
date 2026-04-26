# InvestPlanner API - Backend

[English](#english) | [ภาษาไทย](#ภาษาไทย)

---

## English

InvestPlanner API is the backend service for the InvestPlanner system. It handles core logic, user management, and integration with external financial APIs. This project serves as a practice for building secure and scalable RESTful APIs using .NET 8.

🔗 **Frontend Repository:** [https://github.com/treetasetp42/PlanningPort-FrontEnd](https://github.com/treetasetp42/PlanningPort-FrontEnd)

### Tech Stack
- **Framework:** .NET 8 (ASP.NET Core Web API)
- **ORM:** Entity Framework Core 8
- **Database:** Microsoft SQL Server
- **Authentication:** JWT (JSON Web Tokens) & Google OAuth validation
- **External APIs:** Finnhub (Stock price data)
- **Documentation:** Swagger / OpenAPI

### Key Features
- **Auth & Roles:** Secure login with JWT and role-based access (Admin/User).
- **Google OAuth:** Validates external Google access tokens.
- **Security:** Password hashing via BCrypt and email-based password reset.
- **Data Seeding:** Automatic admin account and permission setup on database update.
- **Cloud Ready:** Optimized for Azure App Service deployment.

### Local Setup
1. **Clone the repo**
   ```bash
   git clone https://github.com/treetasetp42/3TaC8-PlanningPort.git
   cd 3TaC8-PlanningPort
   ```
2. **Environment Variables**
   Set these in `appsettings.Development.json` or .NET User Secrets:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=InvestPlannerDb;Trusted_Connection=True;"
     },
     "Jwt": { "Key": "your_32_char_secret_key" },
     "Finnhub": { "ApiKey": "your_key" },
     "SmtpSettings": { "Password": "app_password" }
   }
   ```
3. **Database Migration**
   ```bash
   dotnet ef database update
   ```
4. **Run**
   ```bash
   dotnet run
   ```

---

## ภาษาไทย

โปรเจกต์นี้เป็นส่วน Backend สำหรับระบบ InvestPlanner สร้างขึ้นมาเพื่อเป็นโปรเจกต์ฝึกหัดการทำ RESTful API แนวคิดคือทำระบบหลังบ้านเพื่อจัดการลอจิกต่างๆ เช่น ข้อมูลผู้ใช้, พอร์ตลงทุน และการเชื่อมต่อกับ API ภายนอก

### เครื่องมือที่ใช้
- **Framework:** .NET 8 (ASP.NET Core Web API)
- **ORM:** Entity Framework Core 8
- **Database:** Microsoft SQL Server
- **Authentication:** ระบบล็อกอินด้วย JWT และตรวจเช็ก Google OAuth
- **External APIs:** Finnhub (ดึงราคาหุ้น)
- **Documentation:** Swagger สำหรับเทสต์ API

### ฟีเจอร์หลัก
- **Auth & Roles:** ล็อกอินและออก Token (JWT) แบ่งสิทธิ์ผู้ใช้และแอดมิน
- **Google OAuth:** เชื่อมต่อและตรวจสอบสิทธิ์ผ่าน Google
- **Security:** เข้ารหัสผ่านด้วย BCrypt และระบบรีเซ็ตรหัสผ่านทางอีเมล
- **Data Seeding:** สร้างบัญชีแอดมินและข้อมูลเริ่มต้นให้อัตโนมัติ

### วิธีรันโปรเจกต์
1. **โหลดโปรเจกต์:** `git clone https://github.com/treetasetp42/3TaC8-PlanningPort.git`
2. **ตั้งค่า Secrets:** ตั้งค่า Connection String, JWT Key และ API Key ใน User Secrets
3. **ฐานข้อมูล:** รัน `dotnet ef database update`
4. **รัน:** `dotnet run`

---

## 📜 Changelog
Detailed history of changes can be found in [CHANGELOG.md](./CHANGELOG.md).

---

**Note:** This project was developed using AI-assisted tools to accelerate coding and boilerplate generation. The core architecture, system logic, database design, and cloud deployment were manually structured and managed.