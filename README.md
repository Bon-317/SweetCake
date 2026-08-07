# SweetCakeShop

SweetCakeShop lÃ  website thÆ°Æ¡ng máº¡i Ä‘iá»‡n tá»­ bÃ¡n bÃ¡nh Ä‘Æ°á»£c xÃ¢y báº±ng ASP.NET Core MVC. Dá»± Ã¡n cÃ³ cÃ¡c chá»©c nÄƒng chÃ­nh: xem danh má»¥c/sáº£n pháº©m, tÃ¬m kiáº¿m, giá» hÃ ng, mÃ£ giáº£m giÃ¡, Ä‘áº·t hÃ ng, thanh toÃ¡n COD hoáº·c Stripe, quáº£n trá»‹ sáº£n pháº©m/Ä‘Æ¡n hÃ ng/kho/nguyÃªn liá»‡u, dashboard doanh thu, xuáº¥t Excel/PDF vÃ  chatbot AI cho khÃ¡ch hÃ ng/admin.

## CÃ´ng nghá»‡ sá»­ dá»¥ng

- ASP.NET Core MVC + Razor Pages Identity
- .NET SDK 10, target framework `net10.0`
- Entity Framework Core + SQL Server
- ASP.NET Core Identity, Roles, Session, MemoryCache
- Bootstrap, jQuery, CSS/JS trong `wwwroot`
- Stripe Checkout cho thanh toÃ¡n online
- Gemini/OpenAI cho chatbot AI
- ClosedXML vÃ  QuestPDF cho xuáº¥t Excel/PDF

## Cáº¥u trÃºc dá»± Ã¡n

```text
SweetCakeShop/
|-- SweetCakeShop.slnx
|-- implementation_plan.md
|-- README.md
|-- SweetCakeShop/
|   |-- Program.cs
|   |-- SweetCakeShop.csproj
|   |-- appsettings.json
|   |-- appsettings.Development.json
|   |-- Properties/
|   |   `-- launchSettings.json
|   |-- Constants/
|   |   `-- Háº±ng sá»‘ role, tráº¡ng thÃ¡i Ä‘Æ¡n hÃ ng, intent chat, bá»™ lá»c doanh thu
|   |-- Controllers/
|   |   `-- Controller cho Home, Products, Cart, Admin, Dashboard, Chat, Notification
|   |-- Data/
|   |   |-- ApplicationDbContext.cs
|   |   |-- SeedData.cs
|   |   |-- IdentitySeed.cs
|   |   `-- Migrations/
|   |-- Models/
|   |   |-- Entity database
|   |   |-- Api/
|   |   |-- AI/
|   |   `-- ViewModels/
|   |-- Services/
|   |   |-- Service nghiá»‡p vá»¥: cart, order, coupon, payment, revenue, export
|   |   |-- AI/
|   |   `-- Chat/
|   |-- Views/
|   |   `-- Razor views cho customer vÃ  admin
|   |-- Areas/Identity/
|   |   `-- Razor Pages Ä‘Äƒng kÃ½, Ä‘Äƒng nháº­p, quáº£n lÃ½ tÃ i khoáº£n
|   |-- ViewComponents/
|   |   `-- CartBadgeViewComponent
|   `-- wwwroot/
|       |-- css/
|       |-- js/
|       |-- images/
|       |-- uploads/
|       `-- lib/
```

## CÃ¡c module chÃ­nh

- `Program.cs`: cáº¥u hÃ¬nh DI, DbContext, Identity, Session, HttpClient, Stripe, middleware, routing vÃ  seed dá»¯ liá»‡u khi app khá»Ÿi Ä‘á»™ng.
- `Data/ApplicationDbContext.cs`: khai bÃ¡o DbSet vÃ  quan há»‡ database cho sáº£n pháº©m, danh má»¥c, Ä‘Æ¡n hÃ ng, chi tiáº¿t Ä‘Æ¡n, nguyÃªn liá»‡u, cÃ´ng thá»©c, giá» hÃ ng DB, Ä‘Ã¡nh giÃ¡, coupon, thÃ´ng bÃ¡o, chat history.
- `Data/SeedData.cs`: tá»± cháº¡y migration vÃ  seed dá»¯ liá»‡u máº«u cho danh má»¥c, sáº£n pháº©m, nguyÃªn liá»‡u, cÃ´ng thá»©c.
- `Data/IdentitySeed.cs`: seed role `Admin` vÃ  tÃ i khoáº£n admin máº·c Ä‘á»‹nh.
- `Controllers/ProductsController.cs`: danh sÃ¡ch sáº£n pháº©m, tÃ¬m kiáº¿m/lá»c, chi tiáº¿t sáº£n pháº©m, review.
- `Controllers/CartController.cs`: giá» hÃ ng, coupon, checkout, táº¡o Ä‘Æ¡n, chá»n thanh toÃ¡n, xÃ¡c nháº­n thanh toÃ¡n.
- `Controllers/AdminController.cs`: quáº£n lÃ½ Ä‘Æ¡n hÃ ng, sáº£n pháº©m, danh má»¥c, nguyÃªn liá»‡u, cÃ´ng thá»©c, coupon.
- `Controllers/AdminDashboardController.cs`: dashboard doanh thu vÃ  xuáº¥t bÃ¡o cÃ¡o.
- `Controllers/ChatController.cs` vÃ  `Controllers/AIChatController.cs`: API chat khÃ¡ch hÃ ng/admin, láº¥y lá»‹ch sá»­ chat persistent vÃ  quáº£n lÃ½ giÃ¡m sÃ¡t real-time SignalR (`/api/Chat/admin/sessions`, `/api/Chat/admin/my-history`, `toggle-handoff`, `reply-customer`).
- `Services/AI` vÃ  `Services/Chat`: bá»™ mÃ¡y Hybrid RAG, Native Function Calling, Ä‘á»‹nh tuyáº¿n ngá»¯ nghÄ©a (`QueryPlannerService`), phÃ¢n tÃ­ch nÃ¢ng cao Admin (`AdminAdvancedAnalyticsService`), lÆ°u lá»‹ch sá»­ chat persistent.
- `Hubs/ChatHub.cs`: SignalR Hub há»— trá»£ giao tiáº¿p theo thá»i gian thá»±c (Live Monitoring & Handoff) giá»¯a KhÃ¡ch hÃ ng vÃ  Admin.

## Cáº­p nháº­t ná»•i báº­t: Lá»™ trÃ¬nh 3 bÆ°á»›c Chatbot AI ToÃ n diá»‡n (HoÃ n thÃ nh 100%)

Há»‡ thá»‘ng Chatbot AI cá»§a SweetCakeShop Ä‘Ã£ Ä‘Æ°á»£c nÃ¢ng cáº¥p toÃ n diá»‡n theo chuáº©n enterprise, tÃ­ch há»£p sÃ¢u kiáº¿n trÃºc **Hybrid RAG**, **Native Function Calling**, **SignalR Real-time Monitoring/Handoff** vÃ  **Executive Advanced Analytics**.

### BÆ°á»›c 1: Native Function Calling & Hybrid RAG Search (HÆ°á»›ng A - KhÃ¡ch hÃ ng)
- **Hybrid RAG Engine (`HybridRagSearchService`):** XÃ¢y dá»±ng bá»™ mÃ¡y tÃ¬m kiáº¿m lai káº¿t há»£p tá»« khÃ³a ngá»¯ nghÄ©a vÃ  full-text search tiáº¿ng Viá»‡t. Tá»± Ä‘á»™ng loáº¡i bá» stopword, cháº¥m Ä‘iá»ƒm Ä‘a trÆ°á»ng (TÃªn sáº£n pháº©m, MÃ´ táº£, Danh má»¥c) vÃ  Æ°u tiÃªn hiá»ƒn thá»‹ cÃ¡c sáº£n pháº©m bÃ¡n cháº¡y nháº¥t (`SoldQuantity`).
- **Native Function Calling (`BakeryCustomerPlugin` & `QueryPlannerService`):** AI Ä‘Æ°á»£c trang bá»‹ nÄƒng lá»±c tá»± gá»i hÃ m thá»i gian thá»±c trá»±c tiáº¿p tá»« SQL Server:
  - `search_products`: TÃ¬m kiáº¿m sáº£n pháº©m thÃ´ng minh theo tá»« khÃ³a hoáº·c sá»Ÿ thÃ­ch.
  - `recommend_products`: Gá»£i Ã½ bÃ¡nh theo dá»‹p tiá»‡c (sinh nháº­t, ká»· niá»‡m, quÃ  táº·ng) hoáº·c má»©c giÃ¡/lá»i dáº·n.
  - `get_active_promotions`: Truy váº¥n cÃ¡c chÆ°Æ¡ng trÃ¬nh khuyáº¿n mÃ£i vÃ  mÃ£ giáº£m giÃ¡ Ä‘ang Ã¡p dá»¥ng.
  - `search_news`: TÃ¬m Ä‘á»c tin tá»©c, bÃ i viáº¿t hÆ°á»›ng dáº«n lÃ m bÃ¡nh/tá»• chá»©c tiá»‡c.
  - `check_order_status`: Tra cá»©u tÃ¬nh tráº¡ng Ä‘Æ¡n hÃ ng theo sá»‘ Ä‘iá»‡n thoáº¡i hoáº·c mÃ£ Ä‘Æ¡n.
- **Tá»‘i Æ°u hÃ³a Context RAG (`BuildRelevantCatalogTextAsync`):** Tá»± Ä‘á»™ng cháº¯t lá»c vÃ  náº¡p Top sáº£n pháº©m & Æ°u Ä‘Ã£i liÃªn quan vÃ o System Prompt má»™t cÃ¡ch tinh gá»n, ngÄƒn ngá»«a tÃ¬nh tráº¡ng trÃ n token vÃ  Ä‘áº£m báº£o Ä‘á»™ chÃ­nh xÃ¡c tuyá»‡t Ä‘á»‘i.

### BÆ°á»›c 2: Há»£p nháº¥t 2 luá»“ng Chat (Customer & Admin - Real-time SignalR)
- **Äá»“ng bá»™ giao diá»‡n & Tráº£i nghiá»‡m liá»n máº¡ch (`_AdminChatWidget.cshtml`, `ai-chat.js`):** Giao diá»‡n chat chuáº©n hÃ³a theo phong cÃ¡ch hiá»‡n Ä‘áº¡i vá»›i hiá»‡u á»©ng gÃµ phÃ­m (`typing dots`), tháº» sáº£n pháº©m trá»±c quan (`Product Cards`) vÃ  cÃ¡c chip tráº£ lá»i nhanh (`Quick Chips`).
- **GiÃ¡m sÃ¡t trá»±c tiáº¿p - Live Monitoring (`ICustomerProductChatService` & `ChatController`):**
  - Admin cÃ³ thá»ƒ theo dÃµi danh sÃ¡ch cÃ¡c phiÃªn trÃ² chuyá»‡n cá»§a khÃ¡ch hÃ ng Ä‘ang hoáº¡t Ä‘á»™ng trÃªn website theo thá»i gian thá»±c (`GET /api/Chat/admin/sessions`).
  - Xem chi tiáº¿t toÃ n bá»™ Ä‘oáº¡n thoáº¡i cá»§a khÃ¡ch (`GET /api/Chat/admin/history?sessionKey=...`).
- **Tiáº¿p quáº£n trá»±c tiáº¿p - Handoff (SignalR `ChatHub`):**
  - Khi phÃ¡t hiá»‡n cÃ¢u há»i khÃ³ hoáº·c khÃ¡ch cÃ³ yÃªu cáº§u Ä‘áº·c biá»‡t, Admin cÃ³ thá»ƒ báº­t cháº¿ Ä‘á»™ **Tiáº¿p quáº£n / Ngáº¯t AI tá»± Ä‘á»™ng** (`POST /api/Chat/admin/toggle-handoff`).
  - Khi cháº¿ Ä‘á»™ Handoff Ä‘Æ°á»£c báº­t, AI láº­p tá»©c chuyá»ƒn sang tráº¡ng thÃ¡i giá»¯ im láº·ng (`isSilent = true`). Admin gá»­i tin nháº¯n tráº£ lá»i trá»±c tiáº¿p cho khÃ¡ch qua SignalR (`POST /api/Chat/admin/reply-customer`), tin nháº¯n láº­p tá»©c hiá»ƒn thá»‹ trÃªn mÃ n hÃ¬nh khÃ¡ch vá»›i huy hiá»‡u **ðŸ‘‘ Admin há»— trá»£ trá»±c tiáº¿p**.

### BÆ°á»›c 3: Admin Advanced Analytics (PhÃ¢n tÃ­ch & Thá»‘ng kÃª kinh doanh chuyÃªn sÃ¢u)
- **Bá»™ mÃ¡y phÃ¢n tÃ­ch dá»¯ liá»‡u quáº£n trá»‹ (`AdminAdvancedAnalyticsService` & `IAdminAdvancedAnalyticsService`):** Cho phÃ©p trá»£ lÃ½ AI Ä‘Ã³ng vai trÃ² nhÆ° má»™t chuyÃªn gia phÃ¢n tÃ­ch kinh doanh (Business Analyst) cá»§a tiá»‡m bÃ¡nh:
  1. **PhÃ¢n tÃ­ch xu hÆ°á»›ng doanh thu & nguyÃªn nhÃ¢n (`AnalyzeRevenueTrendAsync`):** So sÃ¡nh doanh thu ká»³ hiá»‡n táº¡i (tuáº§n/thÃ¡ng) vá»›i ká»³ trÆ°á»›c, tÃ­nh AOV (Average Order Value), tá»· lá»‡ tÄƒng/giáº£m % vÃ  tá»± Ä‘á»™ng chá»‰ ra nguyÃªn nhÃ¢n biáº¿n Ä‘á»™ng (nhÆ° sá»± gia tÄƒng lÆ°á»£ng Ä‘Æ¡n hÃ ng hay thay Ä‘á»•i dÃ²ng sáº£n pháº©m bÃ¡n cháº¡y).
  2. **Top sáº£n pháº©m bÃ¡n cháº¡y theo ká»³ Ä‘á»™ng (`GetTopSellingByPeriodAsync`):** Thá»‘ng kÃª chÃ­nh xÃ¡c Top 5/10 mÃ³n bÃ¡nh bÃ¡n cháº¡y nháº¥t trong ngÃ y (`today`), tuáº§n (`week`), thÃ¡ng (`month`) hoáº·c nÄƒm (`year`), chi tiáº¿t sá»‘ lÆ°á»£ng bÃ¡n vÃ  tá»•ng doanh thu mang láº¡i.
  3. **PhÃ¢n tÃ­ch kÃªnh bÃ¡n hÃ ng & hÃ nh vi khÃ¡ch hÃ ng (`GetOrderChannelBreakdownAsync`):** BÃ³c tÃ¡ch cÆ¡ cáº¥u Ä‘Æ¡n hÃ ng giá»¯a **KhÃ¡ch thÃ nh viÃªn** vs **KhÃ¡ch vÃ£ng lai**, thá»‘ng kÃª sá»‘ Ä‘Æ¡n cÃ³ sá»­ dá»¥ng mÃ£ giáº£m giÃ¡ (Coupon) vÃ  tá»•ng sá»‘ tiá»n tiáº¿t kiá»‡m cho khÃ¡ch.
  4. **Truy váº¥n Ä‘á»™ng / Thá»‘ng kÃª linh hoáº¡t Text-to-SQL an toÃ n (`ExecuteDynamicAnalyticsQueryAsync`):** AI hiá»ƒu cÃ¢u há»i tá»± do báº±ng tiáº¿ng Viá»‡t, phÃ¢n tÃ­ch Ã½ Ä‘á»‹nh vÃ  thá»±c thi truy váº¥n thá»‘ng kÃª an toÃ n tá»« DB (nhÆ° kiá»ƒm tra cÃ¡c mÃ£ coupon dÃ¹ng nhiá»u nháº¥t, danh sÃ¡ch nguyÃªn liá»‡u cÃ³ tá»“n kho dÆ°á»›i ngÆ°á»¡ng bÃ¡o Ä‘á»™ng, tra cá»©u Ä‘Æ¡n hÃ ng chá» xá»­ lÃ½ pending) vÃ  trÃ¬nh bÃ y dá»¯ liá»‡u dáº¡ng báº£ng Markdown kÃ¨m káº¿t luáº­n.
- **Tá»‘i Æ°u Ä‘á»‹nh tuyáº¿n ngá»¯ nghÄ©a & láº­p káº¿ hoáº¡ch (`SemanticFunctionMapper` & `BuildPlannerPrompt`):** Cáº­p nháº­t há»‡ thá»‘ng Ä‘á»‹nh tuyáº¿n kÃ©p (LLM + Semantic Fallback), Ä‘áº£m báº£o AI luÃ´n Ã¡nh xáº¡ Ä‘Ãºng **100%** sang cÃ¡c hÃ m phÃ¢n tÃ­ch chuyÃªn sÃ¢u khi Admin há»i vá» xu hÆ°á»›ng, kÃªnh bÃ¡n hÃ ng hay thá»‘ng kÃª Ä‘á»™ng.
- **Duy trÃ¬ lá»‹ch sá»­ trÃ² chuyá»‡n persistent khi chuyá»ƒn trang (`GET /api/Chat/admin/my-history`):** Lá»‹ch sá»­ chat cá»§a Admin Ä‘Æ°á»£c lÆ°u persistent trong bá»™ nhá»› cache phiÃªn lÃ m viá»‡c. Khi Admin di chuyá»ƒn giá»¯a cÃ¡c tab (MÃ£ giáº£m giÃ¡, ÄÆ¡n hÃ ng, NguyÃªn liá»‡u...) vÃ  má»Ÿ láº¡i bong bÃ³ng chat âœ¨, toÃ n bá»™ lá»‹ch sá»­ trÃ² chuyá»‡n vÃ  phÃ¢n tÃ­ch trÆ°á»›c Ä‘Ã³ Ä‘Æ°á»£c táº£i láº¡i Ä‘áº§y Ä‘á»§, khÃ´ng bá»‹ máº¥t mÃ¡t.

## YÃªu cáº§u mÃ´i trÆ°á»ng

- .NET SDK 10.x
- SQL Server hoáº·c SQL Server Express/LocalDB
- Visual Studio 2022/Cursor/VS Code tÃ¹y mÃ´i trÆ°á»ng dev
- TÃ¹y chá»n: Stripe key, Gemini key, OpenAI key náº¿u muá»‘n dÃ¹ng thanh toÃ¡n online/chat AI Ä‘áº§y Ä‘á»§

## Cáº¥u hÃ¬nh trÆ°á»›c khi cháº¡y

1. Cáº­p nháº­t connection string trong `SweetCakeShop/appsettings.json` hoáº·c dÃ¹ng User Secrets:

```powershell
cd .\SweetCakeShop
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=SweetCakeShop;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

2. Cáº¥u hÃ¬nh key dá»‹ch vá»¥ náº¿u cáº§n:

```powershell
dotnet user-secrets set "Stripe:PublishableKey" "your_stripe_publishable_key"
dotnet user-secrets set "Stripe:SecretKey" "your_stripe_secret_key"
dotnet user-secrets set "Gemini:ApiKey" "your_gemini_api_key"
dotnet user-secrets set "OpenAI:ApiKey" "your_openai_api_key"
```

3. CÃ³ thá»ƒ dÃ¹ng biáº¿n mÃ´i trÆ°á»ng cho má»™t sá»‘ key:

```powershell
$env:STRIPE_SECRET_KEY="your_stripe_secret_key"
$env:GEMINI_API_KEY="your_gemini_api_key"
$env:OPENAI_API_KEY="your_openai_api_key"
```

KhÃ´ng nÃªn commit API key tháº­t lÃªn repository. Náº¿u key Ä‘Ã£ tá»«ng bá»‹ commit, nÃªn thu há»“i vÃ  táº¡o key má»›i trÃªn trang quáº£n trá»‹ dá»‹ch vá»¥ tÆ°Æ¡ng á»©ng.

## CÃ¡ch cháº¡y dá»± Ã¡n

Cháº¡y tá»« thÆ° má»¥c gá»‘c repo:

```powershell
dotnet restore .\SweetCakeShop\SweetCakeShop.csproj
dotnet run --project .\SweetCakeShop\SweetCakeShop.csproj
```

Theo `Properties/launchSettings.json`, app cháº¡y máº·c Ä‘á»‹nh táº¡i:

- HTTP: `http://localhost:5258`
- HTTPS: `https://localhost:7120`

Khi app khá»Ÿi Ä‘á»™ng, `SeedData.Initialize()` sáº½ gá»i `context.Database.Migrate()`, vÃ¬ váº­y database sáº½ tá»± apply migration cÃ²n thiáº¿u vÃ  seed dá»¯ liá»‡u máº«u náº¿u báº£ng Ä‘ang trá»‘ng.

## TÃ i khoáº£n seed

Admin máº·c Ä‘á»‹nh Ä‘Æ°á»£c táº¡o trong `Data/IdentitySeed.cs`:

```text
Email: admin@gmail.com
Password: Admin@123
Role: Admin
```

TÃ i khoáº£n thÆ°á»ng cÃ³ thá»ƒ Ä‘Äƒng kÃ½ trá»±c tiáº¿p á»Ÿ trang Identity `/Identity/Account/Register`.

## Flow khá»Ÿi Ä‘á»™ng á»©ng dá»¥ng

```text
dotnet run
  |
  v
Program.cs táº¡o WebApplicationBuilder
  |
  v
Äá»c ConnectionStrings:DefaultConnection
  |
  v
ÄÄƒng kÃ½ ApplicationDbContext vá»›i SQL Server
  |
  v
ÄÄƒng kÃ½ Identity + Role + Cookie
  |
  v
ÄÄƒng kÃ½ Session, HttpContextAccessor, MemoryCache
  |
  v
ÄÄƒng kÃ½ cÃ¡c service nghiá»‡p vá»¥, AI, chat, payment
  |
  v
Äá»c Stripe secret tá»« config hoáº·c STRIPE_SECRET_KEY
  |
  v
Build app
  |
  v
Táº¡o scope Ä‘á»ƒ cháº¡y migration + seed data + seed admin
  |
  v
Cáº¥u hÃ¬nh middleware: HTTPS, StaticAssets, Routing, Session, Auth
  |
  v
Map controller route, API controller, Razor Pages
  |
  v
App láº¯ng nghe HTTP/HTTPS theo launchSettings
```

## Flow khÃ¡ch hÃ ng mua hÃ ng

```text
Home/Index
  |
  v
Products/Index hoáº·c Products/IndexPro
  |
  v
Products/Details/{id}
  |
  v
Cart/Add
  |
  v
Náº¿u chÆ°a Ä‘Äƒng nháº­p: giá» hÃ ng lÆ°u trong Session
Náº¿u Ä‘Ã£ Ä‘Äƒng nháº­p: giá» hÃ ng lÆ°u trong báº£ng CartItems
  |
  v
Cart/Index
  |
  v
Cart/ApplyCoupon náº¿u cÃ³ mÃ£ giáº£m giÃ¡
  |
  v
Cart/Checkout
  |
  v
Náº¿u chÆ°a Ä‘Äƒng nháº­p: chuyá»ƒn sang Identity Login
  |
  v
Cart/CheckoutConfirm
  |
  v
OrderService.CreateOrderAsync táº¡o Order + OrderDetails
  |
  v
XÃ³a giá» hÃ ng, xÃ³a coupon trong Session
  |
  v
Cart/Payment
  |
  v
Chá»n COD hoáº·c Online
```

## Flow thanh toÃ¡n

### COD

```text
Cart/ProcessPayment(method = COD)
  |
  v
OrderStatuses.ApplyConfirmed(order)
  |
  v
LÆ°u tráº¡ng thÃ¡i Ä‘Æ¡n hÃ ng
  |
  v
Cart/Success
```

### Online qua Stripe

```text
Cart/ProcessPayment(method = Online)
  |
  v
PaymentService.CreatePaymentAsync táº¡o Stripe Checkout Session
  |
  v
Order.Status = AwaitingPayment
  |
  v
Redirect sang Stripe Checkout
  |
  v
Stripe redirect vá» Cart/Success?orderId=...&session_id=...
  |
  v
Success action kiá»ƒm tra Stripe session
  |
  v
Náº¿u paid: xÃ¡c nháº­n Ä‘Æ¡n hÃ ng
Náº¿u khÃ´ng paid: chuyá»ƒn PaymentFailed hoáº·c giá»¯ tráº¡ng thÃ¡i hiá»‡n táº¡i
```

Náº¿u Stripe lá»—i hoáº·c thiáº¿u cáº¥u hÃ¬nh, `PaymentService` tráº£ fallback dáº¡ng mÃ£ chuyá»ƒn khoáº£n thá»§ cÃ´ng Ä‘á»ƒ UI váº«n cÃ³ thá»ƒ hiá»ƒn thá»‹ hÆ°á»›ng xá»­ lÃ½.

## Flow admin

```text
Admin Ä‘Äƒng nháº­p báº±ng tÃ i khoáº£n role Admin
  |
  v
/Admin hoáº·c /AdminDashboard
  |
  v
Xem dashboard doanh thu, thá»‘ng kÃª Ä‘Æ¡n, top sáº£n pháº©m
  |
  v
Quáº£n lÃ½ sáº£n pháº©m, danh má»¥c, nguyÃªn liá»‡u, cÃ´ng thá»©c
  |
  v
Quáº£n lÃ½ Ä‘Æ¡n hÃ ng vÃ  cáº­p nháº­t tráº¡ng thÃ¡i
  |
  v
Quáº£n lÃ½ coupon
  |
  v
Xuáº¥t bÃ¡o cÃ¡o Excel/PDF
  |
  v
DÃ¹ng admin AI chat Ä‘á»ƒ há»i dá»¯ liá»‡u kinh doanh
```

CÃ¡c controller admin Ä‘á»u Ä‘Æ°á»£c báº£o vá»‡ báº±ng:

```csharp
[Authorize(Roles = nameof(Roles.Admin))]
```

## Flow chatbot AI

### Chat khÃ¡ch hÃ ng (Hybrid RAG & Native Function Calling + Live SignalR)

```text
Widget frontend gá»­i request tá»›i /api/Chat/SendMessage (hoáº·c nháº­n realtime qua SignalR ChatHub)
  |
  v
ChatController nháº­n UserMessage & kiá»ƒm tra tráº¡ng thÃ¡i Handoff (Tiáº¿p quáº£n trá»±c tiáº¿p)
  |
  +---[Náº¿u Admin Ä‘ang Handoff (`active = true`)]---> AI giá»¯ im láº·ng (`isSilent = true`), Admin tráº£ lá»i trá»±c tiáº¿p qua SignalR
  |
  v [Náº¿u AI tá»± Ä‘á»™ng tÆ° váº¥n]
CustomerProductChatService xÃ¡c Ä‘á»‹nh user hoáº·c ChatToken & láº¥y lá»‹ch sá»­ tá»« DB
  |
  v
QueryPlannerService / Semantic Kernel Ã¡nh xáº¡ Ã½ Ä‘á»‹nh & gá»i Native Function Calling (`BakeryCustomerPlugin`)
  |
  +---> search_products / recommend_products / get_active_promotions / search_news / check_order_status
  |
  v
HybridRagSearchService thá»±c hiá»‡n tÃ¬m kiáº¿m lai (Tá»« khÃ³a ngá»¯ nghÄ©a + Full-text cháº¥m Ä‘iá»ƒm Ä‘a trÆ°á»ng SQL Server)
  |
  v
BuildRelevantCatalogTextAsync cháº¯t lá»c Top sáº£n pháº©m & Æ°u Ä‘Ã£i náº¡p vÃ o System Prompt
  |
  v
LlmCompletionService gá»i Gemini / OpenAI (hoáº·c Fallback) táº¡o pháº£n há»“i tá»± nhiÃªn báº±ng tiáº¿ng Viá»‡t
  |
  v
LÆ°u tin nháº¯n vÃ o CustomerChatMessages & gá»­i Ä‘á»“ng thá»i qua SignalR tá»›i Admin Live Monitoring Panel
```

### Chat admin (Advanced Executive Analytics & Persistent History)

```text
Admin má»Ÿ bong bÃ³ng chat âœ¨ (Tá»± Ä‘á»™ng khÃ´i phá»¥c lá»‹ch sá»­ qua /api/Chat/admin/my-history)
  |
  v
Admin nháº­p cÃ¢u há»i phÃ¢n tÃ­ch (VD: "Xu hÆ°á»›ng doanh thu tuáº§n nÃ y vÃ¬ sao?", "KÃªnh bÃ¡n hÃ ng nÃ o tá»‘t?")
  |
  v
ChatController nháº­n request (/api/Chat/admin) & kiá»ƒm tra quyá»n Role Admin
  |
  v
AiChatService / QueryPlannerService thá»±c hiá»‡n Æ°u tiÃªn Ä‘á»‹nh tuyáº¿n ngá»¯ nghÄ©a kÃ©p (LLM + Fast Deterministic Map)
  |
  +---> AnalyzeRevenueTrend: PhÃ¢n tÃ­ch xu hÆ°á»›ng, % tÄƒng trÆ°á»Ÿng, AOV vÃ  chá»‰ rÃµ nguyÃªn nhÃ¢n biáº¿n Ä‘á»™ng
  +---> GetTopSellingByPeriod: Top sáº£n pháº©m bÃ¡n cháº¡y theo ká»³ Ä‘á»™ng (today, week, month, year)
  +---> GetOrderChannelBreakdown: BÃ³c tÃ¡ch kÃªnh bÃ¡n hÃ ng, ThÃ nh viÃªn vs VÃ£ng lai, hiá»‡u quáº£ Coupon
  +---> ExecuteDynamicAnalyticsQuery: Truy váº¥n tá»± do Text-to-SQL an toÃ n, cáº£nh bÃ¡o tá»“n kho, Ä‘Æ¡n pending
  |
  v
AdminAdvancedAnalyticsService thá»±c thi truy váº¥n nghiá»‡p vá»¥ cáº¥p cao trá»±c tiáº¿p trÃªn ApplicationDbContext
  |
  v
ConsultantResponseService xÃ¢y dá»±ng Executive RAG Context (cháº¿ Ä‘á»™ Admin háº¡n má»©c 1000 tokens)
  |
  v
LlmCompletionService tá»•ng há»£p bÃ¡o cÃ¡o quáº£n trá»‹ chuyÃªn nghiá»‡p & lÆ°u tráº¡ng thÃ¡i persistent
```

## Database vÃ  migration

Migration náº±m táº¡i:

```text
SweetCakeShop/Data/Migrations/
```

CÃ¡c lá»‡nh thÆ°á»ng dÃ¹ng:

```powershell
cd .\SweetCakeShop
dotnet ef migrations add TenMigrationMoi
dotnet ef database update
```

Trong runtime hiá»‡n táº¡i, app Ä‘Ã£ tá»± gá»i `Database.Migrate()` khi khá»Ÿi Ä‘á»™ng, nÃªn vá»›i database má»›i chá»‰ cáº§n cháº¡y app lÃ  schema vÃ  dá»¯ liá»‡u máº«u sáº½ Ä‘Æ°á»£c táº¡o.

## Ghi chÃº phÃ¡t triá»ƒn

- Route máº·c Ä‘á»‹nh: `{controller=Home}/{action=Index}/{id?}`.
- Razor Pages Identity Ä‘Æ°á»£c map báº±ng `app.MapRazorPages()`.
- Static assets náº±m trong `wwwroot`.
- áº¢nh upload sáº£n pháº©m náº±m trong `wwwroot/uploads/products`.
- Cart anonymous dÃ¹ng Session; cart user Ä‘Äƒng nháº­p dÃ¹ng DB.
- Review sáº£n pháº©m yÃªu cáº§u Ä‘Äƒng nháº­p.
- Checkout yÃªu cáº§u Ä‘Äƒng nháº­p.
- Admin dashboard vÃ  API admin yÃªu cáº§u role `Admin`.

