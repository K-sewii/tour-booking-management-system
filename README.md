# DongNamTrip

DongNamTrip là nền tảng minh họa quản lý và khám phá tour khu vực Đông Nam Bộ. Dự án hiện có hai giao diện sử dụng chung API:

- Web ASP.NET Core MVC cho khách hàng.
- WinForms Admin cho nhân viên có vai trò phù hợp.

API ASP.NET Core kết nối SQL Server. Xác thực, tài khoản, vai trò và hồ sơ dùng SQL Server; một số danh mục và nghiệp vụ tour hiện vẫn là dữ liệu minh họa trong bộ nhớ. Xem [Phạm vi hiện tại](#phạm-vi-hiện-tại) trước khi dùng dữ liệu cho mục đích khác.

## Kiến trúc và project

| Project | Vai trò |
| --- | --- |
| `DongNamTrip.Api` | API, xác thực SQL Server, phân quyền API, health check và `DemoStore`. |
| `DongNamTrip.Web` | Website MVC; gọi API qua `DongNamTrip.Client`, không kết nối SQL trực tiếp. |
| `DongNamTrip.Admin` | Ứng dụng WinForms quản trị; gọi API, không kết nối SQL trực tiếp. |
| `DongNamTrip.Client` | HTTP client và DTO client dùng chung cho Web/Admin. |
| `DongNamTrip.Contracts` | Contract, DTO, role dùng chung. |

## Yêu cầu môi trường

- Windows để chạy toàn bộ solution, đặc biệt là `DongNamTrip.Admin` (Windows Forms).
- [.NET SDK 10.0.100](global.json) hoặc feature band mới hơn theo `rollForward: latestFeature` trong `global.json`.
- SQL Server có hỗ trợ Windows Authentication và một công cụ chạy script, ví dụ SQL Server Management Studio (SSMS).
- Tài khoản Windows đang chạy API phải được SQL Server cho phép kết nối và có quyền phù hợp trên database `DongNamTrip`.
- Python 3 chỉ cần khi chạy các smoke test hiện có.

Kiểm tra SDK đang dùng:

```powershell
dotnet --version
dotnet --info
```

## Lấy mã nguồn và chuẩn bị

Clone từ [repository DongNamTrip](https://github.com/K-sewii/tour-booking-management-system.git), sau đó chạy mọi lệnh dưới đây tại thư mục chứa `DongNamTrip.sln`.

```powershell
git clone https://github.com/K-sewii/tour-booking-management-system.git
Set-Location .\tour-booking-management-system
Get-Item .\DongNamTrip.sln

dotnet restore .\DongNamTrip.sln
dotnet tool restore
dotnet build .\DongNamTrip.sln --no-restore
```

`dotnet-tools.json` ghim `dotnet-ef` phiên bản 10.0.12. Công cụ này phục vụ thao tác EF có chủ đích; dự án hiện dùng Database First, nên không chạy `database update`, `EnsureCreated`, `EnsureDeleted` hay migration để khởi tạo database đang dùng.

## Khởi tạo database

**Trạng thái hiện tại của repository:** không có thư mục `database/`, không có file `.sql`, và không có `DongNamTrip_Full.sql`. Vì vậy repository hiện chưa cung cấp script để một thành viên tự tạo database mới hoặc nhập dữ liệu mẫu.

Không tự suy ra schema từ các entity EF rồi tạo bảng tay. `DongNamTripDbContext` được scaffold theo Database First; nó ánh xạ database có sẵn, không phải cơ chế tạo schema của ứng dụng.

### Database mới

1. Xin nhóm phụ trách database cung cấp script SQL đã được phê duyệt (schema, vai trò và dữ liệu khởi tạo nếu cần).
2. Đọc hướng dẫn đi kèm script trước khi chạy. Chỉ chạy toàn bộ script một lần trên database mới, bằng SSMS hoặc công cụ SQL được nhóm thống nhất.
3. Sau khi nhóm thêm script vào repo, tài liệu cần ghi chính xác tên file và thứ tự chạy. Nếu script là `database/DongNamTrip_Full.sql`, mở file đó trong SSMS, chọn đúng SQL Server đích, rồi chạy toàn bộ script trên **database mới**.

Không chạy lại script tổng hợp lên database đang có dữ liệu. Không dùng lệnh `DROP`, migration, `database update`, `EnsureCreated` hoặc `EnsureDeleted` để “làm mới” database.

### Database đã có dữ liệu

Không chạy script khởi tạo. Chỉ xác nhận database đã có các bảng/role bắt buộc, cấu hình connection string riêng của máy, rồi chạy API. Endpoint `GET /health/db` giúp kiểm tra kết nối và đếm bảng/role mà không sửa dữ liệu.

## Cấu hình kết nối SQL Server

API đọc `ConnectionStrings:DongNamTrip` qua cơ chế cấu hình chuẩn của .NET. User Secrets là nơi được khuyến nghị cho Development; cấu hình chuẩn khác của .NET vẫn có thể tham gia theo thứ tự provider của ứng dụng. Không ghi connection string thật vào source hoặc Git.

Project API đã có `UserSecretsId`, nên không cần chạy `dotnet user-secrets init`. Mỗi thành viên thay tên server bằng giá trị thấy trong SSMS của chính máy mình:

```powershell
dotnet user-secrets set "ConnectionStrings:DongNamTrip" "Server=.\SQLEXPRESS;Database=DongNamTrip;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;" --project .\src\DongNamTrip.Api
```

Ví dụ trên dùng Windows Authentication (`Integrated Security=True`), không chứa mật khẩu SQL. Nếu tên instance là `MAYCHU\SQL2022`, chỉ thay phần `Server`:

```text
Server=MAYCHU\SQL2022;Database=DongNamTrip;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;
```

Không dùng User Secrets của người khác, không chia sẻ file secrets, token hoặc mật khẩu qua Git. API sẽ dừng khi không tìm thấy `ConnectionStrings:DongNamTrip`.

Web có `ApiBaseUrl` mặc định là `http://localhost:5080/` trong `src/DongNamTrip.Web/appsettings.json`. Chỉ cần đổi khi API chạy cổng localhost khác:

```powershell
$env:ApiBaseUrl = "http://localhost:5080/"
```

WinForms dùng `DONGNAMTRIP_API_URL`, mặc định cũng là `http://localhost:5080/` và chỉ chấp nhận địa chỉ loopback:

```powershell
$env:DONGNAMTRIP_API_URL = "http://localhost:5080/"
```

## Chạy ứng dụng

API phải chạy trước vì Web và WinForms đều gọi API. API và Web chỉ cho phép môi trường `Development` trong mã hiện tại. Launch profile `http` cấu hình lần lượt API ở cổng 5080 và Web ở cổng 5081.

Mở ba cửa sổ PowerShell riêng tại thư mục solution. Mỗi lệnh `dotnet run` chạy liên tục; dùng `Ctrl+C` trong đúng cửa sổ để dừng ứng dụng đó.

### Terminal 1 — API

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\DongNamTrip.Api --launch-profile http
```

- API: <http://localhost:5080>
- Health cơ bản: <http://localhost:5080/health>
- Health SQL Server: <http://localhost:5080/health/db>

### Terminal 2 — Web

Chạy sau khi API trả health thành công.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\DongNamTrip.Web --launch-profile http
```

Mở Web tại <http://localhost:5081>.

### Terminal 3 — WinForms Admin

Chạy sau khi API đã sẵn sàng.

```powershell
dotnet run --project .\src\DongNamTrip.Admin
```

Nếu đã đặt biến `DONGNAMTRIP_API_URL` tạm thời cho cổng khác, đặt lại trước khi chạy Admin hoặc mở PowerShell mới.

### Sau khi sửa mã

1. Dừng đúng API, Web hoặc Admin bằng `Ctrl+C` trong terminal đang chạy.
2. Build lại project bị ảnh hưởng hoặc cả solution.
3. Khởi động lại theo ba terminal ở trên.

Khi nghi có executable cũ hoặc DLL bị khóa, xác định **đúng** process trước:

```powershell
Get-NetTCPConnection -State Listen -LocalPort 5080,5081 | Select-Object LocalPort, OwningProcess
$dongNamTripPid = 1234 # thay 1234 bằng OwningProcess vừa tìm được
Get-Process -Id $dongNamTripPid | Select-Object Id, ProcessName, Path
```

Chỉ sau khi đã xác nhận đó là process DongNamTrip cần dừng:

```powershell
Stop-Process -Id $dongNamTripPid
```

Không dừng hàng loạt mọi process `dotnet` vì có thể làm gián đoạn ứng dụng khác.

## Tài khoản và phân quyền

### Đăng ký công khai

Đăng ký trên Web luôn tạo tài khoản `Customer` đang hoạt động. Request công khai không thể tự gán role, trạng thái hoặc hash mật khẩu. Mật khẩu được hash bằng ASP.NET Core `PasswordHasher` trước khi lưu SQL Server.

`Customer` dùng Web, có thể đăng nhập/đăng xuất, cập nhật hồ sơ liên hệ và lưu lịch trình minh họa của mình trong phiên API. Customer không được mở WinForms Admin; form Admin chỉ chấp nhận tài khoản có ít nhất một role nhân viên.

### Vai trò hiện có

Mã hiện có hỗ trợ sáu role: `Admin`, `Customer`, `Operator`, `Editor`, `Finance`, `Publisher`. Một tài khoản có thể có nhiều role.

- `Admin`, `Operator`, `Editor`, `Finance`, `Publisher` là nhóm nhân viên mà WinForms cho phép đăng nhập.
- Menu WinForms được ẩn/hiện theo role: Admin toàn bộ menu hiện có; Editor có Địa điểm; Operator có các mục vận hành; Finance có Tài chính; Publisher có Công bố nội dung.
- Việc thấy menu không thay thế kiểm tra ở API. Endpoint thêm/sửa địa điểm hiện yêu cầu `Admin` hoặc `Editor`; các màn hình nghiệp vụ khác chưa đồng nghĩa đã có API/SQL triển khai.

### Bootstrap Admin (Development)

Bootstrap Admin chỉ chạy khi môi trường là `Development` **và** `BootstrapAdmin:Enabled=true`. Nó chỉ tạo Admin nếu email chưa tồn tại; không đổi mật khẩu hoặc nâng quyền cho tài khoản đã có.

Thiết lập giá trị riêng, mạnh và không đưa vào README/Git:

```powershell
dotnet user-secrets set "BootstrapAdmin:Enabled" "true" --project .\src\DongNamTrip.Api
dotnet user-secrets set "BootstrapAdmin:Name" "Tên quản trị Development" --project .\src\DongNamTrip.Api
dotnet user-secrets set "BootstrapAdmin:Email" "admin-dev@example.test" --project .\src\DongNamTrip.Api
dotnet user-secrets set "BootstrapAdmin:Password" "<mật-khẩu-mạnh-từ-8-ký-tự>" --project .\src\DongNamTrip.Api
```

Sau khi API đã tạo tài khoản, có thể tắt bootstrap để tránh chạy kiểm tra không cần thiết:

```powershell
dotnet user-secrets set "BootstrapAdmin:Enabled" "false" --project .\src\DongNamTrip.Api
```

### Seed bảy tài khoản Development

Chỉ bật seed khi database có đủ sáu role và bạn cần tạo các tài khoản Development bị thiếu:

```powershell
dotnet user-secrets set "DevelopmentTestAccounts:Enabled" "true" --project .\src\DongNamTrip.Api
```

Lần chạy API kế tiếp, seed kiểm tra bảy email sau và chỉ tạo email chưa tồn tại:

| Email | Role |
| --- | --- |
| `admin@dongnamtrip.test` | Admin |
| `operator@dongnamtrip.test` | Operator |
| `editor@dongnamtrip.test` | Editor |
| `finance@dongnamtrip.test` | Finance |
| `publisher@dongnamtrip.test` | Publisher |
| `customer1@dongnamtrip.test` | Customer |
| `customer2@dongnamtrip.test` | Customer |

Nếu chưa cấu hình mật khẩu riêng, ứng dụng sinh mật khẩu ngẫu nhiên cho từng tài khoản mới và lưu vào User Secrets của máy đó. Xem **chỉ** các mật khẩu seed trong terminal riêng tư:

```powershell
dotnet user-secrets list --project .\src\DongNamTrip.Api | Select-String '^DevelopmentTestAccounts:Passwords:'
```

Seed không đổi mật khẩu, role hay trạng thái của email đã có. Vì repository hiện không có SQL tổng hợp, không có cơ sở để khẳng định database do script tạo đã có sẵn tài khoản kiểm thử hay mật khẩu ban đầu nào. Nếu sau này script SQL tổng hợp có các tài khoản này, không bật seed để cố thay đổi chúng; dùng tài liệu mật khẩu do nhóm database cung cấp.

## Phạm vi hiện tại

### Đã đọc/ghi SQL Server

- Xác thực: đăng ký, đăng nhập, logout, token phiên và kiểm tra trạng thái tài khoản.
- Bảng tài khoản/role: `TAIKHOAN`, `VAITRO`, `TAIKHOAN_VAITRO`.
- Hồ sơ Web: họ tên, email và số điện thoại.
- `/health/db`: kiểm tra kết nối, số bảng, `VAITRO` và `NGONNGU`.

### Vẫn là dữ liệu trong bộ nhớ (`DemoStore`)

- Danh mục địa điểm, tour hiển thị trên Web.
- Thêm/sửa địa điểm qua API và WinForms Admin.
- Lịch trình nháp/lịch trình lưu từ Planner và danh sách chuyến đi của khách.

Các dữ liệu trên mất khi API khởi động lại. Chúng không đọc/ghi các bảng SQL tour, địa điểm, lịch trình, đơn đặt, thanh toán, hoạt động hoặc suất; các bảng có trong DbContext không có nghĩa nghiệp vụ tương ứng đã được tích hợp.

Token phiên là chuỗi ngẫu nhiên được giữ trong bộ nhớ của API, mặc định sống 14.400 giây (4 giờ; cấu hình `Authentication:SessionLifetimeSeconds`). Logout xóa token phía API. Khởi động lại API làm mất toàn bộ token nên người dùng phải đăng nhập lại, kể cả cookie Web còn thời hạn. Cookie Web được đặt 4 giờ và không sliding.

Vì vậy, chưa xem dự án là đã hoàn thành toàn bộ use case đặt tour, đơn đặt, thanh toán, lịch khởi hành, quản trị tài khoản/quyền hoặc audio. Một số menu WinForms mới là khung giao diện chờ nghiệp vụ/API tương ứng.

## Kiểm thử

Sau khi `dotnet build` thành công, có hai script Python liên quan đến API/Web:

```powershell
# Chỉ chạy khi không có API/Web khác chiếm 5080 và 5081.
# Script tự khởi động executable Debug, đặt token lifetime 10 giây và dừng chúng khi kết thúc.
python .\tests\smoke.py --start
```

`smoke.py` yêu cầu User Secrets có connection string, database có 25 bảng, sáu role và ít nhất một ngôn ngữ. Script tạo tài khoản có tiền tố `auth-test-`, kiểm tra đăng ký/đăng nhập/phân quyền/khóa tài khoản/logout/hết hạn token và luồng Web, rồi xóa chính các tài khoản test đó. Không chạy song song với API/Web đang dùng các cổng này.

Kiểm tra riêng màn hình đăng nhập/đăng ký khi API/Web đã chạy tại 5080/5081:

```powershell
python .\tests\auth_screens.py
```

Script này tạo rồi xóa các email `scr02scr03-*`, kiểm tra validation, email trùng, đăng ký đồng thời, role injection, hash mật khẩu, trạng thái khóa, logout và luồng form. Các script test có thao tác SQL để dọn dữ liệu test; chỉ chạy trên database Development được phép kiểm thử.

## Lỗi thường gặp

| Hiện tượng | Cách kiểm tra/xử lý |
| --- | --- |
| API báo thiếu `ConnectionStrings:DongNamTrip` | Đặt User Secret cho đúng project API; kiểm tra tên server và database của máy mình. |
| `/health/db` trả 503 | Kiểm tra SQL Server đang chạy, Windows account có quyền, tên instance/database và firewall cục bộ. Không tạo lại database tự động. |
| Web/Admin báo không kết nối API | Khởi động API trước; xác nhận `http://localhost:5080/health`; kiểm tra `ApiBaseUrl` của Web hoặc `DONGNAMTRIP_API_URL` của Admin. |
| Web mở nhưng không đúng bản mới | Dừng đúng process đang nghe 5081, build lại Web rồi chạy lại. |
| Build lỗi DLL/EXE đang bị khóa | Dùng `Get-NetTCPConnection` và `Get-Process` để xác định PID DongNamTrip cụ thể, đóng đúng ứng dụng rồi build lại. |
| WinForms từ chối đăng nhập Customer | Đây là thiết kế hiện tại: chỉ role nhân viên mới mở Admin. |
| Không có script tạo database | Đây là thiếu sót hiện tại của repository; xin script đã duyệt từ nhóm database, không tự tạo schema từ DbContext. |

## Thông tin nhóm cần bổ sung

- Script SQL khởi tạo được phê duyệt, vị trí trong repo, thứ tự chạy và dữ liệu seed chính thức.
- Nếu có script SQL tổng hợp: danh sách tài khoản ban đầu, cách phân phối mật khẩu an toàn và việc có cần bật seed ứng dụng hay không.
- Quy ước môi trường ngoài Development, cấu hình triển khai và nơi lưu secrets trong CI/CD.
