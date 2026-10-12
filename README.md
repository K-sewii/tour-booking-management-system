# DongNamTrip

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