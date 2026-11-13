/*
 V003 - Đặt tour, báo giá: cột mới, miền giá trị, chỉ mục (đặc tả sáu module, mục 9.1).
 Chạy sau V001, V002. An toàn khi chạy lại.
 db/init.sh chỉ chạy khi CSDL chưa tồn tại: với CSDL đã có, chạy tay tệp này (xem README).
 Dữ liệu PR #39 được chuẩn hóa trước khi thêm CHECK:
   BAOGIA Issued->Sent, Replaced->Superseded, Draft->Superseded;
   YEUCAUTOURRIENG Reviewing->Verifying, NeedsInfo->Verifying;
   XACMINHDICHVU NeedsInfo->Unavailable (ghi chú giữ nguyên ý nghĩa cũ).
*/
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

/* 2. Chuẩn hóa dòng cũ (trước khi thêm CHECK) */
UPDATE dbo.LICHKHOIHANH SET TrangThai = 'Inactive'   WHERE TrangThai = 'Draft';
UPDATE dbo.BAOGIA       SET TrangThai = 'Sent'       WHERE TrangThai = 'Issued';
UPDATE dbo.BAOGIA       SET TrangThai = 'Superseded' WHERE TrangThai IN ('Replaced', 'Draft');
UPDATE dbo.YEUCAUTOURRIENG SET TrangThai = 'Verifying' WHERE TrangThai IN ('Reviewing', 'NeedsInfo');
UPDATE dbo.XACMINHDICHVU
   SET GhiChu = LEFT(CONCAT(N'[Cần bổ sung thông tin] ', COALESCE(GhiChu, N'')), 1000), KetQua = 'Unavailable'
 WHERE KetQua = 'NeedsInfo';
GO

/* Báo lỗi rõ nếu còn trạng thái đơn lạ (không tự đoán) */
IF EXISTS (SELECT 1 FROM dbo.DONDAT WHERE TrangThai NOT IN ('Pending', 'Confirmed', 'CancelRequested', 'Cancelled', 'Expired', 'Completed'))
BEGIN
    DECLARE @bad nvarchar(400) = (SELECT STRING_AGG(CAST(v AS nvarchar(50)), N', ') FROM (SELECT DISTINCT TrangThai AS v FROM dbo.DONDAT
        WHERE TrangThai NOT IN ('Pending', 'Confirmed', 'CancelRequested', 'Cancelled', 'Expired', 'Completed')) x);
    RAISERROR (N'V003: DONDAT.TrangThai có giá trị lạ (%s). Chuẩn hóa tay rồi chạy lại.', 16, 1, @bad);
    SET NOEXEC ON;
END
GO

/* DEFAULT mới: tìm theo cột, không theo tên cứng */
DECLARE @name sysname, @sql nvarchar(400);
SELECT @name = dc.name FROM sys.default_constraints dc
  JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
 WHERE dc.parent_object_id = OBJECT_ID(N'dbo.LICHKHOIHANH') AND c.name = N'TrangThai' AND dc.definition LIKE N'%Draft%';
IF @name IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE dbo.LICHKHOIHANH DROP CONSTRAINT ' + QUOTENAME(@name) + N'; '
             + N'ALTER TABLE dbo.LICHKHOIHANH ADD CONSTRAINT DF_LICHKHOIHANH_TrangThai DEFAULT (''Open'') FOR TrangThai;';
    EXEC (@sql);
END
SET @name = NULL;
SELECT @name = dc.name FROM sys.default_constraints dc
  JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
 WHERE dc.parent_object_id = OBJECT_ID(N'dbo.BAOGIA') AND c.name = N'TrangThai' AND dc.definition LIKE N'%Draft%';
IF @name IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE dbo.BAOGIA DROP CONSTRAINT ' + QUOTENAME(@name) + N'; '
             + N'ALTER TABLE dbo.BAOGIA ADD CONSTRAINT DF_BAOGIA_TrangThai DEFAULT (''Sent'') FOR TrangThai;';
    EXEC (@sql);
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

/* 4. Chỉ mục phục vụ quy tắc */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BAOGIA_MaYC_Sent' AND object_id = OBJECT_ID(N'dbo.BAOGIA'))
    CREATE UNIQUE INDEX UX_BAOGIA_MaYC_Sent ON dbo.BAOGIA (MaYC) WHERE TrangThai = 'Sent';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DONDAT_Pending_TK_LKH' AND object_id = OBJECT_ID(N'dbo.DONDAT'))
    CREATE UNIQUE INDEX UX_DONDAT_Pending_TK_LKH ON dbo.DONDAT (MaTK, MaLKH) WHERE TrangThai = 'Pending' AND MaLKH IS NOT NULL;
GO
SET NOEXEC OFF;
GO
