# StyCue｜穿搭社群與委託媒合平台後端 API / Fashion Community & Styling Commission Platform Backend API

StyCue 是一個以穿搭分享、提問與委託媒合為核心的社群平台。
此 repository 主要展示此專案後端的 ASP.NET Core Web API 實作，涵蓋資源導向 API、委託與點數交易流程、圖片儲存、第三方金流整合，以及服飾領域搜尋設計。

## 技術棧 / Tech Stack

| 領域 / Area | 技術 / Technology |
| --- | --- |
| Platform | C#, .NET 10, ASP.NET Core Web API |
| Architecture | Controller-based, Service Layer, DTOs, AutoMapper, Dependency Injection |
| Data & Search | Microsoft SQL Server 2022, Entity Framework Core, EF Core Migrations, SQL Server Full-Text Search |
| Security & Integrations | JWT, Google ID Token Verification, Azure Blob Storage, ECPay |
| API & Deployment | OpenAPI, Scalar, Azure VM, Windows Server 2022 |

### 核心領域能力

| 領域 | 展示重點 |
| --- | --- |
| 社群內容 | 貼文、委託、留言、標籤與社群互動 |
| 委託流程 | Close、Repost、Boost、最佳留言與獎勵結算 |
| 積分帳務 | 錢包、每日領取、交易紀錄、退款與獎勵追溯 |
| 身分驗證與媒體存取 | JWT、Google ID Token、Azure Blob Storage、SAS Token |
| 搜尋與資訊檢索 | SearchDocument、同義詞擴展、SQL Server Full-Text Search |
| 整合金流 | ECPay 訂單、付款驗證與點數入帳 |

### 技術亮點

#### Controller-based 架構與 RESTful API 設計

- 採用 ASP.NET Core Controller-based Web API 架構開發
- 以 Controller、Service Layer 與 DTO 分離 HTTP 合約、商業規則與資料模型。
- 依循 Resource-oriented RESTful API 設計，使用資源導向路由、HTTP Methods 與標準 Status Codes 建立一致的 API 合約。
- 委託的 Close、Repost、Boost 與最佳留言等具領域意義的操作，以明確 command endpoint 表達生命週期狀態轉換。

#### 可追溯的點數與委託獎勵流程

- 以 Point Wallet 保存目前積分餘額，並透過 Point Transaction 保留每筆異動紀錄。
- 將建立委託、加碼、提前關閉退款、最佳留言獎勵與到期補結算納入一致的商業流程。

#### 可替換的穿搭搜尋設計

- 以 SearchDocument 將貼文與委託建立統一搜尋讀模型，避免跨多個交易資料表進行複雜搜尋。
- Fashion Dictionary 提供受控同義詞擴展；ISearchCandidateProvider 保留 MVP Candidate Ranking 與 SQL Server Full-Text Search 的可替換邊界。
- 目前使用 SQL Server CONTAINSTABLE relevance rank 進行分頁與相關性排序。

#### 安全與外部(第三方)服務整合

- JWT Bearer Authentication 與 Google ID Token Verification。
- 私有 Azure Blob container 搭配短效 read-only SAS URL。
- ECPay 點數購買、付款驗證，以及 OpenAPI／Scalar API 文件與統一例外回應。

### 後端架構概覽
```
    Next.js Frontend
            │
        API Proxy
            │
    ASP.NET Core Web API
            │
       Controller
            │
       Service Layer
      ┌─────┼─────────────────┐
      │     │                 │
   EF Core  Search Service  External Integrations
      │     │                 │
  SQL Server SearchDocument Google / Azure Blob / ECPay
            │
  SQL Full-Text Search
```

### API Documentation

目前可透過 [StyCue Scalar API Documentation](https://stycue.rocket-coding.com/api-docs/) 瀏覽 API 文件，包含 endpoint 分類、request／response contract、HTTP Status Codes 與 JWT Bearer Authentication 設定。

> 此文件目前由 Staging 環境提供；VM 停用後將以 repository 內的靜態 API 文件與截圖保存展示成果。

### API Resources

| 功能群組 | 主要資源 |
| --- | --- |
| 身分與使用者 | auth、users |
| 社群內容 | posts、commissions、comments、tags |
| 社群互動 | likes、favorites、follows |
| 圖片與點數 | images、points、point-purchases |
| 探索功能 | homepage、search、search-history |

---

StyCue is a community platform for outfit sharing, questions, and styling commissions. 
This repository showcases the project's ASP.NET Core Web API backend, featuring resource-oriented APIs, commission and point-transaction workflows, media storage, payment integration, and fashion-domain search.

### Core Capabilities

| Area | Highlights |
| --- | --- |
| Community Content | Posts, commissions, comments, tags, and social interactions |
| Commission Workflow | Closing, reposting, boosting, best-comment selection, and reward settlement |
| Point Ledger | Wallet, daily claims, transaction history, refunds, and reward traceability |
| Authentication & Secure Media Access | JWT, Google ID Token verification, Azure Blob Storage, and SAS tokens |
| Search & Information Retrieval | SearchDocument, synonym expansion, and SQL Server Full-Text Search |
| Payment Integration | ECPay orders, payment verification, and point crediting |

### Engineering Highlights

#### Controller-based Architecture and RESTful API Design

- Built with an ASP.NET Core Controller-based Web API architecture
- Separating HTTP contracts, business rules, and data models through Controllers, a Service Layer, and DTOs.
- Follows Resource-oriented RESTful API design through resource-based routes, HTTP methods, and standard status codes.
- Domain-specific commission operations, including Close, Repost, Boost, and best-comment selection, use explicit command endpoints to model lifecycle state transitions.

#### Traceable Point and Commission Reward Workflows

- A Point Wallet stores the current balance while Point Transactions preserve an auditable record of every change.
- Commission creation, boosts, early-close refunds, best-comment rewards, and expiry settlement are handled as consistent business workflows.

#### Replaceable Fashion Search Design

- SearchDocument projects posts and commissions into a unified search read model, avoiding complex searches across multiple transactional tables.
- Fashion Dictionary provides controlled synonym expansion, while ISearchCandidateProvider keeps MVP Candidate Ranking and SQL Server Full-Text Search behind a replaceable boundary.
- The active provider uses SQL Server CONTAINSTABLE relevance ranking with pagination.

#### Security and External Integrations

- JWT Bearer Authentication and Google ID Token Verification.
- Private Azure Blob containers with short-lived read-only SAS URLs.
- ECPay point purchases and payment verification, plus OpenAPI/Scalar documentation and consistent exception responses.

### Backend Architecture Overview

```
    Next.js Frontend
            │
        API Proxy
            │
    ASP.NET Core Web API
            │
       Controller
            │
       Service Layer
      ┌─────┼─────────────────┐
      │     │                 │
   EF Core  Search Service  External Integrations
      │     │                 │
  SQL Server SearchDocument Google / Azure Blob / ECPay
            │
  SQL Full-Text Search
```

### API Documentation

The interactive [StyCue Scalar API Documentation](https://stycue.rocket-coding.com/api-docs/) currently provides endpoint grouping, request/response contracts, HTTP status codes, and JWT Bearer authentication configuration.

> The documentation is currently served from the Staging environment. When the VM is retired, static API documentation and screenshots will be preserved in this repository.

### API Resources

| Feature Group | Primary Resources |
| --- | --- |
| Identity & Users | auth, users |
| Community Content | posts, commissions, comments, tags |
| Social Interactions | likes, favorites, follows |
| Media & Points | images, points, point-purchases |
| Discovery | homepage, search, search-history |
