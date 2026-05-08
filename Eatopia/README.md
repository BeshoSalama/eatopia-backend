# Eatopia Backend (ASP.NET Core Web API) — جاهز للتشغيل على Visual Studio ✅

ده مشروع Backend فقط (ASP.NET Core Web API) مبني طبقًا للـ Documentation اللي عندك + متوافق مع الـ Clean Architecture (تقسيم Projects).

## ✅ المتطلبات
- Visual Studio 2022 (أو أحدث)
- .NET 8 SDK
- SQL Server LocalDB (بيجي غالبًا مع Visual Studio)  
  أو أي SQL Server تاني (غير الـ Connection String)

## ✅ تشغيل المشروع
1) افتح الملف: **Eatopia.sln**
2) شغّل المشروع **Eatopia.Api** (Startup Project)
3) أول تشغيل في Development هيعمل:
   - تطبيق الـ Migrations تلقائيًا
   - Seed بيانات Demo
4) هتلاقي Swagger على:
   - `http://localhost:3001/swagger`
   - أو `https://localhost:7265/swagger`

## 🔐 بيانات Demo (Seed)
- Admin: `admin@eatopia.com` / `Admin12345`
- User: `user1@mail.com` / `Password123`
- User: `user2@mail.com` / `Password123`

## 🔑 JWT
بعد ما تعمل Login هتاخد Token:
- في Swagger اضغط **Authorize**
- اكتب:
  `Bearer {token}`

## 🗄️ قاعدة البيانات
الـ Connection String موجود في:
`src/Eatopia.Api/appsettings.json`

الافتراضي:
- LocalDB: `Server=(localdb)\MSSQLLocalDB;Database=EatopiaDb;...`

لو عندك SQL Server مختلف غيّرها براحتك.

## 🧾 JSON Naming + Frontend Compatibility
المشروع يدعم الـ snake_case في endpoints القديمة، وتم إضافة compatibility endpoints للفرونت:

- `POST http://localhost:3001/api/signup`
- `POST http://localhost:3001/api/login`
- `POST http://localhost:3001/api/ai/diet-plan`

الـ endpoints القديمة مازالت موجودة:
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`

## 💬 SignalR Chat
- Hub: `/hubs/chat`
- Authentication: JWT
- في SignalR لازم تبعت التوكن في Query String:
  `?access_token=YOUR_JWT`

### Hub Methods
- `JoinThread(threadId)`
- `SendMessage(threadId, messageText)`
والـ event اللي بيرجع:
- `MessageReceived`

## 📁 Docs
هتلاقي الملفات اللي رفعتها محفوظة داخل:
`docs/`
- Graduation Project (Phase-1).pptx
- FINAL.txt
- New Text Document (4).txt

---
تم إضافة Migration جديدة للتعديلات الخاصة بالفرونت والـ cascade والـ user fields.
