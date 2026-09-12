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
- **Abuse Protection:** ASP.NET Core rate limiting with endpoint-specific policies
- **External APIs:** Finnhub (Stock price data)
- **Documentation:** Swagger / OpenAPI

### Key Features
- **AI Chatbot Endpoint:** Direct Google Gemini 2.5 Flash API integration for high-performance natural language generation.
- **Page-Aware System Instructions:** System prompt automatically consumes frontend routes to contextualize questions according to the active page (e.g. watchlist, portfolio, settings).
- **Polite Female Tone Guardrail:** System prompt enforces a strict polite female Thai persona (forbids male/mixed suffix styles).
- **Safe external-service errors:** Connects through secure configuration and returns generic upstream-service failures without exposing key fragments or provider internals.
- **Auth & Roles:** Secure login with JWT and role-based access (Admin/User).
- **Google OAuth:** Validates external Google access tokens.
- **Security:** BCrypt password hashing, hashed refresh/reset tokens, strict JWT validation, ownership enforcement, bounded uploads, safe production errors, and layered request limits.
- **Data Seeding:** Automatic role and permission setup without creating or promoting a default administrator.
- **Cloud Ready:** Supports containerized deployment behind an HTTPS reverse proxy.

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
- The application refuses to start when `Jwt__Key` is missing, shorter than 32 characters, or contains the known `change_this` fallback text. Use a newly generated random value for production; do not use a public example value.
- No account is promoted automatically. After registering the intended owner, assign the `Admin` role out-of-band through a protected database administration session.
- Application rate limits are intentionally conservative and stored in memory. Keep an additional Nginx/Cloudflare request limit and provider-side Gemini/Finnhub quota for protection across restarts or multiple containers.
- Existing refresh tokens created before this security update are invalid because tokens are now stored as hashes in the database.
- Forward the real client IP and original scheme from the trusted reverse proxy. Do not expose the application or SQL Server ports directly to the public internet.
- Persist `/app/wwwroot/uploads` outside the API container and back up the SQL Server database before starting a release that may apply migrations.

### Default application limits

- Global API traffic: 120 requests per minute per client IP.
- Login and authentication flows: 10 requests per minute per client IP.
- Registration: 3 requests per hour per client IP.
- Password-reset requests: 3 requests per 15 minutes per client IP.
- Write operations: 30 requests per minute per client IP.
- Expensive external-service operations: 5 requests per minute per client IP.
- Profile uploads: 5 requests per 10 minutes, with a 2 MB request/upload ceiling.

These limits protect a single application instance. They reset when the process restarts and are not shared between replicas.

---

## ภาษาไทย

โปรเจกต์นี้เป็นส่วน Backend สำหรับระบบ InvestPlanner สร้างขึ้นมาเพื่อเป็นโปรเจกต์ฝึกหัดการทำ RESTful API แนวคิดคือทำระบบหลังบ้านเพื่อจัดการลอจิกต่างๆ เช่น ข้อมูลผู้ใช้, พอร์ตลงทุน และการเชื่อมต่อกับ API ภายนอก โดยใช้ .NET 10

### เครื่องมือที่ใช้
- **Framework:** .NET 10 (ASP.NET Core Web API) [LTS]
- **ORM:** Entity Framework Core 10
- **Database:** Microsoft SQL Server
- **Authentication:** ระบบล็อกอินด้วย JWT และตรวจเช็ก Google OAuth
- **Abuse Protection:** ระบบ Rate Limiting ของ ASP.NET Core แยกตามประเภท Endpoint
- **External APIs:** Finnhub (ดึงราคาหุ้น)
- **Documentation:** Swagger สำหรับเทสต์ API

### ฟีเจอร์หลัก
- **ระบบวิเคราะห์คำถาม AI (Chatbot Endpoint):** เชื่อมต่อบริการของ Google Gemini 2.5 Flash ตอบโต้ผู้ใช้ได้อย่างรวดเร็วและแม่นยำกว่ารุ่น Lite
- **ระบบแนะนำตามบริบทหน้าเว็บ (Page Path Context):** ประมวลผลจากหน้าที่ผู้ใช้อยู่ ณ ปัจจุบัน (เช่น หน้าเทรด หรือพอร์ตการลงทุน) เพื่ออธิบายฟังก์ชัน ปุ่ม และเมนูในหน้านั้นได้อย่างสมบูรณ์แบบ
- **ระบบควบคุมน้ำเสียงภาษาไทย (Polite Female Persona):** ตีกรอบคำสั่ง System Instructions บังคับให้น้ำเสียงของบอทเป็นผู้หญิงที่สุภาพและลงท้ายด้วยคำว่า **"ค่ะ"** และ **"นะคะ"** เสมอ (ห้ามมีคำหางเสียงผู้ชายหรือหางเสียงที่สับสนปะปน)
- **การจัดการข้อผิดพลาดอย่างปลอดภัย:** รองรับ Config ทั้ง `Gemini:ApiKey` หรือ Environment Variable `GEMINI_API_KEY` และไม่ส่งรายละเอียดภายในหรือส่วนหนึ่งของ API key กลับไปยังผู้ใช้
- **Auth & Roles:** ล็อกอินและออก Token (JWT) แบ่งสิทธิ์ผู้ใช้และแอดมิน
- **Google OAuth:** เชื่อมต่อและตรวจสอบสิทธิ์ผ่าน Google
- **Security:** เข้ารหัสผ่านด้วย BCrypt, เก็บ Refresh/Reset Token แบบ Hash, ตรวจ JWT อย่างเข้มงวด, ตรวจสอบเจ้าของข้อมูล, จำกัดไฟล์อัปโหลด และจำกัดจำนวน Request
- **Data Seeding:** สร้าง Role และ Permission เริ่มต้นโดยไม่สร้างหรือเลื่อนสิทธิ์บัญชีแอดมินอัตโนมัติ

### วิธีรันโปรเจกต์
1. **โหลดโปรเจกต์:** `git clone https://github.com/treetasetp42/3TaC8-PlanningPort.git`
2. **ตั้งค่า Secrets:** ตั้งค่า Connection String, JWT Key และ API Key ใน User Secrets
3. **ฐานข้อมูล:** รัน `dotnet ef database update`
4. **รัน:** `dotnet run`

### ข้อกำหนดความปลอดภัยสำหรับ Production

- เก็บ Secret ผ่าน Environment Variable หรือ Secret Manager เท่านั้น ห้าม Commit ค่า `Jwt__Key`, Connection String, API Key หรือรหัสผ่าน SMTP ลง Git
- ระบบจะไม่เริ่มทำงานหากไม่มี JWT signing key, มีความยาวน้อยกว่า 32 ตัวอักษร หรือยังมีข้อความตัวอย่าง `change_this` และควรสร้างค่าใหม่ที่ไม่ใช่ค่าตัวอย่างสาธารณะสำหรับ Production
- ระบบไม่สร้างบัญชี Admin อัตโนมัติ ต้องกำหนดสิทธิ์เจ้าของระบบผ่านช่องทางจัดการฐานข้อมูลที่ได้รับการป้องกัน
- ใช้ Nginx/Cloudflare จำกัด Request เพิ่มเติม เพราะ Rate Limit ภายในแอปอยู่ในหน่วยความจำและจะเริ่มใหม่เมื่อ Container Restart
- Reverse Proxy ต้องส่ง Client IP และ Scheme จริง และไม่ควรเปิดพอร์ต API หรือ SQL Server สู่อินเทอร์เน็ตโดยตรง
- ต้อง Mount `/app/wwwroot/uploads` เป็น Persistent Storage และสำรองฐานข้อมูลก่อนเริ่ม Release ที่อาจรัน Migration

### ค่า Rate Limit เริ่มต้น

- API ทั้งระบบ: 120 requests/นาที/IP
- Login และ Authentication: 10 requests/นาที/IP
- Registration: 3 requests/ชั่วโมง/IP
- Password Reset: 3 requests/15 นาที/IP
- การเขียนข้อมูล: 30 requests/นาที/IP
- Endpoint ที่เรียกบริการภายนอก: 5 requests/นาที/IP
- Upload โปรไฟล์: 5 requests/10 นาที และขนาดสูงสุด 2 MB

---

## 📜 Changelog
Detailed history of changes can be found in [CHANGELOG.md](./CHANGELOG.md).

---

**Note:** This project was developed using AI-assisted tools to accelerate coding and boilerplate generation. The core architecture, system logic, database design, and cloud deployment were manually structured and managed.
