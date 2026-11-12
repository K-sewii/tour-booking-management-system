# Frontend: kiến trúc, quy tắc và cách thêm code

Áp dụng cho `DongNamTrip.Spa` (React), `DongNamTrip.Web` (BFF + MVC cũ), `DongNamTrip.Client` (HTTP client .NET) và `DongNamTrip.Admin` (WinForms).
Backend: [../Backend/CLAUDE.md](../Backend/CLAUDE.md). Quy tắc đánh dấu **[CI]** do `python tests/architecture/rules.py` hoặc `npm run lint/test` kiểm tra; vi phạm thì CI đỏ. SPA hiện có ESLint và Vitest; chạy các lệnh npm trong `src/Frontend/DongNamTrip.Spa`.

## 1. Mô hình

```
Trình duyệt → SPA (React) → /spa-api (Web, BFF) → API → modules → SQL Server
WinForms Admin → TripApiClient → API
```

- **SPA** chỉ nói chuyện với `/spa-api/*` của Web. Không gọi thẳng API, không giữ token (đăng nhập là cookie HttpOnly do Web quản lý).
- **Web (BFF)**: phục vụ bản build SPA, giữ phiên, kiểm tra antiforgery cho lệnh ghi, chuyển tiếp `proxy/<đường dẫn API>` theo danh sách trắng `AllowedProxy`. Không chứa nghiệp vụ.
- **Backend là nguồn sự thật**: tìm kiếm, lọc, phân trang, tổng, ước tính giá, giới hạn nhập liệu và danh mục (vùng, loại hình, phương thức thanh toán) lấy từ API, chủ yếu `GET /api/meta`. SPA không tự lọc/sắp xếp/cắt dữ liệu và không viết cứng danh mục. **[CI]**
- Web còn giao diện MVC cũ (`?legacy=1`). Tính năng mới làm ở SPA; chỉ sửa MVC khi sửa lỗi hoặc đổi hợp đồng API.

## 2. SPA: cấu trúc

```
src/
  app/        main.tsx (nạp CSS), App.tsx (route, RequireAuth)
  api/        client.ts (fetch + CSRF + lỗi), types.ts (kiểu DTO), meta.ts (rules từ backend), useLoad.ts
  auth/       AuthContext (user, login, logout, hết phiên)
  layouts/    khung trang
  components/ thành phần dùng chung giữa nhiều feature (Catalog, PageState)
  features/   mỗi thư mục = một tính năng/trang (auth, account, tours, destinations, trips, home)
```

## 3. Quy tắc bắt buộc

1. **Mọi lời gọi API đi qua `api/client.ts`** (`api.get/post/put/delete`). Không dùng `fetch()` trong page/component. **[CI]**
2. **Không dùng `localStorage`.** Phiên là cookie HttpOnly. Chỉ bản nháp không nhạy cảm được dùng `sessionStorage` (ví dụ nháp Planner); tuyệt đối không lưu token, mật khẩu, thông tin cá nhân. **[CI]**
3. **Không viết cứng vùng, loại hình, giới hạn hay danh sách mã** (phương thức thanh toán...). Đọc từ `useMeta()`. UI chỉ giữ nhãn hiển thị cho mã; mã lạ hiển thị nguyên mã.
4. **Kiểu DTO ở `api/types.ts`** phải khớp `DongNamTrip.Contracts` (tên camelCase). Giá trị chuỗi cố định dùng union type (`ItemKind`), không dùng `string` trần.
5. **Hook phải sạch**: `react-hooks/rules-of-hooks` và `exhaustive-deps` không có cảnh báo (`--max-warnings 0`). Cần giá trị ổn định cho dependency thì tách ra biến (ví dụ `const userId = user?.id`), không tắt quy tắc.
6. **Không lặp logic giữa trang**: bộ lọc theo query string dùng `useFilterForm`, phân trang dùng `<Pagination>`, trạng thái tải/lỗi dùng `<Loading>`, `<Failure>`, `<PageState>`.
7. **Ngày**: lấy "hôm nay" theo giờ máy người dùng (xem `localDate` trong `PlannerPage`), không dùng `toISOString().slice(0, 10)` (là giờ UTC). Quy tắc nghiệp vụ về ngày do backend quyết định; client chỉ gợi ý `min`.
8. **Kiểm tra dữ liệu ở client chỉ để báo sớm**: backend vẫn là nơi quyết định. Lỗi theo trường từ API (`errors`) hiển thị bằng `errorFor(error, 'Field')`.
9. **Truy cập được (a11y)**: nhãn `label`/`htmlFor` cho input, `role="alert"` cho lỗi, `role="status"` cho trạng thái, trả focus khi đóng `<dialog>`, nút có `aria-label` khi chỉ có biểu tượng.
10. **Route mới** thêm ở `App.tsx` **và** regex fallback SPA trong `Web/Program.cs` (nếu thiếu, tải trực tiếp URL sẽ ra trang MVC/404). Endpoint API mới cần cho SPA thêm vào `AllowedProxy` trong `SpaBridgeController`.

## 4. Quy ước mã

- Một file một component chính, tên file = tên component (`ToursPage.tsx`). Hook đặt tên `useXxx`. Hàm thuần/format để cạnh nơi dùng hoặc trong `components/Catalog.tsx` nếu dùng chung.
- Page: tải dữ liệu bằng `useLoad(key, loader)` (key đổi khi tham số đổi), bọc `loading → <Loading/>`, `error → <Failure/>`, rỗng → khối `empty`. Lệnh ghi: `busy` + `try/catch(messageFor)` + hiển thị lỗi.
- Giữ dòng JSX dễ đọc: tách phần lặp thành component nhỏ khi một khối vượt khoảng 10 dòng hoặc lặp lại ở nơi thứ hai.
- Văn bản hiển thị bằng tiếng Việt; mã/định danh bằng tiếng Anh.
- Không thêm thư viện cho vài dòng code. Thư viện chạy phải có phiên bản cố định (`package-lock.json`) và qua `npm audit --omit=dev`.

## 5. Kiểm thử và công cụ

```powershell
cd src\Frontend\DongNamTrip.Spa
npm ci
npm run lint        # ESLint (hooks + TypeScript), 0 cảnh báo
npm test            # Vitest: client.ts (lỗi, CSRF, 401, mất kết nối)
npm run build       # tsc -b && vite build → ..\DongNamTrip.Web\wwwroot\spa
```

- Trước khi commit: `lint`, `test`, `build` và `python tests/architecture/rules.py`. Thay đổi hành vi UI cần chạy thêm `python tests\spa\spa_runner.py` và `python tests\e2e\browser_runner.py` (Playwright, cần API + DB Development).
- Logic mới không phải JSX (hàm, hook, client) cần kèm test Vitest trong cùng thư mục (`*.test.ts`).
- Định dạng không nằm trong lint. Chưa dùng Prettier vì sẽ viết lại toàn bộ các dòng JSX hiện có; thêm khi cả nhóm đồng ý một lần định dạng lại.

## 6. Checklist

**Thêm trang SPA**: 1. Endpoint backend (xem CLAUDE.md backend) và, nếu mới, thêm vào `AllowedProxy`. 2. Kiểu trong `api/types.ts`. 3. `features/<tên>/<Tên>Page.tsx`. 4. Route trong `App.tsx` + regex fallback trong `Web/Program.cs`. 5. Test e2e nếu là luồng chính. 6. Chạy mục 5.

**Backend thêm danh mục/giới hạn mới cho SPA**: thêm vào `ClientRules` (`Contracts/Models.cs`) và `CatalogService.Rules()`, rồi `Meta` trong `api/types.ts`. Không viết cứng ở SPA.

## 7. Nợ kỹ thuật đã biết

| Nợ | Hướng đi |
|---|---|
| Nhiều dòng JSX rất dài, trang trộn dữ liệu + form + giao diện | Tách hook theo tính năng và component nhỏ khi chạm vào trang đó |
| `api/types.ts` viết tay | Cân nhắc sinh từ OpenAPI hiện có (`http://localhost:5080/openapi/v1.json`) khi quy trình sinh mã được thống nhất; không tự đổi contract chỉ vì mã sinh |
| `useLoad` không hủy request, không cache | Thêm `AbortController` hoặc dùng TanStack Query |
| `FavoriteButton` tải cả `collections` chỉ để biết một mục đã lưu | Endpoint kiểm tra yêu thích hoặc provider dùng chung |
| Hai giao diện (SPA + MVC cũ) và CSS toàn cục dùng chung từ `Web/wwwroot/css` | Quyết định bỏ hoặc đóng băng MVC; chuyển CSS vào SPA |
| Danh sách route lặp ở `App.tsx` và `Web/Program.cs` | Catch-all về `index.html` cho mọi GET HTML ngoài `/spa-api` |
| `SpaBridgeController` lặp khối `catch → 503` | Gom vào filter/middleware |
| Không code-splitting | `React.lazy` theo route khi bundle lớn hơn |

## Luồng đặt tour, tour riêng và quản trị (đã có)

- SPA: `TourDetailPage` (khung đặt tour theo lịch khởi hành), `BookingsPage` (`/Account/Bookings`), `TripsPage` (yêu cầu tour riêng, báo giá, kiểm tra lịch). Giá, số chỗ, khoản cần thu và hạn giữ chỗ do backend tính; SPA chỉ hiển thị.
- Web BFF: route mới phải được thêm vào `AllowedProxy` (và `PublicCatalog` nếu công khai) trong `SpaBridgeController`, và vào regex fallback SPA trong `Web/Program.cs`.
- WinForms: `ToursControl` (SCR16), `BookingsControl` (SCR17; menu "Đơn đặt tour" cho Admin/Operator, "Tài chính" cho Admin/Finance), `PrivateToursControl` (SCR18), `UsersControl` (SCR19), `DashboardControl` (SCR13). Đổi vai trò có hiệu lực sau khi người dùng đăng nhập lại.
