# InvestPlanner API - Backend

[English](#english) | [ภาษาไทย](#ภาษาไทย)

---

## English

InvestPlanner API is the backend service for the InvestPlanner system. It handles core logic, user management, and integration with external financial APIs. This project serves as a practice for building secure and scalable RESTful APIs using .NET 10.

🔗 **Frontend Repository:** [https://github.com/treetasetp42/PlanningPort-FrontEnd](https://github.com/treetasetp42/PlanningPort-FrontEnd)

### Tech Stack
- **Framework:** .NET 10 (ASP.NET Core Web API) [LTS]
- **ORM:** Entity Framework Core 10
- **Database:** Microsoft SQL Server
- **Authentication:** JWT (JSON Web Tokens) & Google OAuth validation
- **External APIs:** Finnhub (Stock price data)
- **Documentation:** Swagger / OpenAPI

### Key Features
- **AI Chatbot Endpoint:** Direct Google Gemini 2.5 Flash API integration for high-performance natural language generation.
- **Page-Aware System Instructions:** System prompt automatically consumes frontend routes to contextualize questions according to the active page (e.g. watchlist, portfolio, settings).
- **Polite Female Tone Guardrail:** System prompt enforces a strict polite female Thai persona (forbids male/mixed suffix styles).
- **Safe external-service errors:** Connects through secure configuration and returns generic upstream-service failures without exposing key fragments or provider internals.
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
     "Jwt": { "Key": "replace_with_a_random_secret_of_at_least_32_characters" },
     "Gemini": { "ApiKey": "your_key" },
     "Finnhub": { "ApiKey": "your_key" },
     "SmtpSettings": { "Password": "app_password" },
     "FrontendUrl": "http://localhost:5173"
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

### Production security requirements

- Supply secrets through environment variables or a secret manager, never committed JSON. ASP.NET Core environment names use double underscores, for example `Jwt__Key`, `ConnectionStrings__DefaultConnection`, `Gemini__ApiKey`, `Finnhub__ApiKey`, `SmtpSettings__Password`, and `FrontendUrl`.
- The application refuses to start when `Jwt__Key` is missing, weak, or still contains the example fallback text. Use a newly generated random value; do not reuse a value that has appeared in Git history.
- No account is promoted automatically. After registering the intended owner, assign the `Admin` role out-of-band through a protected database administration session.
- Application rate limits are intentionally conservative and stored in memory. Keep an additional Nginx/Cloudflare request limit and provider-side Gemini/Finnhub quota for protection across restarts or multiple containers.
- Existing refresh tokens created before this security update are invalid because tokens are now stored as hashes in the database.

---

## ภาษาไทย

โปรเจกต์นี้เป็นส่วน Backend สำหรับระบบ InvestPlanner สร้างขึ้นมาเพื่อเป็นโปรเจกต์ฝึกหัดการทำ RESTful API แนวคิดคือทำระบบหลังบ้านเพื่อจัดการลอจิกต่างๆ เช่น ข้อมูลผู้ใช้, พอร์ตลงทุน และการเชื่อมต่อกับ API ภายนอก โดยใช้ .NET 10

### เครื่องมือที่ใช้
- **Framework:** .NET 10 (ASP.NET Core Web API) [LTS]
- **ORM:** Entity Framework Core 10
- **Database:** Microsoft SQL Server
- **Authentication:** ระบบล็อกอินด้วย JWT และตรวจเช็ก Google OAuth
- **External APIs:** Finnhub (ดึงราคาหุ้น)
- **Documentation:** Swagger สำหรับเทสต์ API

### ฟีเจอร์หลัก
- **ระบบวิเคราะห์คำถาม AI (Chatbot Endpoint):** เชื่อมต่อบริการของ Google Gemini 2.5 Flash ตอบโต้ผู้ใช้ได้อย่างรวดเร็วและแม่นยำกว่ารุ่น Lite
- **ระบบแนะนำตามบริบทหน้าเว็บ (Page Path Context):** ประมวลผลจากหน้าที่ผู้ใช้อยู่ ณ ปัจจุบัน (เช่น หน้าเทรด หรือพอร์ตการลงทุน) เพื่ออธิบายฟังก์ชัน ปุ่ม และเมนูในหน้านั้นได้อย่างสมบูรณ์แบบ
- **ระบบควบคุมน้ำเสียงภาษาไทย (Polite Female Persona):** ตีกรอบคำสั่ง System Instructions บังคับให้น้ำเสียงของบอทเป็นผู้หญิงที่สุภาพและลงท้ายด้วยคำว่า **"ค่ะ"** และ **"นะคะ"** เสมอ (ห้ามมีคำหางเสียงผู้ชายหรือหางเสียงที่สับสนปะปน)
- **การจัดการข้อผิดพลาดอย่างปลอดภัย:** รองรับ Config ทั้ง `Gemini:ApiKey` หรือ Environment Variable `GEMINI_API_KEY` และไม่ส่งรายละเอียดภายในหรือส่วนหนึ่งของ API key กลับไปยังผู้ใช้
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
