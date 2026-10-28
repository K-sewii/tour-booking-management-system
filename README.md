[![Build and smoke test](https://github.com/K-sewii/tour-booking-management-system/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/K-sewii/tour-booking-management-system/actions/workflows/build.yml)

# DongNamTrip

## Chạy bằng Docker

Yêu cầu Docker Desktop đang chạy ở chế độ Linux containers. Tại thư mục gốc của dự án:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
# Nếu vừa tạo .env từ mẫu, đổi MSSQL_SA_PASSWORD thành mật khẩu mạnh của riêng bạn.
docker compose up -d --build
docker compose ps
```

Mở Web tại http://localhost:5081 và kiểm tra API tại http://localhost:5080/health. SQL Server có thể kết nối từ SSMS qua `localhost,14333`, tên đăng nhập `sa`, mật khẩu trong `.env`, database `DongNamTrip`. Lần chạy đầu sẽ tự nạp `DongNamTrip_Full.sql`; dữ liệu SQL được giữ trong Docker volume `dongnamtrip_sqlserver_data`. Để xem kết quả nạp:

```powershell
docker compose logs sql-init
```

File SQL đã có cơ chế bỏ qua khi bản `2026-10-28.full.1` đã được nạp. Nếu bạn cập nhật nội dung file với phiên bản mới, hãy dùng cách nâng cấp phù hợp; script sẽ dừng khi gặp database có schema cũ thay vì xóa dữ liệu. `docker compose down` dừng dịch vụ và giữ volume; `docker compose down -v` xóa cả dữ liệu SQL.

**Lưu ý:** API/Web hiện vẫn dùng dữ liệu demo trong bộ nhớ. Việc nạp database chưa làm các màn hình lấy dữ liệu từ SQL. Tài khoản trên Web vẫn là `khach@dongnamtrip.local` / `Demo@123`; các tài khoản `@dongnamtrip.test` trong file SQL chỉ tồn tại ở database cho đến khi API được kết nối với SQL.

Ứng dụng WinForms chạy trực tiếp trên Windows bằng `dotnet run --project src/DongNamTrip.Admin` và dùng API Docker tại `localhost:5080`.

Dự án nhóm môn C# — Hệ thống quản lý đặt tour du lịch.

## 1. Chuẩn bị

Cài đặt:

- Git.
- Visual Studio Code và extension C# Dev Kit.
- .NET 10 SDK.

Sử dụng Windows để chạy được cả Web và WinForms. Bản hiện tại chưa cần cài SQL Server.

## 2. Lấy mã nguồn từ GitHub

### Lấy mã nguồn lần đầu

Mở VS Code, chọn **Terminal → New Terminal**. Di chuyển đến thư mục muốn lưu dự án, sau đó chạy:

```powershell
git clone https://github.com/K-sewii/tour-booking-management-system.git
cd tour-booking-management-system
code .
```

Nếu repo yêu cầu đăng nhập, sử dụng tài khoản GitHub đã được thêm vào nhóm.

### Cập nhật mã nguồn những lần sau

Mở thư mục dự án bằng VS Code, chọn **Terminal → New Terminal**, rồi chạy:

```powershell
git switch main
git pull --ff-only origin main
```

Nếu đang có thay đổi chưa commit, hãy commit hoặc stash trước khi chuyển nhánh và cập nhật.

Chỉ cần `clone` một lần; những lần sau dùng `pull`.

## 3. Chạy dự án bằng VS Code

### Bước 1 — Khôi phục thư viện và build

Mở terminal tại thư mục chứa tệp `DongNamTrip.sln`, sau đó chạy lần lượt:

```powershell
dotnet --version
dotnet restore DongNamTrip.sln
dotnet build DongNamTrip.sln --no-restore
```

Khi build thành công, tiếp tục bước dưới đây.

### Bước 2 — Chạy API

Mở terminal thứ nhất:

```powershell
dotnet run --project src/DongNamTrip.Api
```

Giữ terminal này hoạt động. Kiểm tra API tại:

http://localhost:5080/health

### Bước 3 — Chạy Web khách hàng

Chọn **Terminal → New Terminal** để mở terminal thứ hai:

```powershell
dotnet run --project src/DongNamTrip.Web
```

Giữ terminal này hoạt động và mở trình duyệt tại:

http://localhost:5081

### Bước 4 — Chạy WinForms quản trị

Mở terminal thứ ba:

```powershell
dotnet run --project src/DongNamTrip.Admin
```

Cửa sổ quản trị WinForms sẽ xuất hiện.

**Lưu ý:** Cả ba terminal đều đặt tại thư mục chứa `DongNamTrip.sln`. API phải tiếp tục chạy để Web và WinForms lấy dữ liệu.

### Tài khoản đăng nhập thử nghiệm

| Ứng dụng | Email | Mật khẩu |
|---|---|---|
| Web khách hàng | khach@dongnamtrip.local | Demo@123 |
| WinForms quản trị | admin@dongnamtrip.local | Demo@123 |

Đây là tài khoản demo. Dữ liệu thêm mới hiện được lưu trong bộ nhớ và sẽ mất khi khởi động lại API.

### Dừng ứng dụng

- Web và API: nhấn `Ctrl + C` trong terminal tương ứng.
- WinForms: đóng cửa sổ ứng dụng.
