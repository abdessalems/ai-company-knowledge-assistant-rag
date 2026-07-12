# 🧠 AI Knowledge Assistant — Private RAG Document Chat

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-512BD4?logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-20-DD0031?logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?logo=typescript&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)
![Ollama](https://img.shields.io/badge/LLM-Ollama%20(local)-000000?logo=ollama&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT-FB015B?logo=jsonwebtokens&logoColor=white)
![Swagger](https://img.shields.io/badge/API-Swagger-85EA2D?logo=swagger&logoColor=black)
![License](https://img.shields.io/badge/License-MIT-green)

A **fully local, on-premise** AI assistant that lets a company's employees chat with their internal documents. Upload PDFs or text files, ask questions in natural language, and get answers grounded **only** in your own documents — each with a **citation (document + page)**. If the answer isn't in the documents, it honestly says *"I don't know."*

> **No document ever leaves your infrastructure.** The AI models run locally (Ollama), the database is local (PostgreSQL). No cloud AI, no API keys, no per-token fees — GDPR-friendly by design and even air-gappable.

---

## 📸 Screenshots

<!-- Add your screenshots to a docs/screenshots/ folder and reference them here.
     Recruiters look at visuals first — a chat screenshot with citation chips is ideal. -->

| Chat with citations | Documents |
|---------------------|-----------|
| _add `docs/screenshots/chat.png`_ | _add `docs/screenshots/documents.png`_ |

---

## ✨ Features

- 🔐 **Authentication** — register / login with JWT access tokens + refresh-token rotation, role-based authorization.
- 📄 **Document management** — upload PDF/TXT, list, delete; each user only ever sees their own documents.
- 🧩 **Automatic ingestion pipeline** — on upload, files are extracted page-by-page, split into overlapping chunks, and embedded — ready to search instantly.
- 💬 **RAG chat** — ask a question → semantic search over your chunks → a local LLM answers, grounded in the retrieved context, **with citations**.
- 🛡️ **Anti-hallucination guardrail** — the model is instructed to answer only from the provided context.
- 🧾 **Audit trail** — every question, answer, and its sources are stored.

---

## 🏗️ Architecture (Clean Architecture)

```
AIKnowledgeAssistant.Domain          → entities, enums, domain rules (no dependencies)
AIKnowledgeAssistant.Application     → interfaces, DTOs, services, validators
AIKnowledgeAssistant.Infrastructure  → EF Core, repositories, auth, AI (Ollama), storage
AIKnowledgeAssistant.API             → controllers, middleware, DI, Program.cs
frontend/                            → Angular 20 SPA
```

Dependencies point **inward** — the Domain knows nothing about the database or the web; the AI provider, database, and file storage all sit behind interfaces and can be swapped without touching business logic.

### RAG pipeline

```
Upload → extract text (per page) → chunk (≈1000 chars, 200 overlap)
       → embed each chunk (nomic-embed-text, 768-dim) → store
Question → embed → cosine-similarity search → top-K chunks
        → prompt a local LLM (llama3.2) → grounded answer + citations
```

---

## 🛠️ Tech stack

**Backend**
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-512BD4?logo=dotnet&logoColor=white)
![FluentValidation](https://img.shields.io/badge/FluentValidation-2C3E50)
![Serilog](https://img.shields.io/badge/Serilog-1F1F1F)

**Frontend**
![Angular](https://img.shields.io/badge/Angular-20-DD0031?logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?logo=typescript&logoColor=white)
![HTML5](https://img.shields.io/badge/HTML5-E34F26?logo=html5&logoColor=white)
![SCSS](https://img.shields.io/badge/SCSS-CC6699?logo=sass&logoColor=white)

**Data & AI**
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)
![Ollama](https://img.shields.io/badge/Ollama-000000?logo=ollama&logoColor=white)
![PdfPig](https://img.shields.io/badge/PdfPig-PDF%20extraction-B7410E)

**Auth & Tooling**
![JWT](https://img.shields.io/badge/JWT-FB015B?logo=jsonwebtokens&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?logo=swagger&logoColor=black)
![Git](https://img.shields.io/badge/Git-F05032?logo=git&logoColor=white)

| Layer | Technologies |
|-------|-------------|
| **Backend** | ASP.NET Core 10, C#, Clean Architecture, Entity Framework Core, PostgreSQL |
| **Auth** | JWT access + refresh tokens, BCrypt password hashing, FluentValidation |
| **AI (local & free)** | Ollama — `nomic-embed-text` (embeddings) + `llama3.2` (generation), cosine-similarity vector search, PdfPig (PDF text extraction) |
| **Frontend** | Angular 20 (standalone components, signals, reactive forms, route guards, HTTP interceptor), SCSS |

---

## 🚀 Getting started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 20+](https://nodejs.org/) and npm
- [PostgreSQL 16+](https://www.postgresql.org/)
- [Ollama](https://ollama.com/)

### 1. Database
Ensure PostgreSQL is running and the connection string in
`AIKnowledgeAssistant.API/appsettings.json` matches your setup, then apply migrations:

```bash
dotnet tool install --global dotnet-ef        # once
dotnet ef database update \
  --project AIKnowledgeAssistant.Infrastructure \
  --startup-project AIKnowledgeAssistant.API
```

### 2. Local AI models
```bash
ollama pull nomic-embed-text
ollama pull llama3.2
```
(The Ollama server runs automatically on `http://localhost:11434`.)

### 3. Backend API
```bash
dotnet run --project AIKnowledgeAssistant.API
```
API: `http://localhost:5226` · Swagger UI: `http://localhost:5226/swagger`

### 4. Frontend
```bash
cd frontend
npm install
npm start
```
App: `http://localhost:4200`

---

## 🔌 API endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/auth/register` | Create an account |
| `POST` | `/api/auth/login` | Login, receive JWT + refresh token |
| `POST` | `/api/auth/refresh-token` | Rotate tokens |
| `POST` | `/api/documents/upload` | Upload a PDF/TXT (multipart) |
| `GET`  | `/api/documents` | List your documents |
| `GET`  | `/api/documents/{id}/chunks` | Inspect a document's chunks |
| `DELETE` | `/api/documents/{id}` | Delete a document |
| `POST` | `/api/chat/ask` | Ask a question, get an answer + citations |

---

## 🔒 Security & privacy

- All AI runs **on-premise** — confidential documents never leave the company.
- Users can only access **their own** documents (enforced in the service layer).
- Passwords are hashed with BCrypt; APIs are protected with JWT.
- The AI provider is behind an interface — swap to Azure OpenAI / OpenAI with one line if ever needed.

---

## 🗺️ Roadmap

- [x] Clean Architecture solution + domain
- [x] EF Core + PostgreSQL + migrations
- [x] JWT authentication + refresh tokens
- [x] Document upload / storage / management
- [x] PDF & TXT extraction + chunking
- [x] Local embeddings (Ollama)
- [x] RAG retrieval + chat with citations
- [x] Angular 20 frontend
- [ ] Docker + docker-compose (with pgvector) + CI/CD + tests
- [ ] Word (.docx) extraction, pgvector-backed similarity search
