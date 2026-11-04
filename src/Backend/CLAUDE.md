# Backend: kiến trúc, quy tắc và cách thêm code

Tài liệu chốt kiến trúc phần backend (`DongNamTrip.Api` + `DongNamTrip.Modules`). Quyết định gốc: [ADR-001](../../docs/architecture/ADR-001-modular-monolith.md).
Quy tắc đánh dấu **[CI]** được `python tests/architecture/rules.py` kiểm tra tự động (chạy trong job `windows` của CI); vi phạm thì CI đỏ.
Quy tắc còn lại phải tự giữ khi review.

## 1. Mô hình

Modular monolith: một API host mỏng, sáu module trong một project, một database. Luồng gọi:

```
HTTP → Presentation (*Endpoints) → BusinessLogic (*Service) → DataAccess (*Repository) → DbContext → SQL Server
```

- `DongNamTrip.Api/Program.cs`: chỉ cấu hình host (DbContext, rate limit, ProblemDetails, health, OpenAPI), đăng ký và map các `IModule`. Cấu hình/transformer OpenAPI nằm riêng trong `DongNamTrip.Api/OpenApiConfiguration.cs`; không chứa nghiệp vụ.
- `DongNamTrip.Modules/<Module>/`: Catalog, Itinerary, Booking, Quotation, Narration, Administration. Mỗi module có `<Name>Module.cs` implement `IModule` (`Register` đăng ký DI, `MapEndpoints` map route).
- `DongNamTrip.Modules/Persistence/`: `DongNamTripDbContext` dùng chung và entity của schema gốc (nợ kỹ thuật đã biết, xem mục 9).
- `DongNamTrip.Contracts` (thư mục `src/Shared`): DTO và hằng số hợp đồng dùng chung với SPA, Web, Admin. Giữ ổn định URL `/api/...` và hình dạng DTO.

**Tiến độ module hiện tại:** Catalog và Itinerary có repository SQL; Booking có repository/service cho lịch sử `DONDAT`/`GIAODICHTIEN` theo tài khoản và preference phương thức thanh toán, nhưng chưa có tạo đơn/thu tiền/hoàn tiền. Administration có endpoint/service/repository (riêng `SqlAuthentication` còn là nợ truy cập DbContext trực tiếp). Quotation và Narration hiện chỉ có lớp module rỗng, không được tài liệu hóa như endpoint.

```
<Module>/
  <Module>Module.cs          IModule: đăng ký DI + map route
  Presentation/<..>Endpoints.cs   nhận request, gọi service, đổi kết quả sang HTTP
  BusinessLogic/<..>Service.cs    quy tắc nghiệp vụ, kiểm tra, ghép dữ liệu; cổng I<Module>Api nếu module khác cần
  DataAccess/<..>Repository.cs    truy vấn/ghi EF Core, không có quy tắc nghiệp vụ
```

## 2. Trách nhiệm từng lớp

| Lớp | Được làm | Không được làm |
|---|---|---|
| Presentation | Khai báo route (`MapGroup`), binding, `RequireAuthorization`, `WithValidation<T>`, đổi kết quả service thành `Results.*`, lấy `AccountId(user)` | Dùng `DongNamTripDbContext`/EF Core, gọi Repository, chứa quy tắc nghiệp vụ (kiểm tra ngày, danh sách giá trị hợp lệ, so sánh trạng thái...) **[CI]** |
| BusinessLogic | Quy tắc, kiểm tra nghiệp vụ, điều phối nhiều repository, gọi module khác qua `I*Api`, trả kết quả có ý nghĩa (bool, null, record + enum) | Biết về HTTP (`Results`, `HttpContext`), tham chiếu `*Endpoints` **[CI]**, dùng DbContext/EF trực tiếp **[CI]** (trừ file trong danh sách nợ ở mục 9) |
| DataAccess | Truy vấn, ghi, `AsNoTracking`, `ExecuteUpdate/Delete`, xử lý tranh chấp (`DbErrors`), entity + `Apply<Module>Model` | Quy tắc nghiệp vụ, tham chiếu Service/Endpoints **[CI]**, trả `IQueryable` ra ngoài |

Repository chỉ truy cập bảng của chính module. Cần dữ liệu của module khác thì đi qua `I*Api` ở BusinessLogic.

## 3. Quy tắc bắt buộc

1. **Module chỉ gọi nhau qua cổng công khai tên `I*Api`** (ví dụ `ICatalogApi`), đặt trong `BusinessLogic` của module cung cấp. Không dùng Service/Repository/entity riêng của module khác. **[CI]**
2. **Presentation → BusinessLogic → DataAccess**, một chiều. Tầng dưới không tham chiếu tầng trên. **[CI]**
3. **Route đăng ký bằng `MapGroup`**, không gọi `endpoints.MapGet/MapPost/...` trên builder gốc. Quyền đặt ở nhóm (`.RequireAuthorization()`), không lặp từng endpoint. **[CI]**
4. **Không viết chuỗi hợp đồng thẳng trong code**: loại mục (`ItemKinds`), loại lịch sử (`ActivityKinds`), phạm vi xóa (`ActivityScopes`), phương thức thanh toán (`PaymentMethods`), tên rate limit (`RateLimits`). Thêm giá trị mới vào `Vocabulary.cs`/`RateLimits.cs`. **[CI]**
5. **Mỗi module có `<Name>Module.cs` và được thêm vào mảng `modules` trong `Program.cs`.** **[CI]**
6. **Kiểm tra dữ liệu vào**: DataAnnotations/`IValidatableObject` trên DTO + `.WithValidation<T>()` cho endpoint nhận body. Quy tắc nghiệp vụ (vùng hợp lệ, ngày đi không ở quá khứ, mục tồn tại) đặt trong Service.
7. **Không trả lỗi 500 cho tình huống dự đoán được**: hai request đồng thời cùng tạo một bản ghi phải thành công (idempotent), dùng `DbErrors.IsDuplicateKey` trong Repository.
8. **Không tạo/sửa schema từ entity EF.** Schema nằm ở `db/init/*.sql` (xem mục 8).
9. **Giữ ổn định hợp đồng**: không đổi URL, HTTP status, tên/kiểu thuộc tính DTO nếu chưa cập nhật SPA, Web, Admin và test. Thêm trường mới thì được.
10. **Không để lọc/tìm kiếm/phân trang/tính toán ở phía SPA**: backend cung cấp (xem rule 3 trong `rules.py`).

## 4. Quy ước endpoint

```csharp
public static void Map(IEndpointRouteBuilder endpoints)
{
    var items = endpoints.MapGroup("/api/items");                      // nhóm công khai
    items.MapGet("", (ItemService service, CancellationToken ct) => service.ListAsync(ct));

    var mine = items.MapGroup("").RequireAuthorization();              // nhóm cần đăng nhập
    mine.MapPost("", async (ItemDraft draft, ClaimsPrincipal user, ItemService service, CancellationToken ct) =>
        await service.SaveAsync(AccountId(user), draft, ct) is { } item ? Results.Ok(item) : Results.BadRequest())
        .WithValidation<ItemDraft>();                                    // lỗi DataAnnotations → 400 ValidationProblem
}
```

- Handler ngắn: gọi **một** hàm service rồi đổi kết quả sang HTTP. Nếu handler dài hơn vài dòng hoặc có `if` nghiệp vụ, đó là dấu hiệu cần chuyển vào Service.
- Service trả kết quả có ý nghĩa, không trả `IResult`:
  - thành công/thất bại đơn giản: `bool` hoặc `T?` (null = không hợp lệ/không tìm thấy).
  - nhiều trạng thái: `record XResult(Value, XStatus)` + `enum XStatus` (mẫu: `RegistrationResult`, `DestinationSaveResult`).
- Bảng đổi kết quả sang HTTP:

| Tình huống | Status |
|---|---|
| Đọc/ghi thành công | 200 (có body), 204 (không body), 201 khi tạo tài khoản |
| Dữ liệu vào sai | 400 (`ValidationProblem` do filter hoặc `BadRequest()`) |
| Chưa đăng nhập / phiên hết hạn | 401 (do authentication; đăng nhập sai cũng 401, không nói rõ lý do) |
| Không đủ quyền | 403 |
| Không có hoặc không thuộc về người gọi | 404 (không tiết lộ tài nguyên của người khác) |
| Trùng dữ liệu duy nhất (email) | 409 |
| Mật khẩu hiện tại sai | 422 |
| Quá giới hạn tần suất | 429 |

- Lỗi dùng `application/problem+json`: `AddProblemDetails` + `UseExceptionHandler` + `UseStatusCodePages` trong `Program.cs` tự bao phủ 4xx/5xx rỗng. SPA/Web/Admin chỉ dựa vào **status code**, không phân tích body lỗi.
- Rate limit: gắn bằng `RequireRateLimiting(RateLimits.X)`; chính sách khai báo ở `Program.cs`. Endpoint tạo tài khoản và đăng nhập bắt buộc có.
- Người gọi: `ModuleHttp.AccountId(user)` (chỉ dùng trong nhóm đã `RequireAuthorization`). Mọi truy vấn dữ liệu cá nhân phải lọc theo `AccountId` ở Repository.

## 5. Giao tiếp giữa module (cổng `I*Api`)

- Module cung cấp: khai báo `interface I<Module>Api` trong `BusinessLogic`, Service implement, đăng ký `services.AddScoped<I<Module>Api>(sp => ...)` trong `<Module>Module.cs`.
- Module dùng: inject `I<Module>Api` vào Service của mình (ví dụ `AccountAreaService` hỏi `ICatalogApi.ExistsAsync/NamesAsync`). Chỉ hỏi những gì cần, không phơi entity.
- Vòng phụ thuộc giữa hai module là dấu hiệu sai ranh giới; tách ra cổng thứ ba hoặc gộp trách nhiệm.

## 6. Dữ liệu và EF Core

- Một `DongNamTripDbContext` cho cả hệ thống. Mỗi module đóng góp phần mô hình của mình bằng hàm `Apply<Module>Model(ModelBuilder)` trong `DataAccess` (khai báo `partial class DongNamTripDbContext`), được gọi duy nhất từ `Persistence/DongNamTripDbContext.Modules.cs`. Thêm module có bảng riêng: thêm một dòng ở đó.
- Entity của schema gốc ở `Persistence/Entities` (dùng chung). Entity của bảng mở rộng (ví dụ `AccountFavorite`) nằm trong `DataAccess` của module sở hữu.
- Đọc: `AsNoTracking()`. Chỉ lấy cột cần dùng khi danh sách lớn. Không gọi DB trong vòng lặp; gom một truy vấn hoặc dùng `Contains` theo danh sách id.
- Ghi nhiều bước cần nhất quán: giao dịch ở Repository (hoặc Service nếu điều phối nhiều repository). Không bật `EnableRetryOnFailure` trừ khi viết lại các giao dịch thủ công bằng execution strategy.
- Mọi hàm async nhận `CancellationToken` và chuyển tiếp xuống EF.

## 7. Xác thực và phân quyền

- Phiên đăng nhập là Bearer token do `SqlSessionStore` (bộ nhớ) cấp; handler `SqlAuthenticationHandler`. Vai trò nằm trong `AppRoles`; endpoint quản trị dùng `.RequireAuthorization(policy => policy.RequireRole(...))`.
- OpenAPI dùng `Microsoft.AspNetCore.OpenApi`; Swagger UI chỉ đọc `/openapi/v1.json`, không đăng ký bộ sinh tài liệu thứ hai. Chỉ map `/openapi/v1.json` và `/swagger` trong `Development`. Scheme Bearer là token phiên ngẫu nhiên/opaque, không phải JWT: không thêm `AddJwtBearer` hoặc sửa `SqlAuthenticationHandler` vì tài liệu.
- Mỗi route mới thêm `.WithTags(<module>)`, summary/description và `.Produces...` khớp status thực tế. Transformer đọc `IAuthorizeData` từ metadata endpoint **và route group** để chỉ áp dụng security requirement cho route bảo vệ; route công khai không được đánh dấu Bearer. Nêu role nhân viên trên route có `RequireRole`.
- Đăng ký công khai luôn tạo `Customer`; vai trò nhân viên chỉ gán qua Admin hoặc bootstrap Development.
- Không ghi mật khẩu, hash, token vào log, thông báo lỗi hay nhật ký thao tác.
- API chỉ chạy ở môi trường `Development` cho đến khi thay phần xác thực demo.

## 8. Database

- Schema và dữ liệu mẫu: `db/init/*.sql`, chạy theo tên file (`DongNamTrip_Full.sql`, rồi `V001_...`, `V002_...`). Docker và CI dùng đúng các file này.
- Tên bảng/cột tiếng Việt, khớp schema gốc (`TAIKHOAN_YEUTHICH`, `MaTK`, `NgayTao`...). Tên thuộc tính C# có thể tiếng Anh, ánh xạ bằng `HasColumnName`.
- Thay đổi schema = thêm file `V0NN_<mô tả>.sql` mới, chạy lại được nhiều lần (`IF OBJECT_ID(...) IS NULL`), rồi cập nhật entity + `Apply<Module>Model`. Không sửa script đã chạy ở môi trường người khác.
- Thêm cột bắt buộc vào bảng đã có dữ liệu: thêm cột nullable hoặc có DEFAULT trước, dọn dữ liệu, rồi siết ràng buộc.

## 9. Nợ kỹ thuật đã biết (không làm xấu thêm)

| Nợ | Ghi chú |
|---|---|
| `Administration/BusinessLogic/SqlAuthentication.cs` (~600 dòng, nhiều kiểu) dùng DbContext trực tiếp | Nằm trong `LEGACY_DB_IN_BUSINESS_LOGIC` của `rules.py`. Hướng đi: tách `AuthRepository`, tách file theo kiểu (Sessions, Bootstrap, Results), xóa mục khỏi danh sách. Không thêm file mới vào danh sách. |
| DbContext dùng chung | ADR-001 bước 3: tách DbContext theo module. |
| `SqlSessionStore` lưu phiên trong bộ nhớ | Mất phiên khi khởi động lại, không chạy nhiều instance. Đặt sau `ISessionStore` khi cần. |
| Rate limit theo IP | Sau BFF mọi người dùng chung IP của Web; cần forwarded headers trước khi lên môi trường thật. |
| `DateTime.UtcNow/Today` rải rác | Cân nhắc `TimeProvider` để test được. |
| `SqlAuthentication` chưa tách repository và còn truy cập DbContext trực tiếp | Có trong `LEGACY_DB_IN_BUSINESS_LOGIC`; tách AuthRepository/file theo trách nhiệm rồi xóa ngoại lệ, không thêm ngoại lệ mới. |

## 10. Checklist

**Thêm endpoint vào module có sẵn**
1. DTO mới ở `Contracts` (nếu cần) + DataAnnotations. 2. Hàm Repository (chỉ truy vấn). 3. Hàm Service (quy tắc, trả `bool/T?/Result`). 4. Route trong `*Endpoints` thuộc đúng `MapGroup`, đúng quyền, `.WithValidation<T>()` nếu có body. 5. Cập nhật `tests/api/*.py` nếu là hành vi mới. 6. Chạy lệnh ở mục 11.

**Thêm module mới**
1. Thư mục `Presentation/BusinessLogic/DataAccess` + `<Name>Module.cs`. 2. Thêm `new <Name>Module()` vào `Program.cs`. 3. Nếu module khác cần dùng: tạo `I<Name>Api`. 4. Nếu có bảng riêng: script `V0NN` + entity + `Apply<Name>Model` + một dòng trong `DongNamTripDbContext.Modules.cs`. 5. Cập nhật bảng module trong ADR-001.

**Thêm giá trị hợp đồng (loại mục, phương thức thanh toán...)**: sửa `Contracts/Vocabulary.cs`, dùng hằng số ở mọi nơi, cập nhật SPA/Web nếu có chuỗi tương ứng.

## 11. Kiểm tra trước khi commit

```powershell
python tests/architecture/rules.py        # kiến trúc, không cần DB/build
dotnet build .\DongNamTrip.sln --no-restore
python .\tests\api\smoke.py --start       # cần DB Development đã nạp db/init/*.sql
python .\tests\api\auth_screens.py --start
python .\tests\api\account_area.py        # tự khởi động API + Web, không có cờ --start
```

Nếu `rules.py` báo lỗi, thông báo nêu cả cách sửa; sửa code cho đúng quy tắc, đừng nới `rules.py`. Chỉ sửa `rules.py` khi quyết định kiến trúc thay đổi, và phải cập nhật tài liệu này cùng lúc.
