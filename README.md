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
`-- csharp SweetCakeShop/
    `-- Một số file C# rời/legacy, không nằm trong solution chính hiện tại
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
- `Controllers/ChatController.cs` và `Controllers/AIChatController.cs`: API chat khách hàng/admin.
- `Services/AI` và `Services/Chat`: xử lý intent, context, RAG, gọi Gemini/OpenAI, lưu lịch sử chat.

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

Không nên commit API key thật lên repository. Nếu key đã từng bị commit, nên thu hồi và tạo key mới trên trang quản trị dịch vụ tương ứng.

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

## Flow khởi động ứng dụng

```text
dotnet run
  |
  v
Program.cs tạo WebApplicationBuilder
  |
  v
Đọc ConnectionStrings:DefaultConnection
  |
  v
Đăng ký ApplicationDbContext với SQL Server
  |
  v
Đăng ký Identity + Role + Cookie
  |
  v
Đăng ký Session, HttpContextAccessor, MemoryCache
  |
  v
Đăng ký các service nghiệp vụ, AI, chat, payment
  |
  v
Đọc Stripe secret từ config hoặc STRIPE_SECRET_KEY
  |
  v
Build app
  |
  v
Tạo scope để chạy migration + seed data + seed admin
  |
  v
Cấu hình middleware: HTTPS, StaticAssets, Routing, Session, Auth
  |
  v
Map controller route, API controller, Razor Pages
  |
  v
App lắng nghe HTTP/HTTPS theo launchSettings
```

## Flow khách hàng mua hàng

```text
Home/Index
  |
  v
Products/Index hoặc Products/IndexPro
  |
  v
Products/Details/{id}
  |
  v
Cart/Add
  |
  v
Nếu chưa đăng nhập: giỏ hàng lưu trong Session
Nếu đã đăng nhập: giỏ hàng lưu trong bảng CartItems
  |
  v
Cart/Index
  |
  v
Cart/ApplyCoupon nếu có mã giảm giá
  |
  v
Cart/Checkout
  |
  v
Nếu chưa đăng nhập: chuyển sang Identity Login
  |
  v
Cart/CheckoutConfirm
  |
  v
OrderService.CreateOrderAsync tạo Order + OrderDetails
  |
  v
Xóa giỏ hàng, xóa coupon trong Session
  |
  v
Cart/Payment
  |
  v
Chọn COD hoặc Online
```

## Flow thanh toán

### COD

```text
Cart/ProcessPayment(method = COD)
  |
  v
OrderStatuses.ApplyConfirmed(order)
  |
  v
Lưu trạng thái đơn hàng
  |
  v
Cart/Success
```

### Online qua Stripe

```text
Cart/ProcessPayment(method = Online)
  |
  v
PaymentService.CreatePaymentAsync tạo Stripe Checkout Session
  |
  v
Order.Status = AwaitingPayment
  |
  v
Redirect sang Stripe Checkout
  |
  v
Stripe redirect về Cart/Success?orderId=...&session_id=...
  |
  v
Success action kiểm tra Stripe session
  |
  v
Nếu paid: xác nhận đơn hàng
Nếu không paid: chuyển PaymentFailed hoặc giữ trạng thái hiện tại
```

Nếu Stripe lỗi hoặc thiếu cấu hình, `PaymentService` trả fallback dạng mã chuyển khoản thủ công để UI vẫn có thể hiển thị hướng xử lý.

## Flow admin

```text
Admin đăng nhập bằng tài khoản role Admin
  |
  v
/Admin hoặc /AdminDashboard
  |
  v
Xem dashboard doanh thu, thống kê đơn, top sản phẩm
  |
  v
Quản lý sản phẩm, danh mục, nguyên liệu, công thức
  |
  v
Quản lý đơn hàng và cập nhật trạng thái
  |
  v
Quản lý coupon
  |
  v
Xuất báo cáo Excel/PDF
  |
  v
Dùng admin AI chat để hỏi dữ liệu kinh doanh
```

Các controller admin đều được bảo vệ bằng:

```csharp
[Authorize(Roles = nameof(Roles.Admin))]
```

## Flow chatbot AI

### Chat khách hàng

```text
Widget frontend gửi request tới /api/Chat/SendMessage
  |
  v
ChatController nhận UserMessage
  |
  v
CustomerProductChatService xác định user hoặc ChatToken
  |
  v
Lấy lịch sử chat + dữ liệu sản phẩm từ SQL
  |
  v
Gọi Gemini trước, fallback OpenAI nếu cần
  |
  v
Lưu tin nhắn vào CustomerChatMessages
  |
  v
Trả response JSON cho widget
```

### Chat admin

```text
Admin gửi câu hỏi tới AIChat/AdminSend hoặc /api/Chat/admin
  |
  v
Kiểm tra quyền Admin
  |
  v
AiChatService xây context kinh doanh
  |
  v
Các service phân tích sản phẩm, đơn hàng, doanh thu, tồn kho xử lý dữ liệu
  |
  v
LlmCompletionService gọi Gemini/OpenAI
  |
  v
Trả câu trả lời cho màn hình admin
```

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

## Ghi chú phát triển

- Route mặc định: `{controller=Home}/{action=Index}/{id?}`.
- Razor Pages Identity được map bằng `app.MapRazorPages()`.
- Static assets nằm trong `wwwroot`.
- Ảnh upload sản phẩm nằm trong `wwwroot/uploads/products`.
- Cart anonymous dùng Session; cart user đăng nhập dùng DB.
- Review sản phẩm yêu cầu đăng nhập.
- Checkout yêu cầu đăng nhập.
- Admin dashboard và API admin yêu cầu role `Admin`.
