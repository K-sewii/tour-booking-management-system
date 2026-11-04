> English documentation: [README.md](README.md)

# DongNamTrip

DongNamTrip là dự án học tập về khám phá tour và quản lý hành trình khu vực Đông Nam Bộ. Khách hàng sử dụng Web React SPA; nhân viên sử dụng ứng dụng quản trị Windows Forms. Hai ứng dụng gọi chung API ASP.NET Core, API kết nối SQL Server.

> Dự án đang trong giai đoạn phát triển. Danh mục tour, điểm đến và lịch trình đã đọc/ghi SQL Server qua API; chưa có luồng đặt tour, giữ chỗ hay cổng thanh toán thật.

## Kiến trúc

| Project | Vai trò |
| --- | --- |
| `src/Frontend/DongNamTrip.Spa` | Giao diện React + TypeScript, Vite và React Router. |
| `src/Frontend/DongNamTrip.Web` | Phục vụ SPA, làm lớp trung gian BFF và giữ bản MVC cũ để đối chiếu. |
| `src/Backend/DongNamTrip.Api` | API, xác thực, phân quyền, host mỏng cho các module. |
| `src/Frontend/DongNamTrip.Admin` | Ứng dụng WinForms cho nhân viên; gọi API. |
| `src/Frontend/DongNamTrip.Client` | HTTP client dùng chung cho Web và WinForms. |
| `src/Shared/DongNamTrip.Contracts` | Các kiểu dữ liệu và hợp đồng dùng chung. |
| `src/Backend/DongNamTrip.Modules` | 6 module nghiệp vụ (Catalog, Itinerary, Booking, Quotation, Narration, Administration; mỗi module có 3 lớp Presentation/BusinessLogic/DataAccess); `Program.cs` chỉ là host mỏng, bảng SQL còn nằm chung trong `Persistence`; xem `docs/architecture/ADR-001-modular-monolith.md`. |
| `tests` | Các bộ kiểm thử API, Web, SQL và trình duyệt. |

Frontend được tổ chức theo tính năng:

```text
src/Frontend/DongNamTrip.Spa/src/
├── app/                  Khởi tạo ứng dụng và route
├── api/                  Client, kiểu dữ liệu và trạng thái tải
├── auth/                 Phiên người dùng dùng chung
├── components/           Thành phần dùng chung
├── layouts/              Bố cục Web, xác thực và tài khoản
├── features/
│   ├── auth/             Đăng nhập, đăng ký
│   ├── account/          Hồ sơ, bảo mật, thanh toán, yêu thích, thông báo
│   ├── home/             Trang chủ
│   ├── tours/            Tour và chi tiết tour
│   ├── destinations/     Điểm đến và chi tiết điểm đến
│   └── trips/            Tạo lịch trình và chuyến đi của tôi
└── styles/               CSS riêng của SPA
```

Web BFF sử dụng cookie HttpOnly cho phiên Web, bảo vệ các thao tác ghi bằng antiforgery và chuyển tiếp yêu cầu đến API. Frontend không lưu API token trong `localStorage` hoặc `sessionStorage`. API tiếp tục kiểm tra quyền và quyền sở hữu dữ liệu.

## Yêu cầu

- Windows để chạy toàn bộ solution, bao gồm WinForms.
- .NET SDK 10; xem `global.json` để biết phiên bản và chính sách roll-forward.
- Node.js 22.12 trở lên và npm 10; bản frontend đã được kiểm tra với Node.js 22.19.0. Xem `package.json` và lockfile khi cài đặt.
- SQL Server, SQL Server Management Studio (SSMS) hoặc công cụ chạy SQL tương đương.
- Tài khoản Windows có quyền truy cập database phát triển `DongNamTrip`.
- Python 3 và các phụ thuộc của từng bộ test nếu muốn chạy kiểm thử; kiểm tra thêm Playwright và trình duyệt theo cấu hình trong `tests`.

## Lấy source và chuẩn bị database

```powershell
git clone https://github.com/K-sewii/tour-booking-management-system.git
Set-Location .\tour-booking-management-system
```

Nếu thay đổi đang nằm trên nhánh riêng và chưa được merge vào `main`, liên hệ người phụ trách để biết tên nhánh rồi checkout nhánh đó sau khi đã được push.

Database sử dụng schema do nhóm cung cấp trong `db/init/`: `DongNamTrip_Full.sql`, tiếp theo là `V001_account_area.sql` và `V002_payment_choice.sql`. Chỉ dùng script khởi tạo trên database mới theo hướng dẫn của người phụ trách; không chạy lại script tổng hợp trên database đã có dữ liệu và không sinh schema từ entity EF.

Các chức năng tài khoản bổ sung cần các script, chia sẻ riêng theo thỏa thuận của nhóm:

1. `V001_account_area.sql` — các bảng cho yêu thích, lịch sử, thông báo và tùy chọn.
2. `V002_payment_choice.sql` — lựa chọn phương thức thanh toán.

Các file chạy theo thứ tự tên. Không tự tạo schema từ entity EF, không chạy lại script khởi tạo hay dùng thao tác xóa/tạo lại database hiện hữu.

## Cấu hình SQL Server

Mỗi thành viên tự đặt connection string Development qua User Secrets cho project API. Thay tên server bằng instance SQL Server trên máy của mình:

```powershell
dotnet user-secrets set "ConnectionStrings:DongNamTrip" "Server=.\SQLEXPRESS;Database=DongNamTrip;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;" --project .\src\Backend\DongNamTrip.Api
```

Không đưa connection string thực, mật khẩu, token hoặc file User Secrets vào Git. Khi API đang chạy, mở `http://localhost:5080/health/db` để kiểm tra kết nối SQL. Nếu không dùng Windows Authentication, hãy trao đổi với người phụ trách database về cấu hình phù hợp.

## Build và chạy

Chạy các lệnh tại thư mục chứa `DongNamTrip.sln`:

```powershell
dotnet restore .\DongNamTrip.sln
dotnet build .\DongNamTrip.sln --no-restore
```

Build SPA trước khi chạy Web, và build lại sau khi thay đổi source frontend:

```powershell
Set-Location .\src\Frontend\DongNamTrip.Spa
npm ci
npm run build
Set-Location ..\..
```

Bản build SPA được tạo trong `src/Frontend/DongNamTrip.Web/wwwroot/spa/`; đây là đầu ra sinh tự động, không phải mã nguồn cần commit.

Mở các cửa sổ PowerShell riêng tại thư mục solution:

**Terminal 1 — API**

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\Backend\DongNamTrip.Api --launch-profile http
```

- API: http://localhost:5080
- Kiểm tra API: http://localhost:5080/health
- Kiểm tra database: http://localhost:5080/health/db

**Terminal 2 — Web**

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\Frontend\DongNamTrip.Web --launch-profile http
```

Mở Web tại http://localhost:5081. Nếu Web báo thiếu bản build SPA, chạy lại `npm ci` và `npm run build` như hướng dẫn ở trên.

**Terminal 3 — WinForms Admin**

```powershell
dotnet run --project .\src\Frontend\DongNamTrip.Admin
```

API cần chạy trước Web và WinForms. Cấu hình mặc định trỏ đến API tại `http://localhost:5080/`; xem `ApiBaseUrl` của Web và `DONGNAMTRIP_API_URL` của Admin nếu môi trường dùng cổng khác.

### Phát triển frontend với Vite

Giữ API và Web đang chạy, mở một terminal khác:

```powershell
Set-Location .\src\Frontend\DongNamTrip.Spa
npm run dev
```

Mở http://127.0.0.1:5173. Vite proxy các yêu cầu cần thiết sang Web trên cổng 5081. Bản MVC cũ còn được giữ để đối chiếu qua `?legacy=1` hoặc cấu hình `Spa__Enabled=false`.

## Các màn hình chính

| Đường dẫn | Chức năng |
| --- | --- |
| `/` | Trang chủ |
| `/Tours`, `/Tours/Details/{id}` | Danh sách và chi tiết tour |
| `/Destinations`, `/Destinations/Details/{id}` | Danh sách và chi tiết điểm đến |
| `/Planner`, `/Trips` | Tạo lịch trình và xem chuyến đi |
| `/Account/Login`, `/Account/Register` | SCR02 đăng nhập và SCR03 đăng ký |
| `/Account/Profile` | SCR21: hồ sơ tài khoản |
| `/Account/Security` | SCR22: mật khẩu và phiên |
| `/Account/Payments`, `/Account/Payments/{id}` | SCR23: thanh toán và hoàn tiền |
| `/Account/Favorites` | SCR24: yêu thích và lịch sử |
| `/Account/Notices` | SCR25: thông báo và tùy chọn |

## Tài khoản và phạm vi dữ liệu

- Đăng ký công khai tạo tài khoản `Customer`. Mật khẩu được hash trước khi lưu; khách không tự chọn vai trò nhân viên.
- Các vai trò hiện có gồm `Admin`, `Operator`, `Editor`, `Finance`, `Publisher` và `Customer`. WinForms dành cho tài khoản nhân viên có vai trò phù hợp. API kiểm tra quyền đối với các thao tác được bảo vệ.
- Tài khoản, vai trò, hồ sơ, mật khẩu và dữ liệu khu vực tài khoản sử dụng SQL Server, với điều kiện database đã được chuẩn bị đủ schema và script bổ sung.
- Danh mục tour, điểm đến và lịch trình đọc/ghi SQL Server qua API; cần dữ liệu mẫu (bản ghi trạng thái `Published`) thì trang mới có nội dung. Frontend chỉ gọi API: tìm kiếm, phân trang, tổng, tạm tính và giới hạn nhập liệu (`GET /api/meta`) đều do Backend trả.
- Màn hình thanh toán đọc đơn/giao dịch thuộc tài khoản khi có dữ liệu và lưu lựa chọn ưu tiên. Chưa có cổng thanh toán, quy trình đặt tour thật hay liên kết ngân hàng; không lưu số thẻ hoặc CVV.
- Phiên API hiện ở trong bộ nhớ. Đăng xuất thu hồi phiên; khởi động lại API làm người dùng phải đăng nhập lại. Một số mục WinForms còn là khung giao diện chờ nghiệp vụ.

### Tài khoản kiểm thử có sẵn trong database

Nếu database được tạo từ `db/init/DongNamTrip_Full.sql`, script đã tạo sẵn bảy tài khoản kiểm thử, **dùng chung mật khẩu ban đầu `DntTest@2026!`**. Không cần cấu hình thêm để đăng nhập. Chỉ dùng trên database phát triển.

| Email | Vai trò |
| --- | --- |
| `admin@dongnamtrip.test` | Admin |
| `operator@dongnamtrip.test` | Operator |
| `editor@dongnamtrip.test` | Editor |
| `finance@dongnamtrip.test` | Finance |
| `publisher@dongnamtrip.test` | Publisher |
| `customer1@dongnamtrip.test` | Customer |
| `customer2@dongnamtrip.test` | Customer |

WinForms Admin chỉ nhận tài khoản nhân viên (không gồm Customer). Nếu đã đổi mật khẩu trong database hoặc database không tạo từ script này, mật khẩu sẽ khác.

### Tạo tài khoản Development tùy chọn

Chỉ sử dụng trong môi trường Development. Bootstrap chỉ tạo Admin mới nếu email chưa tồn tại; không thay đổi tài khoản có sẵn. Thay toàn bộ giá trị ví dụ và không chia sẻ mật khẩu:

```powershell
dotnet user-secrets set "BootstrapAdmin:Enabled" "true" --project .\src\Backend\DongNamTrip.Api
dotnet user-secrets set "BootstrapAdmin:Name" "<tên quản trị viên>" --project .\src\Backend\DongNamTrip.Api
dotnet user-secrets set "BootstrapAdmin:Email" "<email quản trị viên>" --project .\src\Backend\DongNamTrip.Api
dotnet user-secrets set "BootstrapAdmin:Password" "<mật khẩu mạnh>" --project .\src\Backend\DongNamTrip.Api
```

Sau khi tạo xong, có thể tắt bằng cách đặt `BootstrapAdmin:Enabled` thành `false`. Project cũng có seed bảy tài khoản Development tùy chọn; chỉ bật theo thống nhất của nhóm và trên database phát triển. Seed không thay đổi tài khoản trùng email.

```powershell
dotnet user-secrets set "DevelopmentTestAccounts:Enabled" "true" --project .\src\Backend\DongNamTrip.Api
```

Mật khẩu được tạo riêng khi cần có thể xem trong terminal riêng tư:

```powershell
dotnet user-secrets list --project .\src\Backend\DongNamTrip.Api | Select-String '^DevelopmentTestAccounts:Passwords:'
```

Không gửi mật khẩu qua Git hoặc công khai ảnh chụp terminal có chứa secrets.

## Tài liệu API (chỉ Development)

Khi API chạy với `ASPNETCORE_ENVIRONMENT=Development`, mở Swagger tại http://localhost:5080/swagger hoặc OpenAPI JSON tại http://localhost:5080/openapi/v1.json. Hai endpoint này không được map ngoài Development.

Gọi `POST /api/auth/login` (hoặc đăng ký tài khoản Development cô lập), sao chép token trả về, chọn **Authorize** và dán token. Swagger gửi `Authorization: Bearer <token>` đúng tới `localhost:5080`; UI không lưu token. Token là phiên opaque trong bộ nhớ, không phải JWT, và mất khi API khởi động lại. Login/register/catalog công khai không đòi token; endpoint nhân viên nêu rõ role yêu cầu trong tài liệu.

## Kiểm thử

Chạy tại thư mục solution sau khi đã build và chuẩn bị SQL Server Development. Các test có thể tạo tài khoản tạm và dọn dữ liệu tương ứng; không chạy trên database sản xuất. Đọc hướng dẫn từng script và đảm bảo các cổng yêu cầu không bị tiến trình khác chiếm.

```powershell
python .\tests\api\smoke.py --start
python .\tests\api\auth_screens.py --start
python .\tests\e2e\browser_runner.py
python .\tests\spa\spa_runner.py
python .\tests\spa\vite_hmr_runner.py
```

Các test trình duyệt cần môi trường và phụ thuộc phù hợp với cấu hình của repository. Không suy diễn trạng thái hiện tại từ các lần chạy trước; chạy từng runner phù hợp trên database Development cô lập. WinForms chưa có kiểm thử GUI tự động.

## Xử lý lỗi thường gặp

| Hiện tượng | Cách kiểm tra |
| --- | --- |
| API báo thiếu connection string | Kiểm tra User Secrets của đúng project API, tên instance và database. |
| `/health/db` lỗi | Kiểm tra SQL Server đang chạy, tên instance/database và quyền truy cập của Windows account. |
| Web/Admin không kết nối API | Chạy API trước; kiểm tra `/health` và cấu hình địa chỉ API. |
| Web báo thiếu SPA | Chạy `npm ci` và `npm run build` trong `src/Frontend/DongNamTrip.Spa`. |
| Build báo DLL/EXE bị khóa | Dừng đúng tiến trình ứng dụng đang chạy rồi build lại. |
| Customer không vào được WinForms | WinForms dành cho các vai trò nhân viên phù hợp. |
| Thiếu file SQL bổ sung | Xin V001 và V002 từ nhóm; các file cục bộ dưới `artifacts` có thể không được đưa lên Git. |

## Docker

Chạy SQL Server + API + Web (kèm SPA) bằng Docker Compose:

```powershell
Copy-Item .env.example .env        # đặt MSSQL_SA_PASSWORD
# đặt các file SQL của nhóm (schema nền, V001, V002) vào db/init/
docker compose up --build
```

- Web: http://localhost:5081, API: http://localhost:5080 (gồm `/swagger` và `/openapi/v1.json`), SQL Server: `127.0.0.1:14330`.
- Service `db-init` chỉ tạo database `DongNamTrip` khi chưa tồn tại, không bao giờ chạy lại trên database đã có dữ liệu. Giữ volume cục bộ và trao đổi với nhóm trước mọi việc khởi tạo lại.
- Nếu `db/init` không có file `.sql`, stack vẫn chạy nhưng chức năng cần SQL sẽ lỗi.
- WinForms Admin chỉ chạy trên Windows, trỏ tới `http://localhost:5080/`.
- Image chỉ dành cho môi trường Development.
- Docker local được người dùng xác nhận đang chạy. Điều đó không thay cho kết quả toàn bộ test Docker hoặc triển khai production; workflow `.github/workflows/docker.yml` build image, quét lỗ hổng và smoke test mỗi lần push.

Bản tiếng Anh đầy đủ (kiến trúc, CI/CD, cấu hình): [README.md](README.md).
