📈 3TaC8-PlanningPort Backend Setup Guide

คู่มือสำหรับการ Setup ระบบ Backend (ASP.NET Core Web API) และ Database (SQL Server) เมื่อมีการ Pull Code ไปรันที่เครื่องใหม่

🛠 Prerequisites (สิ่งที่ต้องติดตั้งในเครื่อง)
.NET 8.0 SDK หรือเวอร์ชันล่าสุด

Visual Studio 2022 (พร้อม Workload: ASP.NET and web development)

SQL Server Express หรือ Developer Edition

SQL Server Management Studio (SSMS)

🚀 Step-by-Step Setup
1. Restore Packages
หลังจาก Pull Code มาแล้ว ให้เปิด Solution (.sln) ใน Visual Studio จากนั้น:

คลิกขวาที่ Solution -> Restore NuGet Packages

Package หลักที่ใช้: Microsoft.EntityFrameworkCore.SqlServer, BCrypt.Net-Next, Newtonsoft.Json

2. Setup Database (2 วิธี)
วิธี A: สร้างใหม่จาก Code (แนะนำสำหรับเครื่องใหม่)
เปิด Package Manager Console ใน Visual Studio

รันคำสั่งเพื่อให้ EF Core สร้าง Database และ Table ตาม Migration ล่าสุด:

PowerShell
Update-Database
วิธี B: ใช้การ Restore (ถ้าต้องการข้อมูลเดิม)
นำไฟล์ .bak จากเครื่องเดิมมาที่เครื่องใหม่

ใน SSMS: คลิกขวาที่ Databases -> Restore Database...

ตรวจสอบว่า SQL Login (เช่น devpp) มีสิทธิ์เข้าถึง DB ที่ Restore มาแล้ว

3. Setup User Secrets (สำคัญมาก ⚠️)
เนื่องจากไฟล์ความลับไม่ได้ถูกเก็บไว้ใน Git คุณต้องสร้างใหม่ที่เครื่องใหม่เสมอ:

คลิกขวาที่โปรเจกต์ 3TaC8_PlanningPort -> เลือก Manage User Secrets

วางโครงสร้าง JSON นี้ลงไป (เปลี่ยน Connection String ตามชื่อเครื่องใหม่):

JSON
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=ชื่อเครื่องใหม่;Database=3TaC8-DATABASE;User Id=devpp;Password=รหัสผ่าน;TrustServerCertificate=True;"
  },
  "Finnhub": {
    "ApiKey": "รหัส_API_KEY_จาก_FINNHUB"
  }
}
🏗 Project Architecture & Logic
Authentication (User System)

Registration: ใช้ BCrypt ในการ Hash รหัสผ่านก่อนลง Database $2a$11$... เพื่อความปลอดภัยสูงสุด 


Login: ตรวจสอบรหัสผ่านผ่านฟังก์ชัน BCrypt.Verify 

Transactions & Portfolio (Investment)

Asset Support: รองรับหุ้น US (S&P500, Nasdaq) และสินทรัพย์อื่นๆ 


Average Cost: ระบบคำนวณต้นทุนเฉลี่ยจากรายการ 'Buy' ทั้งหมดใน Database 


Real-time Price: ดึงราคาปัจจุบันผ่าน StockService ที่เชื่อมต่อกับ Finnhub API 

📝 Developer Notes
Git Policy: ห้ามใส่ API Key หรือรหัสผ่านลงใน appsettings.json โดยเด็ดขาด ให้ใช้ User Secrets เท่านั้น

Adding Features: หากมีการเพิ่ม Column ใน Entity (Model) ต้องรัน Add-Migration <Name> และ Update-Database ทุกครั้ง