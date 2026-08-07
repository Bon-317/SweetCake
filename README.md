# SweetCakeShop

SweetCakeShop là website thương mại điện tử bán bánh được xây bằng ASP.NET Core MVC. Dự án có các chức năng chính: xem danh mục/sản phẩm, tìm kiếm, giỏ hàng, mã giảm giá, đặt hàng, thanh toán COD hoặc Stripe, quản trị sản phẩm/đơn hàng/kho/nguyên liệu, dashboard doanh thu, xuất Excel/PDF và chatbot AI cho khách hàng/admin.

## Công nghệ sử dụng

- ASP.NET Core MVC + Razor Pages Identity
- .NET SDK 10, target framework `net10.0`
- Entity Framework Core + SQL Server
- ASP.NET Core Identity, Roles, Session, MemoryCache
- Bootstrap, jQuery, CSS/JS trong `wwwroot`
- Stripe Checkout cho thanh toán online
- Gemini/OpenAI cho chatbot AI
- ClosedXML và QuestPDF cho xuất Excel/PDF

## Cấu trúc dự án

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
|   |   `-- Hằng số role, trạng thái đơn hàng, intent chat, bộ lọc doanh thu
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
|   |   |-- Service nghiệp vụ: cart, order, coupon, payment, revenue, export
|   |   |-- AI/
|   |   `-- Chat/
|   |-- Views/
|   |   `-- Razor views cho customer và admin
|   |-- Areas/Identity/
|   |   `-- Razor Pages đăng ký, đăng nhập, quản lý tài khoản
|   |-- ViewComponents/
|   |   `-- CartBadgeViewComponent
|   `-- wwwroot/
|       |-- css/
|       |-- js/
|       |-- images/
|       |-- uploads/
|       `-- lib/
```

## Các module chính

- `Program.cs`: cấu hình DI, DbContext, Identity, Session, HttpClient, Stripe, middleware, routing và seed dữ liệu khi app khởi động.
- `Data/ApplicationDbContext.cs`: khai báo DbSet và quan hệ database cho sản phẩm, danh mục, đơn hàng, chi tiết đơn, nguyên liệu, công thức, giỏ hàng DB, đánh giá, coupon, thông báo, chat history.
- `Data/SeedData.cs`: tự chạy migration và seed dữ liệu mẫu cho danh mục, sản phẩm, nguyên liệu, công thức.
- `Data/IdentitySeed.cs`: seed role `Admin` và tài khoản admin mặc định.
- `Controllers/ProductsController.cs`: danh sách sản phẩm, tìm kiếm/lọc, chi tiết sản phẩm, review.
- `Controllers/CartController.cs`: giỏ hàng, coupon, checkout, tạo đơn, chọn thanh toán, xác nhận thanh toán.
- `Controllers/AdminController.cs`: quản lý đơn hàng, sản phẩm, danh mục, nguyên liệu, công thức, coupon.
- `Controllers/AdminDashboardController.cs`: dashboard doanh thu và xuất báo cáo.
- `Controllers/ChatController.cs` và `Controllers/AIChatController.cs`: API chat khách hàng/admin, lấy lịch sử chat persistent và quản lý giám sát real-time SignalR (`/api/Chat/admin/sessions`, `/api/Chat/admin/my-history`, `toggle-handoff`, `reply-customer`).
- `Services/AI` và `Services/Chat`: bộ máy Hybrid RAG, Native Function Calling, định tuyến ngữ nghĩa (`QueryPlannerService`), phân tích nâng cao Admin (`AdminAdvancedAnalyticsService`), lưu lịch sử chat persistent.
- `Hubs/ChatHub.cs`: SignalR Hub hỗ trợ giao tiếp theo thời gian thực (Live Monitoring & Handoff) giữa Khách hàng và Admin.

Hệ thống Chatbot AI của SweetCakeShop đã được nâng cấp toàn diện theo chuẩn enterprise, tích hợp sâu kiến trúc **Hybrid RAG**, **Native Function Calling**, **SignalR Real-time Monitoring/Handoff** và **Executive Advanced Analytics**.

### Bước 1: Native Function Calling & Hybrid RAG Search 
- **Hybrid RAG Engine (`HybridRagSearchService`):** Xây dựng bộ máy tìm kiếm lai kết hợp từ khóa ngữ nghĩa và full-text search tiếng Việt. Tự động loại bỏ stopword, chấm điểm đa trường (Tên sản phẩm, Mô tả, Danh mục) và ưu tiên hiển thị các sản phẩm bán chạy nhất (`SoldQuantity`).
- **Native Function Calling (`BakeryCustomerPlugin` & `QueryPlannerService`):** AI được trang bị năng lực tự gọi hàm thời gian thực trực tiếp từ SQL Server:
  - `search_products`: Tìm kiếm sản phẩm thông minh theo từ khóa hoặc sở thích.
  - `recommend_products`: Gợi ý bánh theo dịp tiệc (sinh nhật, kỷ niệm, quà tặng) hoặc mức giá/lời dặn.
  - `get_active_promotions`: Truy vấn các chương trình khuyến mãi và mã giảm giá đang áp dụng.
  - `search_news`: Tìm đọc tin tức, bài viết hướng dẫn làm bánh/tổ chức tiệc.
  - `check_order_status`: Tra cứu tình trạng đơn hàng theo số điện thoại hoặc mã đơn.
- **Tối ưu hóa Context RAG (`BuildRelevantCatalogTextAsync`):** Tự động chắt lọc và nạp Top sản phẩm & ưu đãi liên quan vào System Prompt một cách tinh gọn, ngăn ngừa tình trạng tràn token và đảm bảo độ chính xác tuyệt đối.

### Bước 2: Hợp nhất 2 luồng Chat (Customer & Admin - Real-time SignalR)
- **Đồng bộ giao diện & Trải nghiệm liền mạch (`_AdminChatWidget.cshtml`, `ai-chat.js`):** Giao diện chat chuẩn hóa theo phong cách hiện đại với hiệu ứng gõ phím (`typing dots`), thẻ sản phẩm trực quan (`Product Cards`) và các chip trả lời nhanh (`Quick Chips`).
- **Giám sát trực tiếp - Live Monitoring (`ICustomerProductChatService` & `ChatController`):**
  - Admin có thể theo dõi danh sách các phiên trò chuyện của khách hàng đang hoạt động trên website theo thời gian thực (`GET /api/Chat/admin/sessions`).
  - Xem chi tiết toàn bộ đoạn thoại của khách (`GET /api/Chat/admin/history?sessionKey=...`).
- **Tiếp quản trực tiếp - Handoff (SignalR `ChatHub`):**
  - Khi phát hiện câu hỏi khó hoặc khách có yêu cầu đặc biệt, Admin có thể bật chế độ **Tiếp quản / Ngắt AI tự động** (`POST /api/Chat/admin/toggle-handoff`).
  - Khi chế độ Handoff được bật, AI lập tức chuyển sang trạng thái giữ im lặng (`isSilent = true`). Admin gửi tin nhắn trả lời trực tiếp cho khách qua SignalR (`POST /api/Chat/admin/reply-customer`), tin nhắn lập tức hiển thị trên màn hình khách với huy hiệu **👑 Admin hỗ trợ trực tiếp**.

### Bước 3: Admin Advanced Analytics (Phân tích & Thống kê kinh doanh chuyên sâu)
- **Bộ máy phân tích dữ liệu quản trị (`AdminAdvancedAnalyticsService` & `IAdminAdvancedAnalyticsService`):** Cho phép trợ lý AI đóng vai trò như một chuyên gia phân tích kinh doanh (Business Analyst) của tiệm bánh:
  1. **Phân tích xu hướng doanh thu & nguyên nhân (`AnalyzeRevenueTrendAsync`):** So sánh doanh thu kỳ hiện tại (tuần/tháng) với kỳ trước, tính AOV (Average Order Value), tỷ lệ tăng/giảm % và tự động chỉ ra nguyên nhân biến động (như sự gia tăng lượng đơn hàng hay thay đổi dòng sản phẩm bán chạy).
  2. **Top sản phẩm bán chạy theo kỳ động (`GetTopSellingByPeriodAsync`):** Thống kê chính xác Top 5/10 món bánh bán chạy nhất trong ngày (`today`), tuần (`week`), tháng (`month`) hoặc năm (`year`), chi tiết số lượng bán và tổng doanh thu mang lại.
  3. **Phân tích kênh bán hàng & hành vi khách hàng (`GetOrderChannelBreakdownAsync`):** Bóc tách cơ cấu đơn hàng giữa **Khách thành viên** vs **Khách vãng lai**, thống kê số đơn có sử dụng mã giảm giá (Coupon) và tổng số tiền tiết kiệm cho khách.
  4. **Truy vấn động / Thống kê linh hoạt Text-to-SQL an toàn (`ExecuteDynamicAnalyticsQueryAsync`):** AI hiểu câu hỏi tự do bằng tiếng Việt, phân tích ý định và thực thi truy vấn thống kê an toàn từ DB (như kiểm tra các mã coupon dùng nhiều nhất, danh sách nguyên liệu có tồn kho dưới ngưỡng báo động, tra cứu đơn hàng chờ xử lý pending) và trình bày dữ liệu dạng bảng Markdown kèm kết luận.
- **Tối ưu định tuyến ngữ nghĩa & lập kế hoạch (`SemanticFunctionMapper` & `BuildPlannerPrompt`):** Cập nhật hệ thống định tuyến kép (LLM + Semantic Fallback), đảm bảo AI luôn ánh xạ đúng **100%** sang các hàm phân tích chuyên sâu khi Admin hỏi về xu hướng, kênh bán hàng hay thống kê động.
- **Duy trì lịch sử trò chuyện persistent khi chuyển trang (`GET /api/Chat/admin/my-history`):** Lịch sử chat của Admin được lưu persistent trong bộ nhớ cache phiên làm việc. Khi Admin di chuyển giữa các tab (Mã giảm giá, Đơn hàng, Nguyên liệu...) và mở lại bong bóng chat ✨, toàn bộ lịch sử trò chuyện và phân tích trước đó được tải lại đầy đủ, không bị mất mát.

## Yêu cầu môi trường

- .NET SDK 10.x
- SQL Server hoặc SQL Server Express/LocalDB
- Visual Studio 2022/Cursor/VS Code tùy môi trường dev
- Tùy chọn: Stripe key, Gemini key, OpenAI key nếu muốn dùng thanh toán online/chat AI đầy đủ

## Cấu hình trước khi chạy

1. Cập nhật connection string trong `SweetCakeShop/appsettings.json` hoặc dùng User Secrets:

```powershell
cd .\SweetCakeShop
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=SweetCakeShop;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

2. Cấu hình key dịch vụ nếu cần:

```powershell
dotnet user-secrets set "Stripe:PublishableKey" "your_stripe_publishable_key"
dotnet user-secrets set "Stripe:SecretKey" "your_stripe_secret_key"
dotnet user-secrets set "Gemini:ApiKey" "your_gemini_api_key"
dotnet user-secrets set "OpenAI:ApiKey" "your_openai_api_key"
```

3. Có thể dùng biến môi trường cho một số key:

```powershell
$env:STRIPE_SECRET_KEY="your_stripe_secret_key"
$env:GEMINI_API_KEY="your_gemini_api_key"
$env:OPENAI_API_KEY="your_openai_api_key"
```

## Cách chạy dự án

Chạy từ thư mục gốc repo:

```powershell
dotnet restore .\SweetCakeShop\SweetCakeShop.csproj
dotnet run --project .\SweetCakeShop\SweetCakeShop.csproj
```

Theo `Properties/launchSettings.json`, app chạy mặc định tại:

- HTTP: `http://localhost:5258`
- HTTPS: `https://localhost:7120`

Khi app khởi động, `SeedData.Initialize()` sẽ gọi `context.Database.Migrate()`, vì vậy database sẽ tự apply migration còn thiếu và seed dữ liệu mẫu nếu bảng đang trống.

## Tài khoản seed

Admin mặc định được tạo trong `Data/IdentitySeed.cs`:

```text
Email: admin@gmail.com
Password: Admin@123
Role: Admin
```

Tài khoản thường có thể đăng ký trực tiếp ở trang Identity `/Identity/Account/Register`.

## Database và migration

Migration nằm tại:

```text
SweetCakeShop/Data/Migrations/
```

Các lệnh thường dùng:

```powershell
cd .\SweetCakeShop
dotnet ef migrations add TenMigrationMoi
dotnet ef database update
```

Trong runtime hiện tại, app đã tự gọi `Database.Migrate()` khi khởi động, nên với database mới chỉ cần chạy app là schema và dữ liệu mẫu sẽ được tạo.


