# Đặc tả thiết kế đích sáu module DongNamTrip (để sinh code)

Bản rà soát 08/11/2026 (thay bản 05/11). Đối chiếu repo `K-sewii/tour-booking-management-system` tại commit `9950e2f` (07/11/2026) và báo cáo hiện tại (bản text gửi 08/11, gồm Bảng 1.4 QT01–QT09, US/AC mục 3.3, sơ đồ trạng thái STM-01…STM-11, Bảng 3.6 và Chương 4). Mọi chỗ sửa so với bản 05/11 đều có lý do ghi ở mục 12; năm điểm Duy đã chốt ngày 08/11 ở mục 13, phần báo cáo cần sửa theo ở mục 14.

## 0. Cách dùng tài liệu này

Tài liệu mô tả trạng thái **khi sáu module đã làm xong**. Phần đã có trong repo giữ nguyên tên và chữ ký; phần còn thiếu thiết kế theo `src/Backend/CLAUDE.md` để đưa thẳng cho công cụ sinh code. Bộ hình C3, C4 đi kèm vẽ theo bản 05/11; các hình cần vẽ lại sau bản rà này nằm ở mục 11.

| Ký hiệu | Nghĩa |
|---|---|
| **[có]** | Lớp hoặc route đã có trong repo, không sửa |
| **[mở rộng]** | Lớp đã có, thêm phương thức (phương thức mới có dấu ★) |
| **[mới]** | Chưa có, cần sinh |
| (giả định) | Con số tài liệu nghiệp vụ chưa chốt; nhóm xác nhận trước khi sinh |

### 0.1 Hiện trạng repo (`9950e2f`)

| Module | Đã có | Còn phải sinh |
|---|---|---|
| Catalog | Địa điểm, hoạt động, suất (quản trị đủ); đọc tour; `ICatalogApi` 4 phương thức | Quản trị tour và lịch khởi hành (UC16); giữ, trả chỗ; mở rộng `ICatalogApi` |
| Itinerary | Xem trước, liệt kê, lưu (`POST`), ghi phiên bản 1; chỉ một ngày (`NgayThu = 1`), không có suất | Sửa, xóa, chọn suất, kiểm tra, phiên bản cho báo giá (UC06–UC08) |
| Booking | Đọc lịch sử đơn, giao dịch (`/api/account/payments`), lưu lựa chọn thanh toán | Toàn bộ vòng đời đơn, thu hoàn, hết hạn giữ chỗ (UC11–UC13, UC19, UC20) |
| Quotation | Lớp `QuotationModule` rỗng | Toàn bộ (UC09, UC10, UC17, UC18) |
| Narration | Đủ biên tập, bản dịch, audio, nghe, `INarrationApi` | Chỉ cần Booking gọi cổng; nghe và quyền nghe cấp tay qua `/api/internal/listen-rights` đã chạy |
| Administration | Xác thực, phiên, khu tài khoản, ghi nhật ký | Quản lý người dùng, vai trò, đọc nhật ký, thống kê, `NotifyAsync` (UC24) |

Chưa có: bảng nào ghi `DONDAT`, `GIAODICHTIEN`, `BAOGIA`, `HANGMUCBAOGIA`, `YEUCAUTOURRIENG`, `XACMINHDICHVU`, `LICHKHOIHANH`, `LICHTRINH_SUAT`; V003; hằng trạng thái đơn, báo giá, yêu cầu, lịch; khóa cấu hình `HoldHours`, `DepositRate`, `ListenDays`, `BufferMinutes`; project test .NET (kiểm thử backend hiện chạy bằng `tests/api/*.py` và `tests/architecture/rules.py`).

Use case theo code: xong UC01, UC02, UC03, UC14, UC15, UC21, UC22; một phần UC04, UC05, UC07, UC08, UC12, UC23, UC24; chưa UC06, UC09, UC10, UC11, UC13, UC16, UC17, UC18, UC19, UC20.

Giao diện: WinForms có thật Tổng quan, Địa điểm, Hoạt động và suất, Thuyết minh và audio; Tour chỉ đọc (nút thêm vô hiệu); Đơn, Yêu cầu và báo giá, Tài chính, Tài khoản là `ModuleControl` giữ chỗ. SPA chưa có luồng đặt tour, yêu cầu tour riêng, báo giá, hủy đơn; nút "Đặt tour" ở chi tiết tour đang vô hiệu.

### 0.2 Vai trò trong code và tác nhân trong báo cáo

| Vai trò (`AppRoles`) | Tác nhân báo cáo (Bảng 3.1) | Quyền trong tài liệu này |
|---|---|---|
| `Customer` | Khách hàng | Tài khoản đăng nhập thao tác trên dữ liệu của chính mình; đăng ký công khai luôn tạo `Customer` |
| `Operator` | Nhân viên tư vấn và điều hành (kể cả khi được cấp `Finance`) | Hoạt động, suất, tour, lịch khởi hành, đơn, yêu cầu tour riêng, báo giá; ghi thu và hoàn khi được cấp `Finance` |
| `Editor` | Biên tập viên nội dung | Địa điểm, hoạt động, suất, soạn và dịch thuyết minh |
| `Publisher` | Biên tập viên có quyền công bố | Duyệt, tạo audio, công bố (đã có) |
| `Finance` | Không còn là tác nhân riêng: gộp vào Nhân viên tư vấn và điều hành (Duy chốt 08/11, phương án A) | Quyền cấp thêm cho nhân viên để ghi nhận thu và hoàn (UC20); một tài khoản có thể có nhiều vai trò (`TAIKHOAN_VAITRO` n–n), nên Operator được cấp thêm `Finance`. Code giữ vai trò này để vẫn tách được việc ghi tiền |
| `Admin` | Quản trị viên | **Đủ mọi quyền** (Duy chốt 08/11): mọi route của Operator, Editor, Publisher và Finance, cộng người dùng, vai trò, nhật ký, thống kê |

Trong báo cáo chỉ còn một tác nhân nhân viên cho UC19 và UC20, và Quản trị viên kế thừa tác nhân đó nên cũng thực hiện UC20. Trong code, `Admin` có đủ quyền: mọi route đánh dấu `Finance` đều chấp nhận `Admin`. Vì vậy `MainForm.cs` (hiện chỉ hiện menu Tài chính khi `Has(Finance)`) phải sửa để Admin cũng thấy menu này.

### 0.3 Quy ước chung cho mọi lớp mới (lấy từ `CLAUDE.md`, không lặp lại ở từng mục)

- Ba lớp trong mỗi module: `Presentation/*Endpoints.cs` (static, `MapGroup`, `RequireAuthorization`, `.WithTags`, `.WithSummary`, `.Produces<T>()`, `WithValidation<T>()`), `BusinessLogic/*Service.cs` (quy tắc nghiệp vụ), `DataAccess/*Repository.cs` (EF Core; chỉ module chủ bảng được ghi bảng đó).
- Module khác chỉ gọi nhau qua cổng `I*Api` đặt ở `BusinessLogic/` của module chủ; đăng ký `services.AddScoped<IXApi, XService>()` trong `XModule.Register`. `IHostedService` cũng đăng ký trong `Register`, không phải `MapEndpoints`.
- Mọi phương thức async nhận `CancellationToken ct` ở cuối (hình không ghi để gọn).
- Chuỗi trạng thái và loại là hằng trong `Vocabulary.cs` (mục 1); không viết chuỗi trực tiếp trong code, kể cả trong biểu thức EF.
- Service trả `OpResult<T>`. `ModuleHttp.ToHttp` hiện chỉ ánh xạ `Ok → 200`, `Invalid → 400`, `Forbidden → 403`, `NotFound → 404`, còn lại `409`. Tài liệu cần thêm hai thứ nhỏ ở bước 1 (mục 10): `OpStatus.Unprocessable → 422` và overload `ToHttp(result, onOk)` để trả 201. Service có phân biệt "tạo mới" và "đã có" trả `OpResult<Fresh<T>>` với `record Fresh<T>(T Value, bool IsNew)` đặt cạnh `OpResult`; endpoint trả 201 khi `IsNew`, 200 khi không.
- Thao tác của nhân viên làm đổi giá, đơn, khoản thu, báo giá, quyền hoặc nội dung công bố phải ghi `IAdministrationApi.RecordAsync(new AuditEntry(...))` (QT09). Chữ ký thật là `AuditEntry(int ActorId, string Action, string ObjectType, object ObjectId, string? Reason = null, object? Before = null, object? After = null)`: `Action` là nhãn tiếng Việt như các module đang dùng ("Tạo địa điểm", "Cấp quyền nghe"), `ObjectType` là tên bảng (`"DIADIEM"`, `"QUYENNGHE"`). Tác vụ nền không có người thực hiện ghi `ActorId = null`; muốn vậy `AuditEntry.ActorId` và `AuditRepository.AddAsync(actorId)` đổi sang `int?` (cột `NHATKYTHAOTAC.MaTK` đã cho NULL). Bảng nhãn đầy đủ ở mục 9.2.
- Thay đổi lược đồ chỉ qua `db/init/V003_booking_quotation.sql` chạy lại được (mục 9).
- Cấu hình đọc bằng `IConfiguration.GetValue<T>(khóa, mặc định)` với mặc định ghi trong mục 9.3, nên chưa sửa `appsettings.json` vẫn chạy.

## 1. Từ vựng trạng thái thêm vào `Vocabulary.cs`

Repo dùng chuỗi tiếng Anh cho trạng thái (`ContentStatus.Published`). Hằng mới khớp sơ đồ trạng thái của báo cáo (cột "Tên trong báo cáo"). Hai giá trị `Draft` mặc định của `LICHKHOIHANH` và `BAOGIA` trong DDL cũ **không dùng nữa** vì sơ đồ STM-06 và STM-08 không có trạng thái Nháp: lịch khởi hành tạo ra là `Open`, báo giá tạo ra là `Sent` (V003 đổi DEFAULT và CHECK).

| Lớp hằng | Hằng = giá trị | Tên trong báo cáo | Bảng, cột |
|---|---|---|---|
| `TourStatus` (dùng lại `ContentStatus`) | `Draft`, `Published`, `Inactive` | Nháp, Đã công bố, Ngừng | TOUR.TrangThai |
| `DepartureStatus` | `Open`, `Closed`, `Inactive` | Mở, Đóng, Ngừng | LICHKHOIHANH.TrangThai |
| `OrderStatus` | `Pending`, `Confirmed`, `CancelRequested`, `Cancelled`, `Expired`, `Completed` | Chờ xử lý, Đã xác nhận, Chờ hủy, Đã hủy, Hết hạn, Hoàn thành | DONDAT.TrangThai |
| `BookingSources` | `Departure`, `Quote` | Tour có sẵn, Tour riêng | giá trị dẫn xuất (`MaLKH` hoặc `MaBG` khác rỗng), không lưu cột |
| `MoneyKinds` | `Receipt`, `Refund` | Thu, Hoàn | GIAODICHTIEN.LoaiGD |
| `QuoteRequestStatus` | `Submitted`, `Verifying`, `Quoted`, `Accepted`, `Cancelled` | Chờ tiếp nhận, Đang xác minh, Đã báo giá, Đã chốt, Đã hủy | YEUCAUTOURRIENG.TrangThai |
| `QuoteStatus` | `Sent`, `Accepted`, `Expired`, `Superseded` | Đã gửi, Đã chấp nhận, Hết hạn, Đã thay thế | BAOGIA.TrangThai |
| `VerificationResults` | `Available`, `Unavailable`, `Substitute` | Đáp ứng, Không đáp ứng, Thay thế | XACMINHDICHVU.KetQua |
| `AccountStatus` | `Active`, `Locked` | Hoạt động, Khóa | TAIKHOAN.TrangThai |
| `CancelPolicyDefaults` | `Standard` = "Áp dụng điều kiện hủy chung của DongNamTrip; nhân viên xác nhận khoản hoàn." | – | điền vào `DONDAT.DieuKienHuy` khi tour chưa có điều kiện hủy |

Suất hoạt động dùng lại `ContentStatus` (`Published`, `Inactive`). Mỗi lớp hằng có `All` và `Label(string)` giống `ContentStatus`. `SeatHold` không phải hằng chuỗi: là `enum SeatHold { Held, Full, Closed, NotFound }` đặt trong `DongNamTrip.Modules.Catalog` cùng `record HoldResult(SeatHold Status, int Remaining)`.

Quy tắc đọc sơ đồ trạng thái đã dùng khi viết mục 4, 6, 7, và ba chỗ tài liệu khác sơ đồ báo cáo hiện tại, Duy đã chốt sửa báo cáo theo tài liệu (08/11, mục 13): `Expired → Confirmed` khi có thu muộn (theo UC20 và AC2 của US20; STM-07 bản cũ coi Hết hạn là trạng thái cuối), `Quoted → Verifying` khi khách đề nghị đổi báo giá (theo UC10; STM-04 bản cũ chưa có mũi tên), hoàn thành đơn do nhân viên thao tác thay vì `when(chuyến đi kết thúc)` (thời lượng tour là chữ tự do, không tính được ngày kết thúc). Hình STM-04, STM-07 đã vẽ lại (mục 14).

## 2. Cổng giữa các module

| Cổng | Module chủ, lớp hiện thực | Module gọi | Phương thức |
|---|---|---|---|
| `ICatalogApi` **[mở rộng]** | Catalog, `CatalogService` | Itinerary, Booking, Administration, Narration | có: `ExistsAsync`, `PlaceExistsAsync`, `NamesAsync`, `DestinationsAsync`; ★ `SlotsAsync(slotIds): List<SlotDetail>`, ★ `HoursAsync(destinationIds): Dictionary<int, DestinationHours>`, ★ `DepartureAsync(id): DepartureInfo?`, ★ `DeparturesAsync(ids): Dictionary<int, DepartureInfo>`, ★ `HoldSeatsAsync(departureId, guests): HoldResult`, ★ `ReleaseSeatsAsync(departureId, guests): Task` |
| `IItineraryApi` **[mới]** | Itinerary, `ItineraryVersionService` | Quotation | `CreateVersionAsync(itineraryId, accountId): OpResult<ItineraryVersionRef>` (khóa dòng lịch trình bằng `UPDLOCK` trong giao dịch của người gọi), `VersionAsync(versionId): ItineraryVersion?`, `CheckVersionAsync(versionId): ItineraryCheck`, `ProgramAsync(versionId): List<ProgramStop>` |
| `IBookingApi` **[mới]** | Booking, `BookingService` | Quotation | `CreateFromQuoteAsync(QuoteAcceptance acceptance, int actorId): OpResult<int>` (trả mã đơn); `OrderIdByQuoteAsync(quoteId): int?` |
| `INarrationApi` **[có]** | Narration, `ListenService` | Booking | `GrantListenRightAsync(ListenRightGrant grant, actorId): OpResult<ListenRightGrant>`, `RevokeListenRightAsync(orderId, actorId): Task` |
| `IAdministrationApi` **[mở rộng]** | Administration, `AuditService` | mọi module | có: `RecordAsync(AuditEntry)`; ★ `NotifyAsync(accountId, title, body): Task` (ghi TAIKHOAN_THONGBAO, cắt `title` 160, `body` 500 ký tự), ★ `HistoryAsync(objectType, objectId): List<AuditRow>`, ★ `AccountNamesAsync(accountIds): Dictionary<int, string>` |

Phụ thuộc giữa module sau khi sinh: Itinerary → Catalog; Booking → Catalog, Narration; Quotation → Itinerary, Booking; Narration → Catalog (đã có); mọi module → Administration. Riêng **Catalog ⇄ Administration đã phụ thuộc hai chiều trong repo** (Catalog ghi nhật ký qua `IAdministrationApi`; `AccountAreaService` hỏi tên qua `ICatalogApi`). `rules.py` không cấm và hai chiều đi qua hai dịch vụ khác nhau nên không có vòng DI; tài liệu giữ nguyên và không thêm cạnh mới nào làm vòng nặng hơn (Administration không gọi thêm gì ở Catalog).

Giao dịch xuyên module: các module dùng chung một `DongNamTripDbContext` theo phạm vi request, nên lớp điều phối mở giao dịch bằng `BeginAsync()` của repository mình rồi gọi cổng module kia; ghi của module kia nằm trong cùng giao dịch. Cổng không tự mở giao dịch; gọi riêng lẻ ngoài giao dịch thì mỗi lệnh ghi tự commit. Ba luồng dùng cách này: đặt chỗ (Booking giữ chỗ ở Catalog), chấp nhận báo giá (Quotation tạo đơn ở Booking), gửi yêu cầu tour riêng (Quotation tạo phiên bản ở Itinerary). Tác vụ nền xử lý từng đơn hoặc từng báo giá trong một giao dịch riêng để việc đổi trạng thái và trả chỗ không lệch nhau.

## 3. Hợp đồng dữ liệu (DTO) thêm vào `DongNamTrip.Contracts`

```csharp
// Catalog
public record SlotDetail(int Id, int ActivityId, string ActivityName, int DestinationId, DateOnly Date, TimeOnly Start, TimeOnly End, string Status, int DurationMinutes);
public record DestinationHours(TimeOnly? Opens, TimeOnly? Closes, int VisitMinutes);
public record DepartureInfo(int Id, int TourId, string TourName, DateTime StartsAt, decimal Price, int Capacity, int Held, int Remaining, string Status, string? CancelPolicy);
public record AdminTour(int Id, string Name, string Region, string Category, string Duration, decimal ReferencePrice, string Status, int Stops, int OpenDepartures);
public record AdminTourDetail(AdminTour Tour, string Summary, string? CancelPolicy, IReadOnlyList<TourStop> Stops, IReadOnlyList<AdminDeparture> Departures);
public record TourStop(int DestinationId, string DestinationName, int Day, int Order);
public record AdminDeparture(int Id, int TourId, DateTime StartsAt, decimal Price, int Capacity, int Held, string Status);
public record TourForm(string Name, string Region, string Category, string Duration, decimal ReferencePrice, string Summary, string? CancelPolicy);
public record TourStopForm(int DestinationId, int Day, int Order);
public record DepartureForm(DateTime StartsAt, decimal Price, int Capacity);
public record StatusRequest(string Status, string? Reason = null);

// Itinerary  (ItineraryDraft [có] đã biểu diễn nhiều ngày qua `Stops[].Day` (1–30); không thêm `Days`, xem mục 13 điểm 3)
public record SlotPick(int DestinationId, int SlotId, string? Note);
public record DraftCheckRequest(ItineraryDraft Draft, IReadOnlyList<SlotPick> Picks);
public record StopPlan(int DestinationId, int Day, int Order, int VisitMinutes, TimeOnly? Opens, TimeOnly? Closes);
public record ItineraryDetail(Itinerary Itinerary, IReadOnlyList<StopPlan> Stops, IReadOnlyList<SlotDetail> Slots, bool HasRequests);
public record ScheduleWarning(string Code, int? DestinationId, int? SlotId, string Message); // Code: Overlap | WrongDate | Inactive | Duplicate | OutOfHours | Incomplete
public record ItineraryCheck(bool Fits, bool Complete, IReadOnlyList<ScheduleWarning> Warnings);
public record ItineraryVersionRef(int VersionId, int Number, DateTime CreatedAt);
public record ItineraryVersion(int VersionId, int ItineraryId, int AccountId, int Number, DateOnly StartDate, int Guests, string ContentJson);
// ContentJson: { name, startDate, guests, destinationIds, days, slots:[{destinationId, slotId, note}] }; bản 1 do POST hiện có chỉ có bốn khóa đầu.

// Booking
public record BookingForm(int DepartureId, int Guests, string Contact);
public record BookingInfo(int Id, string Source, int? DepartureId, int? QuoteId, string Title, DateTime PlacedAt, int Guests, decimal AgreedPrice, decimal RequiredDeposit, DateTime? HoldUntil, DateOnly? TripDate, string Status);
public record BookingDetail(BookingInfo Booking, string Contact, string CancelPolicy, string? CancelReason, decimal Paid, decimal Refunded, decimal Outstanding, decimal Refundable, IReadOnlyList<MoneyEntry> Transactions,
    IReadOnlyList<ProgramStop>? Program = null, IReadOnlyList<BookingAdjustment>? Adjustments = null);
public record ProgramStop(int Day, string Place);                                   // điểm của chương trình đã chốt (US12 AC1)
public record BookingAdjustment(DateTime At, string Change, string Reason, decimal? Price); // lịch sử điều chỉnh (US12 AC3)
public record CancelRequest(string Reason);
public record BookingFilter(string? Status, DateTime? From, DateTime? To, string? Q, bool? Overdue, int? Page, int? Size);
public record AdminBooking(int Id, int AccountId, string Customer, string Source, string Title, DateTime PlacedAt, int Guests, decimal AgreedPrice, decimal Paid, string Status);
public record AdminBookingDetail(AdminBooking Booking, BookingDetail Detail, IReadOnlyList<string> MissingForConfirm);
public record AdjustForm(string Change, string Reason, string CustomerConsent, decimal? NewPrice);
public record CancelDecision(bool Approve, string Reason);
public record MoneyForm(string Kind, decimal Amount, string Reference, DateTime At);
public record QuoteAcceptance(int QuoteId, int RequestId, int AccountId, int Guests, decimal Amount, decimal Deposit, string CancelPolicy, DateOnly TripStart, string Contact);

// Quotation
public record QuoteRequestForm(int ItineraryId, int Guests, string Contact, string? Needs);
public record QuoteRequestInfo(int Id, int ItineraryId, int VersionId, int VersionNumber, DateTime SentAt, int Guests, string Status, int? CurrentQuoteId);
public record QuoteItemForm(string Description, string Unit, int Quantity, decimal UnitPrice, bool IncludedInPackage);
public record QuoteForm(IReadOnlyList<QuoteItemForm> Items, decimal Deposit, string CancelPolicy, DateTime ValidUntil);
public record QuoteInfo(int Id, int RequestId, int VersionId, decimal Total, decimal Deposit, string CancelPolicy, DateTime ValidUntil, string Status, IReadOnlyList<QuoteItemForm> Items);
public record QuoteRequestDetail(QuoteRequestInfo Request, ItineraryVersion Version, QuoteInfo? Quote, int? OrderId);
public record VerificationForm(string Item, string Provider, DateTime At, string Result, string? Note);
public record VerificationInfo(int Id, string Item, string Provider, DateTime At, string Result, string? Note);
public record AdminQuoteRequestDetail(QuoteRequestInfo Request, string Contact, string? Needs, ItineraryVersion Version, ItineraryCheck Check, IReadOnlyList<VerificationInfo> Verifications, IReadOnlyList<QuoteInfo> Quotes, IReadOnlyList<string> ChangeNotes);
public record QuoteRequestFilter(string? Status, DateTime? From, DateTime? To, int? Page, int? Size);
public record QuoteAccepted(int QuoteId, int OrderId, decimal Amount, decimal Deposit);
public record ChangeRequest(string Note);

// Administration
public record UserFilter(string? Q, string? Role, string? Status, int? Page, int? Size);
public record AdminUser(int Id, string Email, string FullName, string? Phone, string Status, IReadOnlyList<string> Roles, DateTime CreatedAt);
public record AdminUserDetail(AdminUser User, int ActiveSessions, IReadOnlyList<AuditRow> RecentActions);
public record StaffForm(string Email, string FullName, string? Phone, IReadOnlyList<string> Roles);
public record StaffCreated(AdminUser User, string TemporaryPassword);
public record RolesRequest(IReadOnlyList<string> Roles);
public record TempPassword(int AccountId, string Password);
public record AuditFilter(DateTime? From, DateTime? To, string? ObjectType, int? ActorId, int? Page, int? Size);
public record AuditRow(long Id, int? ActorId, string ActorName, DateTime At, string Action, string ObjectType, string ObjectId, string? Reason, string? Before, string? After);
public record OperationSummary(DateTime From, DateTime To, IReadOnlyList<StatusCount> Orders, decimal OrderValue, decimal Received, decimal Refunded, IReadOnlyList<StatusCount> Requests);
public record StatusCount(string Status, int Count);
```

Đã có sẵn trong repo và dùng lại không sửa: `PagedResult<T>`, `Itinerary`, `ItineraryPreview`, `Destination`, `Tour`, `TourDetail`, `TourEstimate`, `MoneyEntry(Id, Kind, Amount, At, Reference)`, `PaymentEntry`, `ListenRightGrant(OrderId, AccountId, From, To)`, `AuditEntry` (Modules), `AdminActivity`, `AdminDestination`, `DeleteResult`. Tên trường DTO đặt tiếng Anh theo kiểu các DTO đang có; cột bảng giữ tên tiếng Việt trong entity (`TrongGoi` ↔ `IncludedInPackage`, `KhoanCanThu` ↔ `RequiredDeposit`, `LienHe` ↔ `Contact`).

## 4. Module Catalog (PH1, kèm tour và lịch khởi hành của PH3)

Hình 4.5.1. Catalog đã có địa điểm, hoạt động, suất và phần đọc tour; phần cần sinh là quản trị tour, lịch khởi hành (SCR16, UC16) và giữ, trả chỗ cho Booking.

### 4.1 Lớp

| Lớp | Loại | | Trường | Phương thức (★ là phương thức mới) |
|---|---|---|---|---|
| `CatalogModule` | IModule | [có] | – | `+ Register(services, configuration): void`<br>`+ MapEndpoints(endpoints): void Map: Catalog, PlaceAdmin, Activity, TourAdmin` |
| `CatalogEndpoints` | static | [mở rộng] | `/api/meta, /api/destinations…, /api/tours…` | `+ Map(endpoints): void` |
| `PlaceAdminEndpoints` | static | [có] | `/api/admin/destinations…` | `+ Map(endpoints): void` |
| `ActivityEndpoints` | static | [có] | `/api/admin/activities…, /api/admin/slots…` | `+ Map(endpoints): void` |
| `TourAdminEndpoints` | static | [mới] | `/api/admin/tours…, /api/admin/departures…` | `+ Map(endpoints): void` |
| `CatalogService` | class | [mở rộng] | `- repository: CatalogRepository` | `+ SearchToursAsync(q, region, category, sort, page, size)`<br>`+ TourDetailAsync(id): TourDetail?`<br>`+ EstimateAsync(id, guests): TourEstimate?`<br>`+ DeparturesAsync(tourId): List<DepartureInfo>` ★<br>`+ SlotsAsync(slotIds): List<SlotDetail>` ★<br>`+ HoursAsync(destinationIds): Dictionary<int, DestinationHours>` ★<br>`+ DepartureAsync(id): DepartureInfo?` ★<br>`+ DeparturesAsync(ids): Dictionary<int, DepartureInfo>` ★<br>`+ HoldSeatsAsync(departureId, guests): HoldResult` ★<br>`+ ReleaseSeatsAsync(departureId, guests)` ★ |
| `ICatalogApi` | interface – module Catalog | [mở rộng] | – | `+ ExistsAsync(kind, id): bool`<br>`+ PlaceExistsAsync(id): bool`<br>`+ NamesAsync(kind, ids): Dictionary`<br>`+ DestinationsAsync(ids): List<Destination>`<br>`+ SlotsAsync(slotIds): List<SlotDetail>` ★<br>`+ HoursAsync(destinationIds)` ★<br>`+ DepartureAsync(id)`, `+ DeparturesAsync(ids)` ★<br>`+ HoldSeatsAsync(departureId, guests): HoldResult` ★<br>`+ ReleaseSeatsAsync(departureId, guests): Task` ★ |
| `PlaceAdminService` | class | [có] | `- repository: PlaceAdminRepository`<br>`- administration: IAdministrationApi` | `+ ListAsync(): List<AdminDestination>`<br>`+ SaveAsync(id, form, actor): OpResult`<br>`+ SetStatusAsync(id, status, actor)`<br>`+ DeleteAsync(id, actor): OpResult<DeleteResult>` |
| `ActivityService` | class | [có] | `- repository: ActivityRepository`<br>`- administration: IAdministrationApi` | `+ SaveAsync(id, form, actor): OpResult<AdminActivity>`<br>`+ SetActivityStatusAsync(id, status, confirm, actor)`<br>`+ CreateSlotAsync(activityId, form, actor)`<br>`+ UpdateSlotAsync(slotId, form, actor)`<br>`+ SetSlotStatusAsync(id, status, confirm, actor)` |
| `TourAdminService` | class | [mới] | `- repository: TourAdminRepository`<br>`- administration: IAdministrationApi` | `+ ListAsync(status?): List<AdminTour>`<br>`+ GetAsync(id): AdminTourDetail?`<br>`+ SaveAsync(id?, form: TourForm, actor): OpResult<AdminTour>`<br>`+ SetPlacesAsync(id, stops: List<TourStopForm>, actor): OpResult<AdminTourDetail>`<br>`+ SetStatusAsync(id, status, actor): OpResult<AdminTour>`<br>`+ DeparturesAsync(tourId): List<AdminDeparture>`<br>`+ SaveDepartureAsync(tourId, id?, form: DepartureForm, actor): OpResult<AdminDeparture>`<br>`+ DeactivateDepartureAsync(id, actor): OpResult<AdminDeparture>` |
| `CatalogRepository` | class | [mở rộng] | `- db: DongNamTripDbContext` | `+ SearchToursAsync(…), TourAsync(id)`<br>`+ DeparturesAsync(tourId, now): List<DepartureInfo>` ★<br>`+ DeparturesAsync(ids): Dictionary<int, DepartureInfo>` ★<br>`+ SlotsAsync(slotIds): List<SlotDetail>` ★<br>`+ HoursAsync(destinationIds)` ★<br>`+ TryHoldAsync(departureId, guests, now): int` ★<br>`+ ReleaseAsync(departureId, guests): int` ★<br>`+ StateAsync(departureId): (status, capacity, held)?` ★ |
| `PlaceAdminRepository` | class | [có] | `- db: DongNamTripDbContext` | `+ ListAsync(), GetAsync(id), SaveAsync(id, form)`<br>`+ SetStatusAsync(id, status), DeleteAsync(id)` |
| `ActivityRepository` | class | [có] | `- db: DongNamTripDbContext` | `+ ListAsync(destinationId), SaveAsync(id, form)`<br>`+ SaveSlotAsync(slotId, activityId, form)`<br>`+ RangesAsync(activityId, date, excludeSlotId)` |
| `TourAdminRepository` | class | [mới] | `- db: DongNamTripDbContext` | `+ ListAsync(status?), GetAsync(id)`<br>`+ NameExistsAsync(name, region, exceptId?): bool`<br>`+ SaveAsync(id?, form): AdminTour`<br>`+ ReplacePlacesAsync(id, stops): void` (xóa hết rồi thêm, trong một giao dịch)<br>`+ SetStatusAsync(id, status): bool`<br>`+ HasHeldSeatsAsync(tourId): bool`<br>`+ DepartureAsync(id): AdminDeparture?`<br>`+ SaveDepartureAsync(tourId, id?, form): AdminDeparture`<br>`+ SetDepartureStatusAsync(id, status): bool` |
| `IAdministrationApi` | interface – module Administration | [mở rộng] | – | `+ RecordAsync(entry: AuditEntry): Task`<br>`+ NotifyAsync(accountId, title, body): Task` ★ |

### 4.2 Endpoint

| Phương thức, route | Vai trò | Vào → Ra | Mã lỗi | UC, SCR | |
|---|---|---|---|---|---|
| `GET /api/meta`, `/api/destinations…`, `/api/tours`, `/api/tours/search`, `/api/tours/{id}`, `/api/tours/{id}/estimate` | công khai | như hiện tại | | UC02, UC03 | [có] |
| `/api/admin/destinations…` (6 route), `/api/admin/activities…`, `/api/admin/slots…` (7 route) | Admin, Editor, Operator | như hiện tại | | UC14, UC15 | [có] |
| `GET /api/tours/{id}/departures` | công khai | → `List<DepartureInfo>` (chỉ lịch `Open` của tour `Published`, chưa khởi hành) | 404 | UC02, UC11, SCR05 | [mới] |
| `GET /api/admin/tours?status=` | Admin, Operator | → `List<AdminTour>` | | UC16, SCR16 | [mới] |
| `GET /api/admin/tours/{id}` | Admin, Operator | → `AdminTourDetail` | 404 | UC16 | [mới] |
| `POST /api/admin/tours` | Admin, Operator | `TourForm` → 201 `AdminTour` (`Draft`) | 400, 409 trùng (tên, khu vực) | UC16 | [mới] |
| `PUT /api/admin/tours/{id}` | Admin, Operator | `TourForm` → `AdminTour` | 400, 404, 409 trùng (tên, khu vực) | UC16 | [mới] |
| `PUT /api/admin/tours/{id}/places` | Admin, Operator | `List<TourStopForm>` → `AdminTourDetail` | 400 địa điểm lặp, thứ tự trùng hoặc địa điểm chưa `Published`; 404; 409 tour đã có chỗ được giữ | UC16 | [mới] |
| `POST /api/admin/tours/{id}/status` | Admin, Operator | `StatusRequest` (`Published`, `Inactive`) → `AdminTour` | 404, 409 chuyển không hợp lệ, 422 thiếu trường hoặc chưa có điểm | UC16 | [mới] |
| `GET /api/admin/tours/{id}/departures` | Admin, Operator | → `List<AdminDeparture>` | 404 | UC16 | [mới] |
| `POST /api/admin/tours/{id}/departures` | Admin, Operator | `DepartureForm` → 201 `AdminDeparture` (`Open`) | 400, 404, 422 tour chưa `Published` | UC16 | [mới] |
| `PUT /api/admin/departures/{id}` | Admin, Operator | `DepartureForm` → `AdminDeparture` | 400, 404, 409 sức chứa < số chỗ đã giữ hoặc đổi ngày giờ khi đã có chỗ giữ | UC16 | [mới] |
| `POST /api/admin/departures/{id}/deactivate` | Admin, Operator | → `AdminDeparture` | 404, 409 đã `Inactive` | UC16 | [mới] |

### 4.3 Quy tắc

- Tour (STM-05): `Draft → Published` khi đủ tên, khu vực, loại hình, thời lượng, giá tham khảo, tóm tắt và có ít nhất một điểm (QT01); khu vực và loại hình thuộc `CatalogOptions`. `Published ⇄ Inactive` ("công bố lại"). Ngừng tour không xóa gì và không đụng đến đơn cũ (AC3 của US16, QT09); tour không còn `Published` thì lịch của nó không nhận giữ chỗ mới (điều kiện nằm trong `TryHoldAsync`), nên không cần buộc ngừng từng lịch trước. Sửa tên, tóm tắt, giá tham khảo được phép và ghi nhật ký.
- Điểm dừng của tour: `TOUR_DIADIEM` có khóa chính (tour, địa điểm) nên một địa điểm xuất hiện tối đa một lần; địa điểm phải `Published`; (ngày thứ, thứ tự) không trùng. `ReplacePlacesAsync` xóa hết rồi thêm trong một giao dịch. Tour đã có lịch được giữ chỗ thì chặn sửa chương trình (409): theo AC3 của US16 muốn đổi chương trình sau khi có khách phải đi qua điều chỉnh đơn (UC19).
- Trùng tên: kiểm tra `NameExistsAsync(name, region)` không phân biệt hoa thường, dấu (CSDL chưa có ràng buộc duy nhất; chấp nhận rủi ro tranh chấp hiếm vì chỉ nhân viên thao tác).
- Lịch khởi hành (STM-06): tạo ra là `Open` khi đủ ngày giờ (sau hiện tại), giá, sức chứa (>0) và tour đang `Published` (AC2 của US16, UC16). `Open → Closed` **tự động** khi `SoChoDaGiu = SucChua`; `Closed → Open` tự động khi giải phóng chỗ làm còn chỗ trống; `Open | Closed → Inactive` (một chiều) qua `deactivate`. Không có thao tác đóng tay. Chỉ lịch `Open` của tour `Published`, `StartsAt > now` mới nhận giữ chỗ.
- Sửa lịch: `Capacity >= SoChoDaGiu`, nếu không 409 kèm số chỗ đã giữ (AC2 của US16). Khi `SoChoDaGiu > 0` không đổi `StartsAt` (409); đổi `Price` vẫn được vì đơn đã lưu giá chốt (QT06). Sau khi lưu, trạng thái tính lại: lịch chưa `Inactive` là `Closed` nếu `SoChoDaGiu = SucChua`, ngược lại `Open`.
- `HoldSeatsAsync` chạy một lệnh `ExecuteUpdateAsync` nguyên tử, tương đương: `UPDATE LICHKHOIHANH SET SoChoDaGiu = SoChoDaGiu + @g, TrangThai = (nếu SoChoDaGiu + @g = SucChua thì Closed ngược lại Open) WHERE MaLKH = @id AND TrangThai = Open AND NgayGioKhoiHanh > @now AND SoChoDaGiu + @g <= SucChua AND tour của lịch là Published` (viết bằng hằng `DepartureStatus`, `ContentStatus`, không viết chuỗi). Một dòng bị ảnh hưởng → `Held`; không dòng nào → đọc lại bằng `StateAsync`: `Full` khi `SucChua − SoChoDaGiu < g` (kể cả lịch đã tự `Closed` vì đầy), `Closed` khi lịch `Inactive`, đã khởi hành hoặc tour không `Published`, `NotFound` khi không có lịch; `Remaining = SucChua − SoChoDaGiu`. Nhờ vậy hai yêu cầu tranh chỗ cuối thì chỉ một được giữ (AC2 của US11).
- `ReleaseSeatsAsync`: `SoChoDaGiu = SoChoDaGiu - @g WHERE SoChoDaGiu >= @g`, và nếu lịch đang `Closed` mà hết đầy thì về `Open`; lịch `Inactive` vẫn trừ số chỗ nhưng không đổi trạng thái. Booking chỉ gọi sau khi đã chuyển trạng thái đơn thành công, nên mỗi đơn trả chỗ đúng một lần.
- Ghi nhật ký: nhãn ở mục 9.2, `Before`, `After` là bản ghi trước và sau.

## 5. Module Itinerary (PH2)

Hình 4.5.2. Đã có xem trước, lưu, liệt kê; cần sinh sửa, xóa, chọn suất, kiểm tra xung đột (UC06, UC07, UC08) và phiên bản lịch trình cho Quotation.

### 5.1 Lớp

| Lớp | Loại | | Trường | Phương thức (★ là phương thức mới) |
|---|---|---|---|---|
| `ItineraryModule` | IModule | [có] | – | `+ Register(services, configuration): void`<br>`+ MapEndpoints(endpoints): void` |
| `ItineraryEndpoints` | static | [mở rộng] | `/api/itineraries…` | `+ Map(endpoints): void` |
| `ItineraryService` | class | [mở rộng] | `- repository: ItineraryRepository`<br>`- catalog: ICatalogApi`<br>`- checker: ScheduleChecker` | `+ PreviewAsync(ids): ItineraryPreview`<br>`+ ListAsync(accountId): List<Itinerary>`<br>`+ SaveAsync(accountId, draft): Itinerary?`<br>`+ GetAsync(accountId, id): ItineraryDetail?` ★<br>`+ UpdateAsync(accountId, id, draft): OpResult<Itinerary>` ★<br>`+ DeleteAsync(accountId, id): OpResult<bool>` ★<br>`+ SetSlotsAsync(accountId, id, picks): OpResult<ItineraryCheck>` ★<br>`+ CheckAsync(accountId, id): OpResult<ItineraryCheck>` ★<br>`+ CheckDraftAsync(draft, picks): ItineraryCheck` ★ |
| `ScheduleChecker` | class | [mới] | `- bufferMinutes: int` (đọc `Itinerary:BufferMinutes`, mặc định 30) | `+ Check(stops: List<StopPlan>, slots: List<SlotDetail>, picks: List<SlotPick>): ItineraryCheck`<br>`- Overlaps(a, b): bool`<br>`- WrongDate(stop, slot): bool` |
| `ItineraryVersionService` | class | [mới] | `- repository: ItineraryRepository`<br>`- catalog: ICatalogApi`<br>`- checker: ScheduleChecker` | `+ CreateVersionAsync(itineraryId, accountId): OpResult<ItineraryVersionRef>`<br>`+ VersionAsync(versionId): ItineraryVersion?`<br>`+ CheckVersionAsync(versionId): ItineraryCheck`<br>`+ ProgramAsync(versionId): List<ProgramStop>` |
| `IItineraryApi` | interface – module Itinerary | [mới] | – | `+ CreateVersionAsync(itineraryId, accountId): OpResult<ItineraryVersionRef>`<br>`+ VersionAsync(versionId): ItineraryVersion?`<br>`+ CheckVersionAsync(versionId): ItineraryCheck`<br>`+ ProgramAsync(versionId): List<ProgramStop>` |
| `ItineraryRepository` | class | [mở rộng] | `- db: DongNamTripDbContext` | `+ ListAsync(accountId), AddAsync(accountId, draft)`<br>`+ GetAsync(accountId, id): ItineraryDetail?` ★<br>`+ UpdateAsync(accountId, id, draft): bool` ★ (thay toàn bộ điểm dừng, bỏ luôn suất của điểm bị bỏ)<br>`+ DeleteAsync(accountId, id): bool` ★<br>`+ HasRequestsAsync(id): bool` ★<br>`+ ReplaceSlotsAsync(id, picks): void` ★<br>`+ LockAsync(id): bool` ★ (`UPDLOCK` dòng LICHTRINH)<br>`+ AddVersionAsync(id, json): ItineraryVersionRef` ★<br>`+ VersionAsync(versionId): ItineraryVersion?` ★ |
| `ICatalogApi` | interface – module Catalog | [mở rộng] | – | `+ DestinationsAsync(ids): List<Destination>`<br>`+ SlotsAsync(slotIds): List<SlotDetail>`<br>`+ HoursAsync(destinationIds)` |

### 5.2 Endpoint

| Phương thức, route | Vai trò | Vào → Ra | Mã lỗi | UC, SCR | |
|---|---|---|---|---|---|
| `GET /api/itineraries/preview`, `GET /api/itineraries`, `POST /api/itineraries` | như hiện tại | như hiện tại | | UC05, UC08 | [có] |
| `POST /api/itineraries/check` | công khai (nháp tạm của khách) | `DraftCheckRequest` → `ItineraryCheck` | 400 | UC07, SCR08 | [mới] |
| `GET /api/itineraries/{id}` | đăng nhập, chủ lịch trình | → `ItineraryDetail` | 404 | UC08 | [mới] |
| `PUT /api/itineraries/{id}` | đăng nhập, chủ | `ItineraryDraft` → `Itinerary` | 400, 404 | UC08 | [mới] |
| `DELETE /api/itineraries/{id}` | đăng nhập, chủ | → 204 | 404, 409 đã có yêu cầu tour riêng | UC08 | [mới] |
| `PUT /api/itineraries/{id}/slots` | đăng nhập, chủ | `List<SlotPick>` → `ItineraryCheck` | 400 suất sai địa điểm, sai ngày, ngừng, trùng; 404 | UC06, SCR08 | [mới] |
| `GET /api/itineraries/{id}/check` | đăng nhập, chủ | → `ItineraryCheck` | 404 | UC07 | [mới] |

Lịch trình của người khác trả 404, không trả 403, để không lộ mã tồn tại (QT08).

### 5.3 Quy tắc

- Cấu trúc dữ liệu hiện có (quyết định cách làm): `LICHTRINH_DIADIEM` có khóa chính (lịch trình, địa điểm), nên mỗi địa điểm chỉ một lần trong lịch trình (`SaveAsync` đã chặn trùng); `POST` hiện gán `NgayThu = 1`, `ThuTu = vị trí`. `LICHTRINH_SUAT` có khóa ngoại ghép tới (lịch trình, địa điểm), nên suất chỉ thêm được cho điểm đang có trong lịch trình; `UpdateAsync` thay toàn bộ điểm dừng (xóa rồi thêm trong một giao dịch vì khóa duy nhất (lịch trình, ngày thứ, thứ tự)) và xóa các suất của điểm bị bỏ, trả cảnh báo cho khách.
- Nhiều ngày (Duy chốt 08/11): để thỏa "sắp xếp theo ngày" (BR2.2, AC2 của US05), dùng `ItineraryStopDraft.Day` (1–30) đã có trong `ItineraryDraft.Stops`; không thêm `Days` để tránh hai nơi biểu diễn cùng một thông tin. Không có `Stops` thì mọi điểm ngày 1 như hiện nay (tương thích ngược).
- `SetSlotsAsync` kiểm tra từng lựa chọn: suất thuộc hoạt động của đúng địa điểm trong lịch trình, `Status = Published`, ngày suất bằng `NgayDi + NgayThu - 1` của điểm dừng, không trùng cặp (địa điểm, suất). Sai → `Invalid` kèm danh sách trường. Hợp lệ → thay toàn bộ LICHTRINH_SUAT của lịch trình với `GhiChu = "cần xác minh"` (QT02), rồi chạy `ScheduleChecker`. Suất chỉ là nhu cầu: không trừ chỗ, không tạo đơn (AC3 của US06).
- `ScheduleChecker.Check` là lớp thuần, không truy cập dữ liệu, kiểm thử đơn vị được. Cảnh báo: `Overlap` khi hai suất cùng ngày giao nhau hoặc cách nhau chưa đủ `BufferMinutes` (giờ đến muộn hơn giờ bắt đầu, AC1 của US07); `WrongDate` khi suất khác ngày điểm dừng; `Duplicate` khi nháp chọn trùng cặp (endpoint `check` không từ chối, chỉ cảnh báo); `Inactive` khi suất đã ngừng; `OutOfHours` khi suất nằm ngoài giờ mở cửa của điểm; `Incomplete` khi điểm thiếu giờ mở cửa hoặc thời lượng (lúc đó `Complete = false`, không kết luận "phù hợp", AC3 của US07). `Fits` = không có cảnh báo ngoài `Incomplete`. Thời gian di chuyển giữa các chặng chưa có dữ liệu nên tính 0 và ghi chú trong cảnh báo.
- `Inactive` hai chỗ, hai nghĩa: chọn suất đang ngừng lúc `SetSlotsAsync` bị từ chối (400); suất đã lưu rồi mới bị ngừng thì khi mở lại `GetAsync`, `CheckAsync` hiện cảnh báo `Inactive` và không tự đổi suất (AC3 của US08).
- `CreateVersionAsync` (chỉ chủ lịch trình): `LockAsync` khóa dòng lịch trình, rồi tạo PHIENBANLICHTRINH với `SoPhienBan = max + 1`, `NoiDungJson` là ảnh chụp lịch trình gồm thông tin chung, điểm dừng, ngày và suất mong muốn. Phiên bản không bao giờ bị sửa (QT05); bản 1 do `POST` đã tạo giữ nguyên.
- Lịch trình đã có yêu cầu tour riêng vẫn sửa được, nhưng yêu cầu cũ giữ phiên bản cũ. `HasRequestsAsync` (chặn xóa, cờ `HasRequests`) **đọc** YEUCAUTOURRIENG qua `MaPB → PHIENBANLICHTRINH` bằng entity chung, chỉ đọc, giống `ActivityRepository` đang đọc `LICHTRINH_SUAT`.

## 6. Module Booking (PH3)

Hình 4.5.3. Đã có xem lịch sử đơn, giao dịch và lựa chọn thanh toán; cần sinh đặt chỗ, hủy, xác nhận, điều chỉnh, ghi nhận thu và hoàn, hết hạn giữ chỗ (UC11, UC12, UC13, UC19, UC20; SCR05, SCR10, SCR17). Các route đặt chỗ, hủy của khách nằm chung nhóm `/api/account/…` với `payments` hiện có.

### 6.1 Lớp

| Lớp | Loại | | Trường | Phương thức (★ là phương thức mới) |
|---|---|---|---|---|
| `BookingModule` | IModule | [có] | – | `+ Register(services, configuration): void` (thêm `AddHostedService<HoldExpiryWorker>()`)<br>`+ MapEndpoints(endpoints): void` |
| `BookingEndpoints` | static | [mở rộng] | `/api/account/bookings…`<br>`/api/account/payments…` | `+ Map(endpoints): void` |
| `BookingAdminEndpoints` | static | [mới] | `/api/admin/bookings…` | `+ Map(endpoints): void` |
| `BookingService` | class | [mới] | `- repository: BookingRepository`<br>`- catalog: ICatalogApi`<br>`- administration: IAdministrationApi` (đọc lịch sử điều chỉnh)<br>`- configuration: IConfiguration` | `+ PlaceAsync(accountId, form: BookingForm): OpResult<Fresh<BookingInfo>>`<br>`+ ListAsync(accountId): List<BookingInfo>`<br>`+ GetAsync(accountId, id): BookingDetail?`<br>`+ RequestCancelAsync(accountId, id, reason): OpResult<BookingInfo>`<br>`+ CreateFromQuoteAsync(acceptance, actorId): OpResult<int>`<br>`+ OrderIdByQuoteAsync(quoteId): int?` |
| `BookingAdminService` | class | [mới] | `- repository: BookingRepository`<br>`- catalog: ICatalogApi`<br>`- narration: INarrationApi`<br>`- administration: IAdministrationApi`<br>`- configuration: IConfiguration` | `+ ListAsync(filter: BookingFilter): PagedResult<AdminBooking>`<br>`+ GetAsync(id): AdminBookingDetail?`<br>`+ ConfirmAsync(id, actor): OpResult<AdminBooking>`<br>`+ AdjustAsync(id, form: AdjustForm, actor): OpResult<AdminBookingDetail>`<br>`+ DecideCancelAsync(id, decision: CancelDecision, actor): OpResult<AdminBooking>`<br>`+ RecordMoneyAsync(id, form: MoneyForm, actor): OpResult<Fresh<MoneyEntry>>`<br>`+ CompleteAsync(id, actor): OpResult<AdminBooking>`<br>`+ ExpireHoldsAsync(now): int` |
| `PaymentService` | class | [có] | `- repository: BookingRepository` | `+ ListAsync(accountId), GetAsync(accountId, orderId)`<br>`+ ChoiceAsync(accountId), SaveChoiceAsync(accountId, method)` |
| `OrderRules` | static | [mới] | – | `+ CanMove(from, to): bool`<br>`+ Paid(entries): decimal` (tổng thu)<br>`+ Refunded(entries): decimal` (tổng hoàn)<br>`+ Outstanding(order, entries): decimal`<br>`+ Refundable(entries): decimal` |
| `HoldExpiryWorker` | BackgroundService | [mới] | `- scopes: IServiceScopeFactory`<br>`- interval: đọc Booking:ExpiryIntervalMinutes (mặc định 5)` | `# ExecuteAsync(stoppingToken): Task` (mỗi lần tạo scope, gọi `BookingAdminService.ExpireHoldsAsync(now)`) |
| `IBookingApi` | interface – module Booking | [mới] | – | `+ CreateFromQuoteAsync(acceptance: QuoteAcceptance, actorId): OpResult<int>`<br>`+ OrderIdByQuoteAsync(quoteId): int?` |
| `BookingRepository` | class | [mở rộng] | `- db: DongNamTripDbContext` | `+ OrdersAsync, OrderAsync, PaymentChoiceAsync (đã có)`<br>`+ BeginAsync(): IDbContextTransaction` ★<br>`+ AddOrderAsync(order: Dondat): int` ★ (bắt `DbErrors.IsDuplicateKey`)<br>`+ OpenOrderAsync(accountId, departureId): Dondat?` ★ (đơn `Pending` của tài khoản cho lịch)<br>`+ ByQuoteAsync(quoteId): Dondat?` ★<br>`+ GetAsync(id): Dondat?` ★<br>`+ MoveAsync(id, from, to): bool` ★<br>`+ SetCancelAsync(id, reason, previous): bool`, `+ ClearCancelAsync(id): void` ★<br>`+ AddMoneyAsync(entry: Giaodichtien): bool` ★ (bắt trùng `MaThamChieu`)<br>`+ MoneyAsync(orderId): List<Giaodichtien>`, `+ MoneyByReferenceAsync(reference): Giaodichtien?` ★<br>`+ ExpiredHoldsAsync(now): List<(Dondat Order, decimal Net)>` ★ (`Pending`, có lịch, `HanGiuCho < now`, kèm `Paid − Refunded`; service giữ lại đơn có `Net < KhoanCanThu`)<br>`+ ListAsync(filter): (List<Dondat>, int)` ★ |
| `ICatalogApi` | interface – module Catalog | [mở rộng] | – | `+ DepartureAsync(id)`, `+ DeparturesAsync(ids)`<br>`+ HoldSeatsAsync(departureId, guests): HoldResult`<br>`+ ReleaseSeatsAsync(departureId, guests): Task` |
| `INarrationApi` | interface – module Narration | [có] | – | `+ GrantListenRightAsync(grant, actorId): OpResult<ListenRightGrant>`<br>`+ RevokeListenRightAsync(orderId, actorId): Task` |
| `IAdministrationApi` | interface – module Administration | [mở rộng] | – | `+ RecordAsync(entry: AuditEntry): Task`<br>`+ NotifyAsync(accountId, title, body)`, `+ AccountNamesAsync(ids)` ★ |

### 6.2 Endpoint

| Phương thức, route | Vai trò | Vào → Ra | Mã lỗi | UC, SCR | |
|---|---|---|---|---|---|
| `GET /api/account/payments`, `/payments/{id}`, `GET`/`PUT /api/account/payment-choice` | đăng nhập | như hiện tại | | UC12 | [có] |
| `POST /api/account/bookings` | đăng nhập | `BookingForm` → 201 `BookingInfo`; 200 nếu gửi lặp cùng lịch và cùng số khách | 400, 404 lịch không tồn tại, 409 hết chỗ, lịch đóng hoặc đã có đơn chờ khác số khách (kèm số chỗ còn) | UC11, SCR05 | [mới] |
| `GET /api/account/bookings` | đăng nhập | → `List<BookingInfo>` | | UC12, SCR10 | [mới] |
| `GET /api/account/bookings/{id}` | đăng nhập, chủ đơn | → `BookingDetail` (kèm `Program`: điểm của tour trong đơn tour có sẵn, rỗng với đơn tour riêng; `Adjustments`: lịch sử điều chỉnh đọc từ nhật ký "Điều chỉnh đơn" của đơn, không hiện ý kiến đồng ý của khách) | 404 | UC12, UC13 | [mới] |
| `GET /api/account/quotes/{id}/program` | đăng nhập, chủ | → `List<ProgramStop>` (lịch trình của phiên bản gắn báo giá, QT05; SPA dùng cho đơn tour riêng) | 404 sai chủ | UC12 | [mới] |
| `POST /api/account/bookings/{id}/cancel` | đăng nhập, chủ đơn | `CancelRequest` → `BookingInfo` | 400 thiếu lý do, 404, 409 đơn không ở `Pending` hoặc `Confirmed`, hoặc `NgayDi` đã qua | UC13, SCR10 | [mới] |
| `GET /api/admin/bookings` | Admin, Operator, Finance | `BookingFilter` (query; `Q` khớp mã đơn hoặc liên hệ; `Overdue = true` chỉ lấy đơn `Confirmed` có `NgayDi` đã qua, để nhân viên hoàn thành) → `PagedResult<AdminBooking>` | | UC19, UC20, SCR17 | [mới] |
| `GET /api/admin/bookings/{id}` | Admin, Operator, Finance | → `AdminBookingDetail` | 404 | UC19, UC20 | [mới] |
| `POST /api/admin/bookings/{id}/confirm` | Admin, Operator | → `AdminBooking` | 404, 409 trạng thái không phải `Pending` hoặc `Expired`, 422 chưa đủ điều kiện (kèm `MissingForConfirm`) | UC19 | [mới] |
| `POST /api/admin/bookings/{id}/adjust` | Admin, Operator | `AdjustForm` → `AdminBookingDetail` | 400 thiếu lý do hoặc ý kiến khách, 404, 409 đơn chưa `Confirmed` | UC19 | [mới] |
| `POST /api/admin/bookings/{id}/cancel-decision` | Admin, Operator | `CancelDecision` → `AdminBooking` | 400 thiếu lý do, 404, 409 đơn không ở `CancelRequested` hoặc không khôi phục được vì khách đã có đơn chờ khác cho lịch | UC13, SCR17 | [mới] |
| `POST /api/admin/bookings/{id}/money` | Admin, Finance | `MoneyForm` → 201 `MoneyEntry`; 200 nếu trùng mã tham chiếu của chính đơn | 400, 404, 409 trùng mã của đơn khác hoặc thu khi đơn `CancelRequested`, `Cancelled`, `Completed`, 422 vượt số còn phải thu hoặc còn có thể hoàn | UC20, SCR17 | [mới] |
| `POST /api/admin/bookings/{id}/complete` | Admin, Operator | → `AdminBooking` | 404, 409 chưa khởi hành hoặc chưa `Confirmed` | UC19 | [mới] |

### 6.3 Máy trạng thái đơn (STM-07) và các luồng

Đơn có hai nguồn (`CK_DONDAT_NguonDon`): **tour có sẵn** (`MaLKH`, giữ chỗ ở lịch khởi hành) và **tour riêng** (`MaBG`, không giữ chỗ). Báo cáo ghi rõ "giữ chỗ chỉ áp dụng cho lịch khởi hành của tour", nên đơn tour riêng có `HanGiuCho = NULL`, không hết hạn giữ chỗ, và không gọi `HoldSeats/ReleaseSeats`.

| Từ | Sang | Phương thức | Điều kiện, tác dụng phụ |
|---|---|---|---|
| (mới) | `Pending` | `PlaceAsync` (tour có sẵn), `CreateFromQuoteAsync` (tour riêng) | Tour có sẵn: giữ chỗ trong cùng giao dịch, `HanGiuCho = min(now + Booking:HoldHours, StartsAt)`, `HoldHours = 48` (giả định). Cả hai nguồn: `KhoanCanThu` (khoản cần thu để xác nhận) và `NgayDi` được ghi lúc tạo (mục dưới) |
| `Pending` | `Confirmed` | `ConfirmAsync` | `Paid − Refunded ≥ KhoanCanThu` (`MissingForConfirm` nêu số còn thiếu). Cấp quyền nghe qua `INarrationApi` (cửa sổ ở mục dưới). Ghi nhật ký, thông báo khách |
| `Pending` | `Expired` | `ExpireHoldsAsync` (`HoldExpiryWorker`) | Chỉ đơn tour có sẵn, `HanGiuCho < now` và `Paid − Refunded < KhoanCanThu`; trả chỗ đúng một lần; ghi nhật ký (người thực hiện rỗng), thông báo khách |
| `Expired` | `Confirmed` | `ConfirmAsync` | Có khoản thu muộn (UC20, AC2 của US20; Duy chốt 08/11, STM-07 có mũi tên này): đủ `KhoanCanThu`, giữ lại chỗ bằng `HoldSeatsAsync` (không `Held` → 409 kèm số chỗ còn), lịch phải còn `Open` và `Price × Guests = GiaChot` (đổi giá → 409, hướng dẫn hoàn tiền) |
| `Pending`, `Confirmed` | `CancelRequested` | `RequestCancelAsync` | Chỉ chủ đơn; `NgayDi` chưa qua (đã qua thì 409, vì đơn chờ nhân viên hoàn thành); lưu `LyDoHuy` và trạng thái cũ vào `TrangThaiTruocHuy`; chỗ vẫn giữ, đơn `CancelRequested` không bị worker hết hạn |
| `CancelRequested` | `Cancelled` | `DecideCancelAsync(approve: true)` | Trả chỗ (đơn tour có sẵn), thu hồi quyền nghe; ghi nhật ký; thông báo khách. Khoản hoàn ghi riêng bằng `RecordMoneyAsync(Refund)`, không tự coi là đã hoàn (AC3 của US13) |
| `CancelRequested` | trạng thái cũ | `DecideCancelAsync(approve: false)` | Đọc `TrangThaiTruocHuy`, đặt lại, xóa `TrangThaiTruocHuy`; lý do từ chối vào nhật ký (`Reason`) và thông báo khách. `CancelReason` chỉ hiển thị khi đơn đang `CancelRequested` hoặc `Cancelled`. Nếu trong lúc chờ duyệt khách đã đặt đơn `Pending` khác cho cùng lịch, việc đặt lại về `Pending` vi phạm `UX_DONDAT_Pending_TK_LKH`: repository bắt trùng khóa, service rollback và trả 409 ("khách đã có đơn chờ khác cho lịch này; hãy duyệt hủy đơn này hoặc xử lý đơn kia") |
| `Confirmed` | `Completed` | `CompleteAsync` | `NgayDi` đã qua (so với ngày máy chủ); nhân viên thao tác vì thời lượng tour là chữ tự do |

`Cancelled`, `Completed` là trạng thái cuối; khách không hủy được đơn `Expired`; đơn `Expired` đã thu tiền thì nhân viên có quyền `Finance` hoàn bằng `Refund`.

Mọi lần chuyển dùng `BookingRepository.MoveAsync(id, from, to)`, một câu `UPDATE … WHERE MaDon = @id AND TrangThai = @from`. Trả `false` nghĩa là đơn đã bị người khác đổi trước, service trả 409 và không chạy tác dụng phụ. Đây là cách bảo đảm trả chỗ đúng một lần (QT04). Mỗi luồng có tác dụng phụ (`ConfirmAsync` từ `Expired`, `DecideCancelAsync`, từng đơn trong `ExpireHoldsAsync`) chạy trong một giao dịch (`BeginAsync`): đổi trạng thái, giữ hoặc trả chỗ, cấp hoặc thu hồi quyền nghe, ghi nhật ký cùng commit hoặc cùng rollback.

Quy tắc tiền (khớp báo cáo: "đã thanh toán = tổng thu − tổng hoàn"): `Paid = Σ Receipt`, `Refunded = Σ Refund`, `Outstanding = max(0, GiaChot − (Paid − Refunded))`, `Refundable = Paid − Refunded`. `Paid`, `Refunded`, `Outstanding` hiển thị riêng (AC3 của US20). Không có công thức hoàn theo điều kiện hủy: `DieuKienHuy` là chữ tự do, số tiền hoàn do nhân viên tài chính quyết định rồi ghi, tối đa `Refundable`; vì vậy `BookingDetail` không có "khoản hoàn dự kiến".

Luồng đặt chỗ `PlaceAsync`: kiểm tra `BookingForm` (số khách `1…Limits.GuestsMax`, liên hệ không rỗng) → mở giao dịch → `OpenOrderAsync(accountId, departureId)`: có đơn `Pending` cùng lịch và cùng số khách thì trả đơn đó (`IsNew = false`, 200), khác số khách thì 409 → `catalog.HoldSeatsAsync` (không `Held` → 409 kèm `Remaining`, `NotFound` → 404) → `catalog.DepartureAsync` đọc giá → `GiaChot = Price × Guests` tính trên máy chủ (QT06) → `AddOrderAsync` với `DieuKienHuy` chép từ tour (tour chưa có điều kiện hủy thì `CancelPolicyDefaults.Standard`), `LienHe`, `NgayDi` = ngày của `StartsAt`, `KhoanCanThu = Round(GiaChot × Booking:DepositRate, 0)` (`DepositRate = 0.3`, giả định), `HanGiuCho` → commit. Hai yêu cầu đồng thời: chỉ mục duy nhất `UX_DONDAT_Pending_TK_LKH` (mục 9) làm lệnh chèn thứ hai báo trùng khóa; repository bắt `DbErrors.IsDuplicateKey`, service rollback (trả lại chỗ vừa giữ) rồi đọc lại và trả đơn đã có (200).

Đơn tour riêng `CreateFromQuoteAsync(acceptance, actorId)` (do Quotation gọi trong giao dịch của nó): tạo đơn `Pending` với `MaBG`, `SoKhach`, `GiaChot = Amount`, `KhoanCanThu = Deposit`, `DieuKienHuy = CancelPolicy`, `NgayDi = TripStart`, `LienHe = Contact`, `HanGiuCho = NULL`. Không tự mở giao dịch.

Cửa sổ quyền nghe khi xác nhận: `From = NgayDi − 1 ngày` lúc 00:00, `To = NgayDi + Booking:ListenDays ngày` lúc 23:59:59 (`ListenDays = 7`, giả định), giờ máy chủ; `ListenRightGrant(OrderId, AccountId, From, To)`. Đơn `Completed` giữ quyền đến hết `To`.

Ghi nhận tiền `RecordMoneyAsync` (`Admin` hoặc `Finance`) chạy trong một giao dịch: `LockOrderAsync` khóa dòng DONDAT bằng `UPDLOCK` trước khi đọc số đã thu (hai lần ghi đồng thời trên cùng một đơn thành tuần tự, không cùng vượt `Outstanding`), khoản tiền và nhật ký cùng commit. `Receipt` không vượt `Outstanding`, và chỉ nhận khi đơn `Pending`, `Confirmed`, `Expired`; `Refund` không vượt `Refundable`, nhận ở mọi trạng thái; `At` không ở tương lai. `MaThamChieu` duy nhất toàn bảng (`UQ_GIAODICHTIEN_MaThamChieu` đã có): trùng với giao dịch của chính đơn này thì trả giao dịch đã có (200, AC1 của US20), trùng với đơn khác thì 409. Ghi nhận thu không tự xác nhận đơn (xác nhận do `ConfirmAsync`, AC1 của US19). Thu cho đơn `Expired` vẫn ghi; `MissingForConfirm` thêm "kiểm tra lại chỗ và giá". Ghi nhật ký mọi khoản thu, hoàn.

Điều chỉnh sau xác nhận `AdjustAsync` (đơn `Confirmed`): không ghi đè mà lưu `AuditEntry` có `Before`, `After`, `Reason = lý do + ý kiến đồng ý của khách`; nếu có `NewPrice` thì cập nhật `GiaChot` và `KhoanCanThu = min(KhoanCanThu, NewPrice)` trong cùng giao dịch (ràng buộc `KhoanCanThu ≤ GiaChot`), rồi thông báo khách (AC3 của US12, QT09).

`AdminBooking.Customer` lấy qua `IAdministrationApi.AccountNamesAsync` (Booking không đọc `TAIKHOAN`); `BookingInfo.Title` của đơn tour có sẵn lấy tên tour qua `ICatalogApi.DeparturesAsync` (một lần cho cả trang), của đơn tour riêng là "Tour riêng theo báo giá #{MaBG}". Khách tự đặt, tự hủy không ghi nhật ký (đã lưu dấu trong DONDAT); mọi thao tác của nhân viên và tác vụ nền đều ghi.

## 7. Module Quotation (PH4, kèm gửi yêu cầu tour riêng của PH2)

Hình 4.5.4. Module hiện chỉ có `QuotationModule.cs` rỗng; toàn bộ phần dưới cần sinh (UC09, UC17, UC18, UC10; SCR10, SCR18).

### 7.1 Lớp

| Lớp | Loại | | Trường | Phương thức (★ là phương thức mới) |
|---|---|---|---|---|
| `QuotationModule` | IModule | [có] | – | `+ Register(services, configuration): void` (đăng ký service, cổng, `AddHostedService<QuoteExpiryWorker>()`)<br>`+ MapEndpoints(endpoints): void` |
| `QuoteRequestEndpoints` | static | [mới] | `/api/account/quote-requests…`<br>`/api/account/quotes…` | `+ Map(endpoints): void` |
| `QuotationAdminEndpoints` | static | [mới] | `/api/admin/quote-requests…` | `+ Map(endpoints): void` |
| `QuoteRequestService` | class | [mới] | `- repository: QuotationRepository`<br>`- itinerary: IItineraryApi`<br>`- booking: IBookingApi`<br>`- administration: IAdministrationApi` | `+ SubmitAsync(accountId, form: QuoteRequestForm): OpResult<Fresh<QuoteRequestInfo>>`<br>`+ ListMineAsync(accountId): List<QuoteRequestInfo>`<br>`+ GetMineAsync(accountId, id): QuoteRequestDetail?`<br>`+ CancelAsync(accountId, id, reason): OpResult<QuoteRequestInfo>`<br>`+ AcceptQuoteAsync(accountId, quoteId): OpResult<Fresh<QuoteAccepted>>`<br>`+ AskChangeAsync(accountId, quoteId, note): OpResult<QuoteRequestInfo>` |
| `QuotationAdminService` | class | [mới] | `- repository: QuotationRepository`<br>`- itinerary: IItineraryApi`<br>`- administration: IAdministrationApi` | `+ ListAsync(filter: QuoteRequestFilter): PagedResult<QuoteRequestInfo>`<br>`+ GetAsync(id): AdminQuoteRequestDetail?`<br>`+ ReceiveAsync(id, actor): OpResult<QuoteRequestInfo>`<br>`+ RecordVerificationAsync(id, form: VerificationForm, actor): OpResult<VerificationInfo>`<br>`+ IssueQuoteAsync(id, form: QuoteForm, actor): OpResult<QuoteInfo>`<br>`+ CancelAsync(id, reason, actor): OpResult<QuoteRequestInfo>`<br>`+ ExpireQuotesAsync(now): int` |
| `QuoteCalculator` | static | [mới] | – | `+ Total(items: List<QuoteItemForm>): decimal`<br>`+ Validate(form: QuoteForm, now): Dictionary<string, string[]>` |
| `QuoteExpiryWorker` | BackgroundService | [mới] | `- scopes: IServiceScopeFactory`<br>`- interval: đọc Quotation:ExpiryIntervalMinutes (mặc định 15)` | `# ExecuteAsync(stoppingToken): Task` (gọi `QuotationAdminService.ExpireQuotesAsync(now)`) |
| `QuotationRepository` | class | [mới] | `- db: DongNamTripDbContext` | `+ BeginAsync(): IDbContextTransaction`<br>`+ OpenRequestAsync(accountId, itineraryId): Yeucautourrieng?` (trạng thái `Submitted`, `Verifying`, `Quoted`)<br>`+ AddRequestAsync(row: Yeucautourrieng): int`<br>`+ RequestAsync(id): Yeucautourrieng?`<br>`+ ListAsync(filter), MineAsync(accountId), OwnerAsync(requestId): int?`<br>`+ MoveRequestAsync(id, from, to): bool`<br>`+ AddVerificationAsync(row: Xacminhdichvu): int`, `+ VerificationsAsync(requestId)`<br>`+ AddQuoteAsync(quote: Baogia, items): int`<br>`+ QuotesAsync(requestId): List<QuoteInfo>`, `+ QuoteAsync(id): Baogia?`<br>`+ SupersedeAsync(requestId): int` (`Sent → Superseded`)<br>`+ MoveQuoteAsync(id, from, to): bool`<br>`+ ExpiredQuotesAsync(now): List<Baogia>` |
| `IItineraryApi` | interface – module Itinerary | [mới] | – | `+ CreateVersionAsync(itineraryId, accountId): OpResult<ItineraryVersionRef>`<br>`+ VersionAsync(versionId): ItineraryVersion?`<br>`+ CheckVersionAsync(versionId): ItineraryCheck`<br>`+ ProgramAsync(versionId): List<ProgramStop>` |
| `IBookingApi` | interface – module Booking | [mới] | – | `+ CreateFromQuoteAsync(acceptance: QuoteAcceptance, actorId): OpResult<int>`<br>`+ OrderIdByQuoteAsync(quoteId): int?` |
| `IAdministrationApi` | interface – module Administration | [mở rộng] | – | `+ RecordAsync(entry: AuditEntry): Task`<br>`+ NotifyAsync(accountId, title, body)`, `+ HistoryAsync(objectType, objectId)` ★ |

Quyền sở hữu yêu cầu xác định qua `MaPB → PHIENBANLICHTRINH.MaLT → LICHTRINH.MaTK` (Bảng 4.20): `QuotationRepository` được **đọc** (không ghi) hai bảng này qua entity chung để lọc theo chủ và kiểm tra yêu cầu đang mở, giống cách `ListenRepository` đang đọc `DONDAT`; mọi ghi vào lịch trình đi qua `IItineraryApi`.

### 7.2 Endpoint

| Phương thức, route | Vai trò | Vào → Ra | Mã lỗi | UC, SCR | |
|---|---|---|---|---|---|
| `POST /api/account/quote-requests` | đăng nhập, chủ lịch trình | `QuoteRequestForm` → 201 `QuoteRequestInfo`; 200 nếu đã có yêu cầu đang mở cho lịch trình đó | 400 (ngày đi đã qua, thiếu liên hệ, số khách), 404 lịch trình không thuộc khách | UC09, SCR10 | [mới] |
| `GET /api/account/quote-requests` | đăng nhập | → `List<QuoteRequestInfo>` | | UC09, UC10 | [mới] |
| `GET /api/account/quote-requests/{id}` | đăng nhập, chủ | → `QuoteRequestDetail` | 404 | UC10, SCR10 | [mới] |
| `POST /api/account/quote-requests/{id}/cancel` | đăng nhập, chủ | (không nhận lý do: YEUCAUTOURRIENG không có cột lý do) → `QuoteRequestInfo` | 404, 409 đã chốt hoặc đã hủy | UC09 | [mới] |
| `POST /api/account/quotes/{id}/accept` | đăng nhập, chủ | → 201 `QuoteAccepted`; 200 nếu đã chấp nhận trước đó | 404 sai chủ, 409 hết hạn, đã thay thế hoặc yêu cầu không ở `Quoted` | UC10 | [mới] |
| `POST /api/account/quotes/{id}/change-request` | đăng nhập, chủ | `ChangeRequest` → `QuoteRequestInfo` | 404, 409 báo giá đã `Accepted`, `Superseded` hoặc yêu cầu không ở `Quoted` | UC10 (luồng thay thế) | [mới] |
| `GET /api/admin/quote-requests` | Admin, Operator | `QuoteRequestFilter` → `PagedResult<QuoteRequestInfo>` | | UC17, SCR18 | [mới] |
| `GET /api/admin/quote-requests/{id}` | Admin, Operator | → `AdminQuoteRequestDetail` | 404 | UC17, UC18 | [mới] |
| `POST /api/admin/quote-requests/{id}/receive` | Admin, Operator | → `QuoteRequestInfo` | 404, 409 | UC17 | [mới] |
| `POST /api/admin/quote-requests/{id}/verifications` | Admin, Operator | `VerificationForm` → 201 `VerificationInfo` | 400 (kết quả lạ, `Unavailable` hoặc `Substitute` thiếu ghi chú đề xuất), 404, 409 chưa ở `Verifying` | UC17 | [mới] |
| `POST /api/admin/quote-requests/{id}/quotes` | Admin, Operator | `QuoteForm` → 201 `QuoteInfo` | 404, 409 yêu cầu không ở `Verifying` hoặc `Quoted`, 422 chưa có xác minh, thiếu hạng mục, hạn hiệu lực sai, lịch trình còn cảnh báo `Overlap` hoặc `Inactive` | UC18 | [mới] |
| `POST /api/admin/quote-requests/{id}/cancel` | Admin, Operator | `CancelRequest` → `QuoteRequestInfo` | 404, 409 | UC17 | [mới] |

### 7.3 Máy trạng thái (STM-04 yêu cầu, STM-08 báo giá) và các luồng

Yêu cầu "đang mở" là yêu cầu ở `Submitted`, `Verifying` hoặc `Quoted`. Báo giá không có trạng thái nháp: `IssueQuoteAsync` kiểm tra hợp lệ rồi tạo thẳng `Sent` (STM-08).

| Đối tượng | Từ → Sang | Phương thức | Ghi chú |
|---|---|---|---|
| Yêu cầu | (mới) → `Submitted` | `SubmitAsync` | Trong một giao dịch: `IItineraryApi.CreateVersionAsync` (khóa dòng lịch trình), rồi `OpenRequestAsync`; đã có yêu cầu đang mở thì rollback (bỏ phiên bản vừa tạo) và trả yêu cầu đó (200); chưa có thì lưu `MaPB` của phiên bản mới |
| Yêu cầu | `Submitted → Verifying` | `ReceiveAsync` | Nhân viên mở yêu cầu (UC17 bước 3.1); đã `Verifying` thì trả nguyên, không lỗi |
| Yêu cầu | `Verifying → Quoted` | `IssueQuoteAsync` | Có ít nhất một kết quả xác minh |
| Yêu cầu | `Quoted → Verifying` | `AskChangeAsync` | Khách đề nghị đổi (Duy chốt 08/11, STM-04 có mũi tên này), kể cả khi báo giá đã hết hạn (đưa yêu cầu vào hàng đợi của nhân viên): báo giá `Sent → Superseded` nếu còn `Sent` (UC10: báo giá hiện tại không dùng để chốt nữa); ghi nhật ký "Khách đề nghị đổi báo giá" với `Reason = note`, nhân viên đọc ở `AdminQuoteRequestDetail.ChangeNotes` (lọc từ `HistoryAsync`) |
| Yêu cầu | `Quoted → Accepted` | `AcceptQuoteAsync` | Cùng giao dịch với tạo đơn |
| Yêu cầu | `Submitted`, `Verifying`, `Quoted → Cancelled` | `CancelAsync` (khách hoặc nhân viên) | Báo giá `Sent` (nếu có) giữ nguyên rồi tự hết hạn; không thể chấp nhận vì yêu cầu không còn `Quoted` |
| Báo giá | (mới) → `Sent` | `IssueQuoteAsync` | Cùng giao dịch: `SupersedeAsync(requestId)` chuyển báo giá `Sent` cũ thành `Superseded` rồi chèn báo giá mới (chỉ mục lọc `UX_BAOGIA_MaYC_Sent` bảo đảm mỗi yêu cầu một báo giá `Sent`; hai nhân viên phát hành đồng thời thì người sau nhận 409). Yêu cầu từ `Verifying` chuyển `Quoted`; từ `Quoted` (báo giá cũ hết hạn) vẫn `Quoted` |
| Báo giá | `Sent → Accepted` | `AcceptQuoteAsync` | `HanHieuLuc >= now`, đúng chủ, ngày đi của lịch trình chưa qua |
| Báo giá | `Sent → Expired` | `ExpireQuotesAsync` (`QuoteExpiryWorker`) | `HanHieuLuc < now`; yêu cầu vẫn `Quoted`, nhân viên phát hành báo giá thay thế; từng báo giá một giao dịch, ghi nhật ký (người thực hiện rỗng), thông báo khách |

Luồng chấp nhận `AcceptQuoteAsync`: mở giao dịch → đọc báo giá và yêu cầu; sai chủ → 404 → báo giá đã `Accepted` thì trả đơn qua `IBookingApi.OrderIdByQuoteAsync` (200) → báo giá phải `Sent` (`Expired`, `Superseded` → 409), `HanHieuLuc >= now` (hết hạn → 409), yêu cầu phải `Quoted` (409) → ngày đi của lịch trình (`IItineraryApi.VersionAsync`) chưa qua, nếu đã qua thì 409 (đơn tour riêng không có hạn giữ chỗ và khách không hủy được khi `NgayDi` đã qua, nên không tạo đơn cho ngày đã qua; khách dùng "đề nghị đổi báo giá") → `MoveQuoteAsync(Sent → Accepted)`; trả `false` do tranh chấp thì đọc lại: đã `Accepted` thì trả đơn (200), ngược lại 409 → `MoveRequestAsync(Quoted → Accepted)` → `IBookingApi.CreateFromQuoteAsync(new QuoteAcceptance(...))` (số khách, liên hệ lấy từ yêu cầu; ngày đi từ `VersionAsync`, hiện là ngày hiện tại của lịch trình chứ chưa phải ảnh chụp trong phiên bản) → ghi nhật ký "Khách chấp nhận báo giá" → commit. `QuoteAccepted` trả 201.

Luồng phát hành `IssueQuoteAsync`: yêu cầu phải `Verifying` hoặc `Quoted`; phải có ít nhất một kết quả xác minh (422); `QuoteCalculator.Validate` (422); `IItineraryApi.CheckVersionAsync` không có cảnh báo `Overlap` hoặc `Inactive` (422, hoạt động không tổ chức được phải đề xuất thay thế, AC3 của US17), `Incomplete` không chặn → giao dịch như bảng trên → `NotifyAsync` khách → ghi nhật ký "Phát hành báo giá".

`QuoteCalculator.Total` cộng `Quantity × UnitPrice` của các hạng mục có `IncludedInPackage = false`; hạng mục đã nằm trong giá trọn gói hiển thị nhưng không cộng lần hai (QT06, AC2 của US18). Tiền tính bằng `decimal`, đơn vị VND, làm tròn 0 chữ số. `Validate` bắt buộc ít nhất một hạng mục, `Quantity > 0`, `UnitPrice >= 0`, `ValidUntil > now`, `0 <= Deposit <= Total`, `CancelPolicy` không rỗng. Báo giá lưu `TongGia = Total`, `KhoanCanThu = Deposit`, `MaPB` = phiên bản của yêu cầu.

Cột `IncludedInPackage` chưa có trong HANGMUCBAOGIA nên V003 thêm `TrongGoi bit NOT NULL DEFAULT 0` (mục 9). Mọi thao tác của nhân viên (`Receive`, `RecordVerification`, `IssueQuote`, `Cancel`) và tác vụ hết hạn đều ghi nhật ký; khách gửi, hủy yêu cầu không ghi (đã lưu dấu trong YEUCAUTOURRIENG).

## 8. Module Narration (PH5) và Administration (PH6)

### 8.1 Narration

Hình 4.5.5 (là Hình 4.5 hiện có). Module đã đủ 15 route và cổng `INarrationApi` (`ListenService` đã cấp, thu hồi quyền nghe, kiểm tra chủ đơn và ghi nhật ký); thiết kế đích không thêm lớp. Thay đổi duy nhất là Booking bắt đầu gọi `GrantListenRightAsync` khi xác nhận đơn và `RevokeListenRightAsync` khi duyệt hủy. Hai route `POST|DELETE /api/internal/listen-rights` (Admin, Operator) giữ nguyên làm đường cấp tay. `ListenRepository.AllowedDestinationsAsync` đã đọc điểm của cả hai nguồn đơn (tour qua `LICHKHOIHANH → TOUR_DIADIEM`, tour riêng qua `BAOGIA → PHIENBANLICHTRINH → LICHTRINH_DIADIEM`), nên không phải sửa khi Booking bắt đầu tạo đơn.

### 8.2 Administration — lớp

Hình 4.5.6. Đã có xác thực, phiên, khu tài khoản và nhật ký ghi; cần sinh quản lý người dùng, vai trò (SCR19, UC24), xem nhật ký và thống kê (SCR13), cùng ba phương thức mới của cổng.

| Lớp | Loại | | Trường | Phương thức (★ là phương thức mới) |
|---|---|---|---|---|
| `AdministrationModule` | IModule | [có] | – | `+ Register(services, configuration): void`<br>`+ MapEndpoints(endpoints): void Map: Administration, UserAdmin` |
| `AdministrationEndpoints` | static | [có] | `/api/auth…, /api/account…` | `+ Map(endpoints): void` |
| `UserAdminEndpoints` | static | [mới] | `/api/admin/users…, /api/admin/audit`<br>`/api/admin/stats` | `+ Map(endpoints): void` |
| `SqlAuthService` | class | [có] | `- db: DongNamTripDbContext`<br>`- sessions: SqlSessionStore`<br>`- passwordHasher: IPasswordHasher<Taikhoan>` | `+ LoginAsync(input): LoginResponse?`<br>`+ RegisterAsync(input): RegistrationResult`<br>`+ ChangePasswordAsync(accountId, input)`<br>`+ GetProfileAsync, UpdateProfileAsync` |
| `SqlSessionStore` | class | [có] | `- sessions: ConcurrentDictionary` | `+ Create(accountId): string`<br>`+ TryGetAccountId(token, out accountId): bool`<br>`+ ForAccount(accountId, currentToken)`<br>`+ RemoveAllForAccount(accountId): void` |
| `AccountAreaService` | class | [có] | `- repository: AccountAreaRepository`<br>`- catalog: ICatalogApi` | `+ CollectionsAsync(accountId)`<br>`+ AddFavoriteAsync, RecordActivityAsync`<br>`+ NoticesAsync, SavePreferenceAsync` |
| `AuditService` | class | [mở rộng] | `- repository: AuditRepository` | `+ RecordAsync(entry: AuditEntry): Task` (`ActorId` có thể rỗng)<br>`+ NotifyAsync(accountId, title, body): Task` ★<br>`+ HistoryAsync(objectType, objectId): List<AuditRow>` ★<br>`+ AccountNamesAsync(ids): Dictionary<int, string>` ★<br>`+ QueryAsync(filter: AuditFilter): PagedResult<AuditRow>` ★ |
| `IAdministrationApi` | interface – module Administration | [mở rộng] | – | `+ RecordAsync(entry: AuditEntry): Task`<br>`+ NotifyAsync(accountId, title, body): Task` ★<br>`+ HistoryAsync(objectType, objectId): List<AuditRow>` ★<br>`+ AccountNamesAsync(accountIds): Dictionary<int, string>` ★ |
| `UserAdminService` | class | [mới] | `- repository: UserAdminRepository`<br>`- sessions: SqlSessionStore`<br>`- passwordHasher: IPasswordHasher<Taikhoan>`<br>`- administration: IAdministrationApi` | `+ ListAsync(filter: UserFilter): PagedResult<AdminUser>`<br>`+ GetAsync(id): AdminUserDetail?`<br>`+ CreateStaffAsync(form: StaffForm, actor): OpResult<StaffCreated>`<br>`+ SetRolesAsync(id, roles: IReadOnlyList<string>, actor): OpResult<AdminUser>`<br>`+ SetStatusAsync(id, status, reason, actor): OpResult<AdminUser>`<br>`+ ResetPasswordAsync(id, actor): OpResult<TempPassword>` |
| `StatisticsService` | class | [mới] | `- repository: StatisticsRepository` | `+ SummaryAsync(from, to, status?): OperationSummary` |
| `AccountAreaRepository` | class | [có] | `- db: DongNamTripDbContext` | `+ FavoritesAsync, AddActivityAsync, NoticesAsync…` |
| `AuditRepository` | class | [mở rộng] | `- db: DongNamTripDbContext` | `+ AddAsync(actorId?, action, objectType, objectId, …)` (`actorId` đổi sang `int?`)<br>`+ AddNoticeAsync(notice: AccountNotice)` ★<br>`+ QueryAsync(filter): (List<AuditRow>, int)` ★<br>`+ ByObjectAsync(objectType, objectId): List<AuditRow>` ★<br>`+ NamesAsync(ids): Dictionary<int, string>` ★ |
| `UserAdminRepository` | class | [mới] | `- db: DongNamTripDbContext` | `+ ListAsync(filter): (List<AdminUser>, int)`<br>`+ GetAsync(id): Taikhoan?`<br>`+ EmailExistsAsync(email): bool`<br>`+ AddAsync(account, roles): int`<br>`+ ReplaceRolesAsync(id, roles): void`<br>`+ SetStatusAsync(id, status): bool`<br>`+ SetPasswordHashAsync(id, hash): bool`<br>`+ CountAdminsAsync(): int` |
| `StatisticsRepository` | class | [mới] | `- db: DongNamTripDbContext` | `+ OrdersByStatusAsync(from, to, status?)`<br>`+ MoneyTotalsAsync(from, to): (thu, hoàn)`<br>`+ RequestsByStatusAsync(from, to)` |
| `ICatalogApi` | interface – module Catalog | [có] | – | `+ NamesAsync(kind, ids): Dictionary` |

### 8.3 Administration — endpoint

| Phương thức, route | Vai trò | Vào → Ra | Mã lỗi | UC, SCR | |
|---|---|---|---|---|---|
| `/api/auth…` (4 route), `/api/account…` (15 route) | như hiện tại | như hiện tại | | UC01 | [có] |
| `GET /api/admin/users` | Admin | `UserFilter` → `PagedResult<AdminUser>` | | UC24, SCR19 | [mới] |
| `GET /api/admin/users/{id}` | Admin | → `AdminUserDetail` | 404 | UC24 | [mới] |
| `POST /api/admin/users` | Admin | `StaffForm` → 201 `StaffCreated` (kèm mật khẩu tạm) | 400 (thiếu vai trò, vai trò lạ), 409 email đã có | UC24 | [mới] |
| `PUT /api/admin/users/{id}/roles` | Admin | `RolesRequest` → `AdminUser` | 400 vai trò lạ hoặc rỗng, 404, 409 tự gỡ Admin của mình hoặc gỡ Admin cuối cùng | UC24 | [mới] |
| `POST /api/admin/users/{id}/status` | Admin | `StatusRequest` (`Active`, `Locked`) → `AdminUser` | 400, 404, 409 tự khóa mình | UC24 | [mới] |
| `POST /api/admin/users/{id}/reset-password` | Admin | → `TempPassword` | 404 | UC24 | [mới] |
| `GET /api/admin/audit` | Admin | `AuditFilter` → `PagedResult<AuditRow>` | | UC24, SCR13 | [mới] |
| `GET /api/admin/stats?from=&to=&status=` | Admin | → `OperationSummary` | 400 khoảng ngày sai | UC24, SCR13 | [mới] |

### 8.4 Administration — quy tắc

- Đổi vai trò có hiệu lực ở phiên kế tiếp (AC1 của US24): `SetRolesAsync` thay TAIKHOAN_VAITRO (tên vai trò phải có trong `AppRoles` và bảng VAITRO) rồi gọi `SqlSessionStore.RemoveAllForAccount(id)`, buộc đăng nhập lại. Vai trò `Finance` gán ở đây là cách duy nhất để một nhân viên được ghi thu hoàn (mục 0.2).
- Khóa tài khoản (`Locked`) cũng xóa mọi phiên; `SqlAuthService.LoginAsync` và handler xác thực đã từ chối tài khoản không `Active`.
- Mật khẩu tạm sinh ngẫu nhiên 12 ký tự, băm bằng `IPasswordHasher<Taikhoan>` (cùng bộ băm `SqlAuthService` đang dùng); chỉ trả một lần trong phản hồi, không ghi vào nhật ký.
- Nhật ký chỉ đọc (QT09), không có route sửa hoặc xóa. `AuditRow.ActorId` rỗng nghĩa là tác vụ hệ thống (hết hạn giữ chỗ, hết hạn báo giá), hiển thị "Hệ thống".
- Thống kê tách ba chỉ tiêu riêng: tổng giá trị đơn (`GiaChot` của đơn không `Cancelled`, `Expired`; đơn `CancelRequested` vẫn tính), tiền thu (`Receipt`), tiền hoàn (`Refund`); không có dữ liệu thì trả số 0 và danh sách theo trạng thái rỗng (UC24 luồng thay thế). `StatisticsRepository` đọc `DONDAT`, `GIAODICHTIEN`, `YEUCAUTOURRIENG` qua entity chung, chỉ đọc.
- `NotifyAsync` ghi một dòng TAIKHOAN_THONGBAO qua `AuditRepository.AddNoticeAsync` (cùng module nên được ghi bảng này; `TieuDe` tối đa 160, `NoiDung` tối đa 500 ký tự, cắt nếu dài); khách xem qua `GET /api/account/notices` đã có.
- `HistoryAsync` đọc nhật ký của một đối tượng cho module khác (ví dụ ghi chú khách đề nghị đổi báo giá); `AccountNamesAsync` trả tên hiển thị để Booking không phải đọc `TAIKHOAN`.

## 9. Thay đổi lược đồ, nhãn nhật ký, cấu hình

### 9.1 `db/init/V003_booking_quotation.sql`

Theo kiểu V001, V002: kiểm tra trước khi tạo nên chạy lại được; CI tự nạp mọi `db/init/V*.sql` theo tên. **Chú ý triển khai:** `db/init.sh` chỉ chạy khi cơ sở dữ liệu chưa tồn tại, nên máy đã có CSDL phải chạy tay `V003` (hoặc `docker compose down -v` để dựng lại). Nội dung đầy đủ (thay cho tệp V003 bản 05/11):

```sql
USE [DongNamTrip];
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 1. Cột mới */
IF COL_LENGTH(N'dbo.DONDAT', N'TrangThaiTruocHuy') IS NULL ALTER TABLE dbo.DONDAT ADD TrangThaiTruocHuy varchar(20) NULL;
IF COL_LENGTH(N'dbo.DONDAT', N'NgayDi')            IS NULL ALTER TABLE dbo.DONDAT ADD NgayDi date NULL;
IF COL_LENGTH(N'dbo.DONDAT', N'KhoanCanThu')       IS NULL ALTER TABLE dbo.DONDAT ADD KhoanCanThu decimal(18,0) NULL;
IF COL_LENGTH(N'dbo.DONDAT', N'LienHe')            IS NULL ALTER TABLE dbo.DONDAT ADD LienHe nvarchar(250) NULL;
IF COL_LENGTH(N'dbo.HANGMUCBAOGIA', N'TrongGoi')   IS NULL
    ALTER TABLE dbo.HANGMUCBAOGIA ADD TrongGoi bit NOT NULL CONSTRAINT DF_HANGMUCBAOGIA_TrongGoi DEFAULT (0);
GO

/* 2. Bỏ trạng thái Nháp của lịch khởi hành và báo giá (STM-06, STM-08); chuẩn hóa dòng cũ */
UPDATE dbo.LICHKHOIHANH SET TrangThai = 'Inactive'   WHERE TrangThai = 'Draft';
UPDATE dbo.BAOGIA       SET TrangThai = 'Superseded' WHERE TrangThai = 'Draft';
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_LICHKHOIHANH_TrangThai' AND definition LIKE N'%Draft%')
BEGIN
    ALTER TABLE dbo.LICHKHOIHANH DROP CONSTRAINT DF_LICHKHOIHANH_TrangThai;
    ALTER TABLE dbo.LICHKHOIHANH ADD CONSTRAINT DF_LICHKHOIHANH_TrangThai DEFAULT ('Open') FOR TrangThai;
END
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_BAOGIA_TrangThai' AND definition LIKE N'%Draft%')
BEGIN
    ALTER TABLE dbo.BAOGIA DROP CONSTRAINT DF_BAOGIA_TrangThai;
    ALTER TABLE dbo.BAOGIA ADD CONSTRAINT DF_BAOGIA_TrangThai DEFAULT ('Sent') FOR TrangThai;
END
GO

/* 3. Miền giá trị (khớp Vocabulary.cs) */
IF OBJECT_ID(N'dbo.CK_TAIKHOAN_TrangThai', N'C') IS NULL
    ALTER TABLE dbo.TAIKHOAN ADD CONSTRAINT CK_TAIKHOAN_TrangThai CHECK (TrangThai IN ('Active', 'Locked'));
IF OBJECT_ID(N'dbo.CK_TOUR_TrangThai', N'C') IS NULL
    ALTER TABLE dbo.TOUR ADD CONSTRAINT CK_TOUR_TrangThai CHECK (TrangThai IN ('Draft', 'Published', 'Inactive'));
IF OBJECT_ID(N'dbo.CK_LICHKHOIHANH_TrangThai', N'C') IS NULL
    ALTER TABLE dbo.LICHKHOIHANH ADD CONSTRAINT CK_LICHKHOIHANH_TrangThai CHECK (TrangThai IN ('Open', 'Closed', 'Inactive'));
IF OBJECT_ID(N'dbo.CK_DONDAT_TrangThai', N'C') IS NULL
    ALTER TABLE dbo.DONDAT ADD CONSTRAINT CK_DONDAT_TrangThai
        CHECK (TrangThai IN ('Pending', 'Confirmed', 'CancelRequested', 'Cancelled', 'Expired', 'Completed'));
IF OBJECT_ID(N'dbo.CK_DONDAT_TrangThaiTruocHuy', N'C') IS NULL
    ALTER TABLE dbo.DONDAT ADD CONSTRAINT CK_DONDAT_TrangThaiTruocHuy CHECK (TrangThaiTruocHuy IS NULL OR TrangThaiTruocHuy IN ('Pending', 'Confirmed'));
IF OBJECT_ID(N'dbo.CK_DONDAT_KhoanCanThu', N'C') IS NULL
    ALTER TABLE dbo.DONDAT ADD CONSTRAINT CK_DONDAT_KhoanCanThu CHECK (KhoanCanThu IS NULL OR (KhoanCanThu >= 0 AND KhoanCanThu <= GiaChot));
IF OBJECT_ID(N'dbo.CK_GIAODICHTIEN_LoaiGD', N'C') IS NULL
    ALTER TABLE dbo.GIAODICHTIEN ADD CONSTRAINT CK_GIAODICHTIEN_LoaiGD CHECK (LoaiGD IN ('Receipt', 'Refund'));
IF OBJECT_ID(N'dbo.CK_YEUCAUTOURRIENG_TrangThai', N'C') IS NULL
    ALTER TABLE dbo.YEUCAUTOURRIENG ADD CONSTRAINT CK_YEUCAUTOURRIENG_TrangThai
        CHECK (TrangThai IN ('Submitted', 'Verifying', 'Quoted', 'Accepted', 'Cancelled'));
IF OBJECT_ID(N'dbo.CK_BAOGIA_TrangThai', N'C') IS NULL
    ALTER TABLE dbo.BAOGIA ADD CONSTRAINT CK_BAOGIA_TrangThai CHECK (TrangThai IN ('Sent', 'Accepted', 'Expired', 'Superseded'));
IF OBJECT_ID(N'dbo.CK_XACMINHDICHVU_KetQua', N'C') IS NULL
    ALTER TABLE dbo.XACMINHDICHVU ADD CONSTRAINT CK_XACMINHDICHVU_KetQua CHECK (KetQua IN ('Available', 'Unavailable', 'Substitute'));
GO

/* 4. Chỉ mục phục vụ quy tắc (IX trạng thái–hạn của DONDAT, BAOGIA đã có trong DongNamTrip_Full.sql) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BAOGIA_MaYC_Sent' AND object_id = OBJECT_ID(N'dbo.BAOGIA'))
    CREATE UNIQUE INDEX UX_BAOGIA_MaYC_Sent ON dbo.BAOGIA (MaYC) WHERE TrangThai = 'Sent';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DONDAT_Pending_TK_LKH' AND object_id = OBJECT_ID(N'dbo.DONDAT'))
    CREATE UNIQUE INDEX UX_DONDAT_Pending_TK_LKH ON dbo.DONDAT (MaTK, MaLKH) WHERE TrangThai = 'Pending' AND MaLKH IS NOT NULL;
GO
```

Lý do từng thay đổi: `TrangThaiTruocHuy` để từ chối yêu cầu hủy trả đơn về đúng trạng thái trước (UC13); `NgayDi` để cấp quyền nghe và hoàn thành đơn của cả hai nguồn; `KhoanCanThu` để một quy tắc xác nhận chung (`Paid − Refunded ≥ KhoanCanThu`) cho cả đơn tour có sẵn (`GiaChot × DepositRate`) lẫn đơn từ báo giá (`BAOGIA.KhoanCanThu`); `LienHe` vì biểu mẫu đặt chỗ nhận liên hệ (UC11) mà `DONDAT` chưa có chỗ lưu; `TrongGoi` để không cộng hai lần hạng mục trọn gói (QT06); `UX_DONDAT_Pending_TK_LKH` để gửi lặp đồng thời không tạo hai đơn và không giữ chỗ hai lần (CLAUDE.md quy tắc 7, AC2 của US11); `UX_BAOGIA_MaYC_Sent` để mỗi yêu cầu một báo giá còn hiệu lực. Đã có sẵn và được dùng lại: `CK_DONDAT_NguonDon`, `UX_DONDAT_MaBG_KhongRong`, `UQ_GIAODICHTIEN_MaThamChieu`, `UQ_PHIENBANLICHTRINH_MaLT_SoPhienBan`, `CK_LICHKHOIHANH_SoChoDaGiu`, `IX_DONDAT_TrangThai_HanGiuCho`, `IX_BAOGIA_TrangThai_HanHieuLuc`.

Sau khi chạy V003 cần thêm vào entity `Dondat` bốn thuộc tính (`TrangThaiTruocHuy`, `NgayDi` kiểu `DateOnly?`, `KhoanCanThu`, `LienHe`) và `Hangmucbaogia` thuộc tính `TrongGoi`, cùng cấu hình trong `DongNamTripDbContext`. Dữ liệu mẫu: các tour seed đều ở `Draft` và chưa có lịch khởi hành nào; để thử luồng đặt chỗ phải công bố tour và tạo lịch bằng SCR16 (hoặc thêm vào `tests/ci/catalog_fixture.sql` cho CI).

### 9.2 Nhãn nhật ký (`AuditEntry.Action`, `ObjectType`)

| Thao tác | `Action` | `ObjectType`, `ObjectId` |
|---|---|---|
| Tour | "Tạo tour", "Sửa tour", "Sửa điểm dừng tour", "Công bố tour", "Ngừng tour" | `TOUR`, mã tour |
| Lịch khởi hành | "Tạo lịch khởi hành", "Sửa lịch khởi hành", "Ngừng lịch khởi hành" | `LICHKHOIHANH`, mã lịch |
| Đơn | "Xác nhận đơn", "Điều chỉnh đơn", "Duyệt hủy đơn", "Từ chối hủy đơn", "Hoàn thành đơn", "Đơn hết hạn giữ chỗ" (không người thực hiện) | `DONDAT`, mã đơn |
| Tiền | "Ghi nhận thu", "Ghi nhận hoàn" | `GIAODICHTIEN`, mã giao dịch (`Reason` = căn cứ, kèm mã đơn) |
| Yêu cầu tour riêng | "Tiếp nhận yêu cầu", "Hủy yêu cầu tour riêng" (nhân viên), "Khách đề nghị đổi báo giá" | `YEUCAUTOURRIENG`, mã yêu cầu |
| Xác minh | "Ghi xác minh dịch vụ" | `XACMINHDICHVU`, mã xác minh |
| Báo giá | "Phát hành báo giá", "Khách chấp nhận báo giá", "Báo giá hết hạn" (không người thực hiện) | `BAOGIA`, mã báo giá |
| Tài khoản | "Tạo tài khoản nhân viên", "Đổi vai trò", "Khóa tài khoản", "Mở khóa tài khoản", "Đặt lại mật khẩu" (không ghi mật khẩu) | `TAIKHOAN`, mã tài khoản |

### 9.3 Cấu hình (giả định, nhóm chốt con số)

Đọc bằng `GetValue<T>(khóa, mặc định)`, mặc định như dưới, nên chưa có trong `appsettings.json` vẫn chạy:

```json
"Booking": { "HoldHours": 48, "DepositRate": 0.3, "ListenDays": 7, "ExpiryIntervalMinutes": 5 },
"Quotation": { "ExpiryIntervalMinutes": 15 },
"Itinerary": { "BufferMinutes": 30 }
```

## 10. Thứ tự sinh code đề xuất

Mỗi bước build và chạy test được ngay, không chờ bước sau. Kiểm thử đơn vị đặt trong project test .NET mới `tests/DongNamTrip.Modules.Tests` (repo chưa có project test .NET; thêm vào `DongNamTrip.sln` và một bước `dotnet test` trong `.github/workflows/build.yml`); kiểm thử qua HTTP thêm vào `tests/api/*.py` như các runner hiện có.

1. Contracts và Modules: hằng ở mục 1, DTO ở mục 3; `OpStatus.Unprocessable` (422), `ModuleHttp.ToHttp(result, onOk)`, `Fresh<T>`; `AuditEntry.ActorId` và `AuditRepository.AddAsync` sang `int?`. Chạy `tests/architecture/rules.py` để chắc không còn chuỗi trạng thái viết tay.
2. V003 và entity, DbContext (mục 9.1).
3. Administration, phần cổng: `NotifyAsync`, `HistoryAsync`, `AccountNamesAsync` và các hàm repository tương ứng (để Booking, Quotation build được ngay). Test: thông báo xuất hiện ở `GET /api/account/notices`.
4. Catalog: mở rộng `ICatalogApi`, `CatalogService`, `CatalogRepository`; sinh `TourAdmin*`. Test: giữ chỗ đồng thời (hai tác vụ tranh chỗ cuối, chỉ một `Held`), lịch tự `Closed` khi đầy và tự `Open` khi trả chỗ, giảm sức chứa dưới số chỗ đã giữ trả 409, không giữ chỗ được khi tour `Inactive`.
5. Itinerary: `ScheduleChecker` (test đơn vị thuần), sau đó service, repository (`UpdateAsync`, `ReplaceSlotsAsync`, `LockAsync`), `ItineraryVersionService`. Test: đổi điểm dừng thì suất của điểm bị bỏ bị xóa; suất sai ngày bị từ chối.
6. Booking: `OrderRules` (test đơn vị cho `CanMove`, `Outstanding`, `Refundable`), `BookingService`, `BookingAdminService`, `HoldExpiryWorker`. Test: gửi lặp đồng thời trả cùng đơn và chỉ giữ chỗ một lần; hết hạn trả chỗ đúng một lần khi worker chạy hai lần; xác nhận cấp quyền nghe, duyệt hủy thu hồi quyền nghe; từ chối hủy trả đúng trạng thái cũ; chỉ `Admin` và `Finance` ghi được thu hoàn (Operator, Editor, Customer nhận 403); hai khoản thu đồng thời khác mã tham chiếu chỉ một khoản được ghi khi tổng vượt số còn phải thu.
7. Quotation: `QuoteCalculator` (test đơn vị), repository, hai service, `QuoteExpiryWorker`. Test: chấp nhận lặp trả cùng đơn; báo giá mới chuyển báo giá cũ sang `Superseded`; chấp nhận báo giá hết hạn trả 409; chấp nhận khi ngày đi đã qua trả 409 và không tạo đơn; gửi yêu cầu lặp trả cùng yêu cầu và không để lại phiên bản thừa.
8. Administration, phần quản trị: `UserAdmin*`, `Statistics*`, `AuditService.QueryAsync`. Test: gỡ Admin cuối cùng trả 409; đổi vai trò xóa phiên; thống kê khớp dữ liệu.
9. Frontend: SPA (SCR05, SCR08, SCR10; bật nút "Đặt tour") và WinForms (SCR13, SCR16, SCR17, SCR18, SCR19; thay các `ModuleControl` giữ chỗ) gọi các route mới qua `DongNamTrip.Client`. Menu "Tài chính" (`Admin` và `Finance`; sửa `MainForm.cs` để Admin cũng thấy) dùng `GET /api/admin/bookings` và `POST …/money`; menu "Đơn đặt tour" (Admin, Operator) dùng các route xác nhận, điều chỉnh, duyệt hủy, hoàn thành; SCR17 thêm bộ lọc "quá ngày đi, chưa hoàn thành" (`Overdue`) và nút "Hoàn thành đơn".

Sau mỗi PR lớn, cập nhật phụ lục B của báo cáo Chương 4–6 (danh sách cập nhật khi code thêm) và `src/Backend/CLAUDE.md` (mục "Tiến độ module hiện tại").

## 11. Hình đi kèm

Bộ hình ở `thiet-ke-day-du_2026-11-05/` vẽ theo bản 05/11. Sau bản rà này cần vẽ lại:

| Hình | Cần đổi |
|---|---|
| Hình 4.4 (C3) | Thêm quan hệ Catalog ⇄ Administration đã có (ghi chú vòng chấp nhận) nếu muốn đầy đủ; các cạnh mới giữ nguyên (Booking → Catalog, Booking → Narration, Quotation → Itinerary, Quotation → Booking) |
| Hình 4.5.1 (Catalog) | Thêm `HoursAsync`, `DeparturesAsync(ids)`, `HoldResult`; `deactivate` thay cho `status` của lịch |
| Hình 4.5.2 (Itinerary) | `CheckDraftAsync(draft, picks)`, `LockAsync`; thêm cảnh báo `OutOfHours` |
| Hình 4.5.3 (Booking) | `OrderIdByQuoteAsync`, `Fresh<T>`, `ExpireHoldsAsync` do worker gọi; bỏ `ExpectedRefund`; route tiền cho `Admin`, `Finance` |
| Hình 4.5.4 (Quotation) | `QuotationRepository.BeginAsync`; hai cổng có thêm phương thức; không còn trạng thái nháp của báo giá |
| Hình 4.5.6 (Administration) | Thêm `HistoryAsync`, `AccountNamesAsync`; `StaffCreated` |
| Hình 4.1 | Giữ nguyên |

Các hình dùng kỹ thuật bỏ đường đã nêu ở bản 05/11 (đường `*Module → *Endpoints`, đường ghi nhật ký của Booking và Quotation ghi bằng chữ trong mô tả lớp).

## 12. Thay đổi so với bản 05/11 và lý do

| # | Thay đổi | Lý do |
|---|---|---|
| 1 | Thêm mục 0.1 hiện trạng repo; đổi commit tham chiếu sang `9950e2f` | Duy yêu cầu cập nhật theo repo mới nhất |
| 2 | Lịch khởi hành: bỏ `Draft`, tạo ra là `Open`, `Closed` tự động khi đầy, bỏ đóng tay, thêm `deactivate`; bỏ rule 409 khi ngừng tour còn lịch `Open` | Khớp STM-05, STM-06 của báo cáo (tour Đã công bố "tạo lịch khởi hành ở trạng thái Mở"; Mở → Đóng when đầy chỗ); ngừng tour chặn giữ chỗ mới bằng điều kiện trong `TryHoldAsync` |
| 3 | Báo giá: bỏ `Draft`; `IssueQuoteAsync` tạo thẳng `Sent` | Khớp STM-08 (khởi tạo → Đã gửi); không có endpoint lưu nháp nào |
| 4 | Vai trò: route ghi thu hoàn cho `Admin` và `Finance` (Operator không có nếu chưa được cấp `Finance`); báo cáo gộp NV có quyền tài chính vào Nhân viên tư vấn và điều hành (Duy chốt 08/11, phương án A), code giữ `Finance` như quyền cấp thêm; thống kê chỉ Admin; thêm bảng vai trò ↔ tác nhân; sửa menu Tài chính của WinForms cho Admin | Duy chốt 08/11 "Admin có đủ quyền"; UC20, QT07 vẫn đòi người có quyền tài chính xác nhận nên Operator không tự ghi; US24 giao thống kê cho Quản trị viên |
| 5 | Tiền: `Paid − Refunded` (đã thanh toán = tổng thu − tổng hoàn); bỏ `ExpectedRefund`, thêm `Refundable`; một công thức `MaxRefund` | Khớp Bảng 4.13; `DieuKienHuy` là chữ tự do nên không tính được số hoàn dự kiến; bản 05/11 có hai công thức khác nhau |
| 6 | Thêm `DONDAT.KhoanCanThu`, `LienHe`; `NgayDi` cho cả hai nguồn; `HanGiuCho` rỗng với đơn tour riêng | Bản 05/11 dùng `Baogia.KhoanCanThu` mà Booking không đọc được, biểu mẫu nhận liên hệ nhưng không có chỗ lưu, đơn tour riêng không có `StartsAt`; báo cáo: giữ chỗ chỉ áp dụng cho lịch khởi hành |
| 7 | Thêm `UX_DONDAT_Pending_TK_LKH`; luồng `PlaceAsync` bắt trùng khóa; khác số khách trả 409; bỏ chỉ mục trùng với `IX_*` đã có | Gửi lặp đồng thời (AC2 của US11) và CLAUDE.md quy tắc 7; hai chỉ mục V003 cũ trùng chỉ mục đã có trong schema |
| 8 | V003 nhúng trong tài liệu; thêm CHECK `TAIKHOAN`, `KhoanCanThu`; đổi DEFAULT; chuẩn hóa dòng `Draft` cũ; ghi chú `init.sh` không chạy lại | Tài liệu tự chứa; bản 05/11 thiếu `TAIKHOAN` ở danh sách CHECK |
| 9 | `Fresh<T>`, `OpStatus.Unprocessable`, `ToHttp(result, onOk)`, `IBookingApi.OrderIdByQuoteAsync`, `HoldResult` | `ModuleHttp.ToHttp` hiện chỉ 200/400/403/404/409; cần 200/201 và 422; Quotation cần đọc đơn đã tạo mà không gọi hàm nội bộ Booking |
| 10 | `IAdministrationApi.NotifyAsync` làm ở bước 3 cùng `HistoryAsync`, `AccountNamesAsync`; `AuditEntry.ActorId` thành `int?` | Bản 05/11 để `NotifyAsync` ở bước 7 nên bước 6 không build; worker hết hạn không có người thực hiện; Booking không được đọc `TAIKHOAN` |
| 11 | Nhãn nhật ký tiếng Việt, `ObjectType` là tên bảng; thêm nhật ký cho thu hoàn, hoàn thành, duyệt hủy, mọi thao tác Quotation của nhân viên | Khớp cách các module đang dùng; bản 05/11 dùng mã kiểu `tour.save` và thiếu nhiều thao tác (QT09) |
| 12 | `IHostedService` đăng ký trong `Register`, chu kỳ đọc cấu hình; mỗi đơn, báo giá một giao dịch | `MapEndpoints` không nhận `services`; chu kỳ bản 05/11 viết cứng |
| 13 | Lịch trình: ghi rõ ràng buộc dữ liệu hiện có (một điểm một lần, `NgayThu = 1`, khóa ngoại suất tới điểm), thêm `DraftCheckRequest`, `HoursAsync`, `OutOfHours`, xử lý `Inactive`, `Duplicate`; nhiều ngày qua `Stops[].Day` | Bản 05/11 gọi `Opens`, `Closes`, `Day` mà không có nguồn dữ liệu; chữ ký `CheckDraftAsync` không khớp endpoint |
| 14 | Tạo yêu cầu tour riêng: khóa dòng lịch trình rồi mới kiểm tra yêu cầu đang mở; `Quoted → Verifying` làm báo giá cũ `Superseded` ngay khi khách đề nghị; hủy yêu cầu không động tới báo giá | Tránh phiên bản thừa và tranh chấp; UC10 "báo giá hiện tại không dùng để chốt nữa"; báo giá còn `Sent` của yêu cầu đã hủy không thể chấp nhận vì yêu cầu không còn `Quoted` |
| 15 | Route khách đặt chỗ, gửi yêu cầu chuyển sang `/api/account/…`; vai trò ghi "đăng nhập" thay "Customer" | Thống nhất tiền tố; các route khu tài khoản hiện có chỉ yêu cầu đăng nhập |
| 16 | `StaffCreated` cho `POST /api/admin/users`; băm bằng `IPasswordHasher<Taikhoan>` | Service và endpoint bản 05/11 trả hai kiểu khác nhau; repo dùng `IPasswordHasher` |
| 17 | Thêm `TourAdmin`: chặn sửa điểm dừng khi đã có chỗ giữ, chặn đổi ngày giờ lịch khi đã có chỗ giữ, địa điểm lặp, kiểm tra khu vực, loại hình | `TOUR_DIADIEM` có khóa chính (tour, địa điểm); AC3 của US16 |

## 13. Quyết định đã chốt (Duy, 08/11/2026)

Duy chọn phương án A cho cả năm điểm: sửa báo cáo theo tài liệu thay vì bỏ tính năng. Tài liệu không còn dấu chờ quyết.

| # | Điểm | Quyết định | Đã thể hiện ở |
|---|---|---|---|
| 1 | STM-07 và thu muộn | Thêm mũi tên Hết hạn → Đã xác nhận `[đủ khoản cần thu, còn chỗ, giá không đổi]`; `Hết hạn` không còn là trạng thái cuối | Mục 6.3 dòng `Expired → Confirmed` |
| 2 | STM-04 và đổi báo giá | Thêm mũi tên Đã báo giá → Đang xác minh `khách đề nghị đổi báo giá` | Mục 7.3 dòng `Quoted → Verifying` |
| 3 | Lịch trình nhiều ngày | Biểu diễn bằng `ItineraryStopDraft.Day` đã có; không thêm `Days` (đổi so với quyết định ban đầu 08/11, cần báo Duy) | Mục 2 (`ItineraryDraft`), mục 5 |
| 4 | Cột mới | Đưa `DONDAT.TrangThaiTruocHuy`, `NgayDi`, `KhoanCanThu`, `LienHe` và `HANGMUCBAOGIA.TrongGoi` vào báo cáo | V003 (mục 9.1), mục 14 |
| 5 | Hoàn thành đơn | Nhân viên bấm hoàn thành khi `NgayDi` đã qua; nhãn STM-07 đổi theo; kèm hai vá: khách không hủy được đơn đã qua `NgayDi`, SCR17 lọc "quá ngày đi, chưa hoàn thành" | Mục 6.2 route `complete`, `cancel`, `BookingFilter.Overdue`; mục 6.3 dòng `Confirmed → Completed`, `CancelRequested`; bước 9 |

Nếu sau này đổi ý một điểm, sửa đúng các dòng ở cột cuối và dòng tương ứng ở mục 12.

## 14. Chỗ báo cáo cần sửa theo tài liệu này

Danh sách đầy đủ kèm nội dung dán sẵn: `BaoCao_CanSua_Sau_Chot_2026-11-08.md` cùng thư mục; toàn bộ 18 hình cần thay gom ở `hinh-can-thay/` (mục 9 của tệp đó). Tóm tắt:

| Chỗ trong báo cáo | Sửa |
|---|---|
| Hình 3.26 (STM-04) | Thêm mũi tên Đã báo giá → Đang xác minh; hình mới `hinh-cap-nhat/STM PH2 Yêu cầu tour riêng.png` |
| Hình 3.29 (STM-07) | Thêm mũi tên Hết hạn → Đã xác nhận, bỏ ký hiệu kết thúc dưới Hết hạn, đổi nhãn Đã xác nhận → Hoàn thành; hình mới `hinh-cap-nhat/STM PH3 Đơn đặt tour.png` |
| Hình 3.35c, 3.36c, 3.36a | Thêm năm cột mới; mặc định `BAOGIA.TrangThai = 'Sent'`, `LICHKHOIHANH.TrangThai = 'Open'`; hình mới ở `hinh-cap-nhat/` |
| Bảng 4.7 (DONDAT) | Thêm bốn dòng cột, sửa "Ràng buộc bổ sung" |
| Bảng 4.12 (HANG_MUC_BAO_GIA) | Thêm dòng `TrongGoi`, sửa câu "không thêm cột mới vào lược đồ" |
| Danh sách lược đồ luận lý (sau Hình 3.35) | Thêm cột vào dòng `DONDAT`, `HANGMUCBAOGIA` |
| Bảng 3.1 và mục tác nhân của Chương 1 | Quản trị viên có đủ mọi quyền, kể cả ghi nhận thu và hoàn |
| UC19 (Excel UseCase), ACT UC19, ACT UC13, SCR17 (Excel ScreenDesign) | Thêm bước, nút và bộ lọc "Hoàn thành đơn"; kiểm tra ngày đi chưa qua khi hủy. Hình đã vẽ lại ở `hinh-can-thay/` |
| Hình 3.1, 3.5, 3.7, 3.8, 3.10, 3.13, UC20, ACT UC20, ACT PH3, SD hủy và hoàn tiền; mục 1.1.4, BR3.7, mô tả PH3, US20; Excel UseCase (ma trận quyền, UC20), Excel ScreenDesign (SCR17) | Gộp "Nhân viên có quyền tài chính" vào "Nhân viên tư vấn và điều hành" (Duy chọn phương án A, 08/11); hình đã vẽ lại ở `gop-nv-tai-chinh/`, danh sách đầy đủ ở mục 7 của tệp trên |
