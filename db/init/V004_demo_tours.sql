/*
 V004 - Bốn tour có sẵn để demo luồng đặt tour trong đồ án.
 Chạy sau DongNamTrip_Full.sql, V001, V002, V003; không chạy lại script tổng hợp.
 Giá, sức chứa và điều kiện hủy dưới đây là dữ liệu demo, không phải báo giá nhà cung cấp.
 Chỉ chuyển bốn tour tham khảo còn Draft; nhận diện theo tên/khu vực, không cố định ID.
 Chạy lại không đổi chương trình đã chuẩn bị, không ghi đè số chỗ giữ hay nhân đôi lịch.
 Lịch mới: ba thứ Bảy liên tiếp, bắt đầu ít nhất bảy ngày sau ngày chạy (giờ Việt Nam).
*/
USE [DongNamTrip];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.DONDAT', N'KhoanCanThu') IS NULL
        THROW 50001, N'V004: cần áp dụng V003_booking_quotation.sql trước.', 1;

    DECLARE @Tours TABLE (
        Code varchar(10) PRIMARY KEY,
        OriginalName nvarchar(150), DemoName nvarchar(150), Region nvarchar(50),
        Category nvarchar(100), Duration nvarchar(50), Price decimal(18,0),
        Capacity int, StartMinute int, Summary nvarchar(1000),
        TourId int NULL, NeedsSetup bit NOT NULL DEFAULT (0)
    );
    INSERT @Tours (Code, OriginalName, DemoName, Region, Category, Duration, Price, Capacity, StartMinute, Summary)
    VALUES
    ('VF03', N'Tham khảo Viet Fun VF03 - Khám phá địa đạo Củ Chi',
        N'Củ Chi - Hành trình về vùng đất thép', N'TP. Hồ Chí Minh', N'Văn hóa', N'Nửa ngày',
        500000, 24, 450,
        N'Khởi hành từ trung tâm TP. Hồ Chí Minh lúc 07:30, tham quan địa đạo Củ Chi và tìm hiểu lịch sử, trở về vào đầu giờ chiều. Gói demo gồm xe khứ hồi, vé tham quan và hướng dẫn viên; không gồm ăn trưa, trải nghiệm bắn súng và chi phí cá nhân. Tour mẫu phục vụ kiểm thử đồ án.'),
    ('VF04', N'Tham khảo Viet Fun VF04 - Cao Đài và Củ Chi',
        N'Cao Đài - Củ Chi: Sắc màu văn hóa và lịch sử', N'Tây Ninh', N'Văn hóa', N'1 ngày',
        730000, 28, 360,
        N'Khởi hành từ TP. Hồ Chí Minh lúc 06:00; buổi sáng tìm hiểu kiến trúc Tòa Thánh Cao Đài Tây Ninh, ăn trưa rồi tham quan địa đạo Củ Chi trên đường trở về. Gói demo gồm xe khứ hồi, hướng dẫn viên, vé tham quan và một bữa trưa; không gồm chi phí cá nhân. Tour mẫu phục vụ kiểm thử đồ án.'),
    ('VF184', N'Tham khảo Viet Fun VF184 - Vũng Tàu một ngày',
        N'Vũng Tàu - Biển xanh và dấu ấn phố biển', N'Vũng Tàu', N'Kết hợp', N'1 ngày',
        1050000, 30, 390,
        N'Khởi hành từ TP. Hồ Chí Minh lúc 06:30. Tham quan Bạch Dinh, khu vực Tượng Chúa Kitô Vua và Mũi Nghinh Phong; buổi chiều nghỉ ngơi, dạo biển Bãi Sau rồi trở về. Gói demo gồm xe khứ hồi, hướng dẫn viên, vé trong chương trình và bữa trưa; không gồm dịch vụ biển, mua sắm và chi phí cá nhân. Tour mẫu phục vụ kiểm thử đồ án.'),
    ('VF180', N'Tham khảo Viet Fun VF180 - Bình Châu, Hồ Cốc; ngủ Bình Châu',
        N'Bình Châu - Hồ Cốc: Nghỉ dưỡng 2 ngày 1 đêm', N'Vũng Tàu', N'Kết hợp', N'2 ngày 1 đêm',
        2300000, 20, 420,
        N'Khởi hành từ TP. Hồ Chí Minh lúc 07:00. Ngày 1 trải nghiệm khoáng nóng Bình Châu và nghỉ đêm tại khu vực Bình Châu; ngày 2 thư giãn ở biển Hồ Cốc trước khi trở về. Gói demo gồm xe khứ hồi, hướng dẫn viên, một đêm phòng đôi dùng chung, vé ngâm khoáng cơ bản và các bữa ăn trong chương trình; không gồm spa riêng, phụ thu phòng đơn và chi phí cá nhân. Tour mẫu phục vụ kiểm thử đồ án.');

    -- Mỗi tour phải khớp duy nhất một bản ghi của danh mục gốc hoặc lần chạy trước.
    IF EXISTS (
        SELECT 1 FROM @Tours s
        WHERE (SELECT COUNT(*) FROM dbo.TOUR t WITH (UPDLOCK, HOLDLOCK)
               WHERE t.KhuVuc = s.Region AND t.TenTour IN (s.OriginalName, s.DemoName)) <> 1
    )
        THROW 50002, N'V004: thiếu hoặc trùng tên tour nguồn; chưa thay đổi dữ liệu.', 1;

    UPDATE s SET TourId = t.MaTour, NeedsSetup = CASE WHEN t.TenTour = s.OriginalName THEN 1 ELSE 0 END
    FROM @Tours s JOIN dbo.TOUR t ON t.KhuVuc = s.Region AND t.TenTour IN (s.OriginalName, s.DemoName);

    -- Không đổi chương trình/giá của tour đã được vận hành hoặc có đơn đặt.
    IF EXISTS (
        SELECT 1 FROM @Tours s JOIN dbo.TOUR t ON t.MaTour = s.TourId
        WHERE s.NeedsSetup = 1 AND (t.TrangThai <> 'Draft' OR EXISTS (
            SELECT 1 FROM dbo.LICHKHOIHANH l LEFT JOIN dbo.DONDAT d ON d.MaLKH = l.MaLKH
            WHERE l.MaTour = t.MaTour AND (l.SoChoDaGiu > 0 OR d.MaDon IS NOT NULL)
        ))
    )
        THROW 50003, N'V004: tour nguồn không còn là nháp trống; cần kiểm tra trước khi đổi chương trình.', 1;

    DECLARE @Stops TABLE (
        Code varchar(10), Region nvarchar(50), PlaceName nvarchar(120),
        DayNumber int, StopOrder int, PlaceId int NULL,
        PRIMARY KEY (Code, DayNumber, StopOrder)
    );
    INSERT @Stops (Code, Region, PlaceName, DayNumber, StopOrder) VALUES
        ('VF03', N'TP. Hồ Chí Minh', N'Địa đạo Củ Chi', 1, 1),
        ('VF04', N'Tây Ninh', N'Tòa Thánh Cao Đài Tây Ninh', 1, 1),
        ('VF04', N'TP. Hồ Chí Minh', N'Địa đạo Củ Chi', 1, 2),
        ('VF184', N'Vũng Tàu', N'Bạch Dinh', 1, 1),
        ('VF184', N'Vũng Tàu', N'Tượng Chúa Kitô Vua Vũng Tàu', 1, 2),
        ('VF184', N'Vũng Tàu', N'Mũi Nghinh Phong', 1, 3),
        ('VF184', N'Vũng Tàu', N'Bãi Sau Vũng Tàu', 1, 4),
        ('VF180', N'Vũng Tàu', N'Suối khoáng nóng Bình Châu', 1, 1),
        ('VF180', N'Vũng Tàu', N'Biển Hồ Cốc', 2, 1);

    IF EXISTS (
        SELECT 1 FROM @Stops s
        WHERE (SELECT COUNT(*) FROM dbo.DIADIEM d
               WHERE d.KhuVuc = s.Region AND d.TenDD = s.PlaceName AND d.TrangThai = 'Published') <> 1
    )
        THROW 50004, N'V004: điểm dừng chưa tồn tại duy nhất hoặc chưa công bố.', 1;

    UPDATE s SET PlaceId = d.MaDD
    FROM @Stops s JOIN dbo.DIADIEM d ON d.KhuVuc = s.Region AND d.TenDD = s.PlaceName AND d.TrangThai = 'Published';

    -- Giá và điều kiện dành riêng cho demo của DongNamTrip, bỏ thông tin nhà cung cấp ở bốn tour này.
    UPDATE t SET TenTour = s.DemoName, LoaiHinh = s.Category, ThoiLuong = s.Duration,
        GiaThamKhao = s.Price, TomTat = s.Summary,
        DieuKienHuy = N'Điều kiện demo: gửi yêu cầu hủy trên hệ thống; nhân viên xem xét và đối soát. Hủy từ 3 ngày trước ngày đi: hoàn 100% tiền đã thu; hủy muộn hơn: hoàn 50%; không tham gia: không hoàn. Mọi khoản hoàn cần nhân viên ghi nhận, không tự động chuyển tiền.',
        TrangThai = 'Published'
    FROM dbo.TOUR t JOIN @Tours s ON s.TourId = t.MaTour WHERE s.NeedsSetup = 1;

    DELETE p FROM dbo.TOUR_DIADIEM p JOIN @Tours s ON s.TourId = p.MaTour WHERE s.NeedsSetup = 1;
    INSERT dbo.TOUR_DIADIEM (MaTour, MaDD, NgayThu, ThuTu)
    SELECT t.TourId, s.PlaceId, s.DayNumber, s.StopOrder
    FROM @Stops s JOIN @Tours t ON t.Code = s.Code WHERE t.NeedsSetup = 1;

    DECLARE @Today date = CONVERT(date, DATEADD(hour, 7, SYSUTCDATETIME()));
    DECLARE @Earliest date = DATEADD(day, 7, @Today);
    -- 1900-01-01 là thứ Hai; phép tính không phụ thuộc SET DATEFIRST/ngôn ngữ SQL Server.
    DECLARE @FirstSaturday date = DATEADD(day, (5 - DATEDIFF(day, '19000101', @Earliest) % 7 + 7) % 7, @Earliest);

    INSERT dbo.LICHKHOIHANH (MaTour, NgayGioKhoiHanh, Gia, SucChua, SoChoDaGiu, TrangThai)
    SELECT s.TourId, dates.StartsAt, s.Price, s.Capacity, 0, 'Open'
    FROM @Tours s JOIN dbo.TOUR t ON t.MaTour = s.TourId
    CROSS JOIN (VALUES (0), (7), (14)) weeks(DayOffset)
    CROSS APPLY (SELECT DATEADD(minute, s.StartMinute, CONVERT(datetime2, DATEADD(day, weeks.DayOffset, @FirstSaturday))) AS StartsAt) dates
    WHERE t.TrangThai = 'Published' AND NOT EXISTS (
        SELECT 1 FROM dbo.LICHKHOIHANH existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.MaTour = s.TourId AND existing.NgayGioKhoiHanh = dates.StartsAt
    );

    COMMIT TRANSACTION;
    SELECT t.MaTour, t.TenTour, t.TrangThai, t.GiaThamKhao,
           (SELECT COUNT(*) FROM dbo.TOUR_DIADIEM p WHERE p.MaTour = t.MaTour) AS DiemDung,
           (SELECT COUNT(*) FROM dbo.LICHKHOIHANH l WHERE l.MaTour = t.MaTour AND l.TrangThai = 'Open' AND l.NgayGioKhoiHanh > DATEADD(hour, 7, SYSUTCDATETIME())) AS LichMo
    FROM dbo.TOUR t JOIN @Tours s ON s.TourId = t.MaTour ORDER BY t.MaTour;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
