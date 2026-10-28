/*
===============================================================================
 DONGNAMTRIP - KHỞI TẠO CSDL VÀ NẠP DỮ LIỆU DÙNG CHUNG CHO NHÓM
 Phiên bản: 2026-10-28.full.1 | SQL Server 2019 trở lên | UTF-8 có BOM
===============================================================================


 CÁCH CHẠY TRÊN MÁY MỖI THÀNH VIÊN
 0. Khi thay database trên máy đang chạy dự án, dừng API/Web/WinForms trước.
    Khởi động lại API sau khi tạo mới và đăng nhập lại để loại phiên cũ.
 1. Mở SSMS, kết nối SQL Server trên máy của mình bằng tài khoản có quyền
    tạo database, bảng, chỉ mục và ghi dữ liệu.
 2. Ctrl+O mở file này, nhấn F5 chạy TOÀN BỘ nội dung.
 3. Database DongNamTrip sẽ được tạo nếu chưa có. Bảng và dữ liệu được tạo
    trong một transaction; nếu gặp lỗi, phần tạo bảng/nạp dữ liệu hoàn tác.
 4. Kiểm tra thông báo và các bảng kết quả ở cuối. Refresh Object Explorer.
 5. Cấu hình connection string của API đến SQL Server trên máy của thành viên.
    Tên server DUCK\DUCK là tên máy của Đức, KHÔNG cố định trong file này.

 THÀNH PHẦN
 - 25 bảng: 24 bảng theo báo cáo + NHATKYTHAOTAC.
 - Giữ nguyên tên cột, kiểu, nullable, PK/FK, CHECK, UNIQUE và chỉ mục
   của schema đang dùng; không thêm MaVaiTro vào TAIKHOAN.
 - 6 vai trò; 3 ngôn ngữ vi/en/ko.
 - 7 tài khoản kiểm thử Active; 7 liên kết tài khoản-vai trò.
 - 30 địa điểm Published; 60 gợi ý trải nghiệm Published.
 - 8 tour THAM KHẢO Draft; 20 liên kết tour-địa điểm chính.
 - Một nhật ký nạp danh mục, có nguồn công khai và ngày tra cứu 28/10/2026.

 TÀI KHOẢN KIỂM THỬ - MẬT KHẨU BAN ĐẦU CHUNG: DntTest@2026!
 admin@dongnamtrip.test       -> Admin
 operator@dongnamtrip.test    -> Operator
 editor@dongnamtrip.test      -> Editor
 finance@dongnamtrip.test     -> Finance
 publisher@dongnamtrip.test   -> Publisher
 customer1@dongnamtrip.test   -> Customer
 customer2@dongnamtrip.test   -> Customer
 Đây là tài khoản Development dùng chung để kiểm thử, không phải tài khoản
 vận hành thật. SĐT 0900000001-0900000007 là số MINH HỌA, không phải liên hệ.
 Mật khẩu trong TAIKHOAN là hash IdentityV3, mỗi tài khoản có salt riêng:
 PBKDF2-HMAC-SHA512, 100000 vòng, salt 16 byte, subkey 32 byte, Base64 84 ký tự.
 Đối chiếu định dạng PasswordHasher<TUser> của ASP.NET Core:
 https://source.dot.net/Microsoft.Extensions.Identity.Core/PasswordHasher.cs.html
 Không dùng HASHBYTES/SHA đơn thuần thay cho PasswordHasher.

 LƯU Ý VỚI CODE SEED CỦA API HIỆN CÓ
 - Các tài khoản trên đã có trong SQL, API không cần tạo lại.
 - User Secrets trên máy cũ có thể còn mật khẩu ngẫu nhiên của database cũ.
   Với database MỚI tạo từ file này, dùng DntTest@2026! để đăng nhập ban đầu.
 - Seeder phải giữ tài khoản đã tồn tại. Có thể tắt cấu hình
   DevelopmentTestAccounts:Enabled và BootstrapAdmin:Enabled khi không cần.
 - Đây chỉ tạo dữ liệu SQL; không tự sửa API, menu Web hoặc chức năng WinForms.
 - Phiên đang nằm trong bộ nhớ API không được tạo bởi file SQL này.

 PHÂN BIỆT DỮ LIỆU DANH MỤC VÀ DỊCH VỤ ĐÃ XÁC NHẬN
 - Địa điểm có thật và chương trình tour tham khảo có nguồn ghi trong @Nguon.
 - Thời lượng tham quan/trải nghiệm là gợi ý biên tập để lập kế hoạch,
   không phải lịch hoặc suất do đơn vị tổ chức xác nhận.
 - KhuVuc là nhãn vùng sản phẩm của đề tài, không phải danh mục hành chính.
 - Tour giữ Draft: giá từ công khai tại ngày tra cứu, chưa là cam kết bán của
   DongNamTrip; chưa có xác nhận ngày khởi hành, giá bán, sức chứa/chính sách.
 - Không tự tạo SUAT/LICHKHOIHANH, đơn đặt, khoản thu/hoàn hoặc quyền nghe.
   Các bảng này đã có cấu trúc, dữ liệu sẽ được nhập qua chức năng nghiệp vụ.
 - Không tạo tệp audio/đường dẫn giả; THUYETMINH/BANDICH/TEPAUDIO chưa seed.
 - Tọa độ, giờ mở cửa, ảnh và phí hoạt động chưa xác minh giữ NULL.
 - SoDienThoai vẫn cho NULL theo schema; tài khoản thật có thể bổ sung sau.

 QUY TẮC VẪN DO API THỰC HIỆN
 - Phân quyền, chủ sở hữu; giữ/trả chỗ đúng một lần; xử lý yêu cầu đồng thời.
 - Chốt giá và điều kiện; tính hạng mục báo giá; không thu/hoàn trùng hoặc vượt.
 - Liên kết phiên bản lịch trình, báo giá, đơn và phạm vi nghe hợp lệ.
 - Ghi nhật ký nghiệp vụ; không ghi mật khẩu/hash/token vào JSON nhật ký.
 - DATETIME2 theo UTC; DATE/TIME của SUAT theo ngày giờ địa phương Việt Nam.

 KIỂM CHỨNG
 Đã đối chiếu schema gốc, dữ liệu, quan hệ và định dạng hash bằng công cụ
 kiểm tra. Không có SQL Server trong môi trường tạo file nên chưa thực thi
 file full trên một SQL Server thật; kết quả SSMS/API của bạn là bước xác nhận.
===============================================================================
*/
USE [master];
GO
IF DB_ID(N'DongNamTrip') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [DongNamTrip];');
END;
GO
USE [DongNamTrip];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

IF DB_NAME() <> N'DongNamTrip'
BEGIN
    ;THROW 51200, N'Sai database. Dừng để tránh tạo bảng nhầm. Kiểm tra lỗi CREATE DATABASE/USE.', 1;
END;
IF TRY_CONVERT(int, SERVERPROPERTY('ProductMajorVersion')) < 15
BEGIN
    ;THROW 51201, N'File yêu cầu SQL Server 2019 trở lên.', 1;
END;

DECLARE @PhienBanFull nvarchar(100) = N'2026-10-28.full.1';
DECLARE @CacBang TABLE (TenBang sysname PRIMARY KEY);
INSERT INTO @CacBang VALUES
    (N'VAITRO'),
    (N'TAIKHOAN'),
    (N'TAIKHOAN_VAITRO'),
    (N'DIADIEM'),
    (N'HOATDONG'),
    (N'SUAT'),
    (N'TOUR'),
    (N'TOUR_DIADIEM'),
    (N'LICHKHOIHANH'),
    (N'LICHTRINH'),
    (N'LICHTRINH_DIADIEM'),
    (N'LICHTRINH_SUAT'),
    (N'PHIENBANLICHTRINH'),
    (N'YEUCAUTOURRIENG'),
    (N'XACMINHDICHVU'),
    (N'BAOGIA'),
    (N'HANGMUCBAOGIA'),
    (N'DONDAT'),
    (N'GIAODICHTIEN'),
    (N'NGONNGU'),
    (N'THUYETMINH'),
    (N'BANDICH'),
    (N'TEPAUDIO'),
    (N'QUYENNGHE'),
    (N'NHATKYTHAOTAC');

-- Nhận diện bản full đã nạp. Không dùng marker schema cũ để giả định đã có seed.
IF EXISTS (
    SELECT 1 FROM sys.extended_properties
    WHERE class = 0 AND name = N'DongNamTrip.FullBundleVersion'
      AND CONVERT(nvarchar(100), value) = @PhienBanFull
)
AND NOT EXISTS (
    SELECT 1 FROM @CacBang b
    WHERE OBJECT_ID(N'dbo.' + QUOTENAME(b.TenBang), N'U') IS NULL
)
BEGIN
    PRINT N'Bản full này đã được nạp. Giữ nguyên dữ liệu và mật khẩu đang có.';
    SELECT DB_NAME() AS CoSoDuLieu, @PhienBanFull AS PhienBan, N'Không thay đổi' AS KetQua;
    RETURN;
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
BEGIN
    ;THROW 51202, N'Database đã có bảng từ bản khác. File không tự xóa hoặc nâng cấp. Hãy giữ dữ liệu hiện có và xử lý việc tạo mới/nâng cấp riêng.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @KhoaFull int;
    EXEC @KhoaFull = sys.sp_getapplock
        @Resource=N'DongNamTrip.FullSetup', @LockMode='Exclusive',
        @LockOwner='Transaction', @LockTimeout=15000;
    IF @KhoaFull < 0
    BEGIN
        ;THROW 51203, N'Không lấy được khóa khởi tạo database. Thử lại sau.', 1;
    END;
    -- Kiểm tra lại sau khi có khóa để hai lần cài không tạo trùng.
    IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
    BEGIN
        ;THROW 51204, N'Database đã được tiến trình khác tạo bảng. Dừng lần chạy này.', 1;
    END;

    -- PHẦN 1: 25 BẢNG, RÀNG BUỘC VÀ CHỈ MỤC.
    -- 01. Bảng vai trò: Lưu danh mục vai trò và ý nghĩa của từng vai trò.
    CREATE TABLE [dbo].[VAITRO]
    (
        [MaVaiTro] int IDENTITY(1,1) NOT NULL,
        [TenVaiTro] nvarchar(50) NOT NULL,
        [MoTa] nvarchar(250) NULL,
        CONSTRAINT [PK_VAITRO] PRIMARY KEY ([MaVaiTro]),
        CONSTRAINT [UQ_VAITRO_TenVaiTro] UNIQUE ([TenVaiTro]),
        CONSTRAINT [CK_VAITRO_TenVaiTroKhongRong] CHECK (LEN(LTRIM(RTRIM([TenVaiTro]))) > 0)
    );

    -- 02. Bảng tài khoản: Lưu thông tin đăng nhập, liên hệ và trạng thái tài khoản.
    CREATE TABLE [dbo].[TAIKHOAN]
    (
        [MaTK] int IDENTITY(1,1) NOT NULL,
        [HoTen] nvarchar(100) NOT NULL,
        [Email] varchar(254) COLLATE Latin1_General_100_CI_AS NOT NULL,
        [MatKhauBam] varchar(255) NOT NULL,
        [SoDienThoai] varchar(15) NULL,
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_TAIKHOAN_TrangThai] DEFAULT ('Active'),
        [NgayTao] datetime2 NOT NULL CONSTRAINT [DF_TAIKHOAN_NgayTao] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_TAIKHOAN] PRIMARY KEY ([MaTK]),
        CONSTRAINT [UQ_TAIKHOAN_Email] UNIQUE ([Email]),
        CONSTRAINT [CK_TAIKHOAN_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_TAIKHOAN_HoTenKhongRong] CHECK (LEN(LTRIM(RTRIM([HoTen]))) > 0),
        CONSTRAINT [CK_TAIKHOAN_MatKhauBamKhongRong] CHECK (LEN(LTRIM(RTRIM([MatKhauBam]))) > 0),
        CONSTRAINT [CK_TAIKHOAN_EmailKhongRong] CHECK (LEN(LTRIM(RTRIM([Email]))) > 0),
        CONSTRAINT [CK_TAIKHOAN_EmailCoBan] CHECK ([Email] LIKE '%_@_%._%' AND [Email] NOT LIKE '% %'),
        CONSTRAINT [CK_TAIKHOAN_EmailDaTrim] CHECK (DATALENGTH([Email]) = DATALENGTH(LTRIM(RTRIM([Email]))))
    );

    -- 03. Bảng liên kết tài khoản và vai trò: Lưu việc gán một hoặc nhiều vai trò cho tài khoản.
    CREATE TABLE [dbo].[TAIKHOAN_VAITRO]
    (
        [MaTK] int NOT NULL,
        [MaVaiTro] int NOT NULL,
        [NgayGan] datetime2 NOT NULL CONSTRAINT [DF_TAIKHOAN_VAITRO_NgayGan] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_TAIKHOAN_VAITRO] PRIMARY KEY ([MaTK], [MaVaiTro]),
        CONSTRAINT [FK_TAIKHOAN_VAITRO_MaTK] FOREIGN KEY ([MaTK]) REFERENCES [dbo].[TAIKHOAN] ([MaTK]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_TAIKHOAN_VAITRO_MaVaiTro] FOREIGN KEY ([MaVaiTro]) REFERENCES [dbo].[VAITRO] ([MaVaiTro]) ON DELETE NO ACTION ON UPDATE NO ACTION
    );

    -- 04. Bảng địa điểm: Lưu thông tin địa điểm, vị trí, thời lượng và trạng thái công bố.
    CREATE TABLE [dbo].[DIADIEM]
    (
        [MaDD] int IDENTITY(1,1) NOT NULL,
        [TenDD] nvarchar(120) NOT NULL,
        [KhuVuc] nvarchar(50) NOT NULL,
        [LoaiHinh] nvarchar(100) NOT NULL,
        [DiaChi] nvarchar(250) NOT NULL,
        [ViDo] decimal(9,6) NULL,
        [KinhDo] decimal(9,6) NULL,
        [MoTa] nvarchar(3000) NOT NULL,
        [ThoiLuongPhut] int NOT NULL,
        [GioMoCua] time NULL,
        [GioDongCua] time NULL,
        [AnhMinhHoa] nvarchar(500) NULL,
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_DIADIEM_TrangThai] DEFAULT ('Draft'),
        CONSTRAINT [PK_DIADIEM] PRIMARY KEY ([MaDD]),
        CONSTRAINT [CK_DIADIEM_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_DIADIEM_TenDDKhongRong] CHECK (LEN(LTRIM(RTRIM([TenDD]))) > 0),
        CONSTRAINT [CK_DIADIEM_ViDo] CHECK ([ViDo] IS NULL OR [ViDo] BETWEEN -90 AND 90),
        CONSTRAINT [CK_DIADIEM_KinhDo] CHECK ([KinhDo] IS NULL OR [KinhDo] BETWEEN -180 AND 180),
        CONSTRAINT [CK_DIADIEM_ThoiLuong] CHECK ([ThoiLuongPhut] BETWEEN 15 AND 1440),
        CONSTRAINT [CK_DIADIEM_GioHoatDong] CHECK ([GioMoCua] IS NULL OR [GioDongCua] IS NULL OR [GioDongCua] > [GioMoCua]),
        CONSTRAINT [CK_DIADIEM_TrangThai] CHECK ([TrangThai] IN ('Draft','Published','Inactive')),
        CONSTRAINT [CK_DIADIEM_DiaChiKhongRong] CHECK (LEN(LTRIM(RTRIM([DiaChi]))) > 0),
        CONSTRAINT [CK_DIADIEM_MoTaKhongRong] CHECK (LEN(LTRIM(RTRIM([MoTa]))) > 0)
    );

    -- 05. Bảng hoạt động: Lưu hoạt động tại địa điểm, điều kiện tham gia và phí tham khảo.
    CREATE TABLE [dbo].[HOATDONG]
    (
        [MaHD] int IDENTITY(1,1) NOT NULL,
        [MaDD] int NOT NULL,
        [TenHD] nvarchar(150) NOT NULL,
        [MoTa] nvarchar(2000) NULL,
        [ThoiLuongPhut] int NOT NULL,
        [DieuKien] nvarchar(500) NULL,
        [PhiThamKhao] decimal(18,0) NULL,
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_HOATDONG_TrangThai] DEFAULT ('Draft'),
        CONSTRAINT [PK_HOATDONG] PRIMARY KEY ([MaHD]),
        CONSTRAINT [FK_HOATDONG_MaDD] FOREIGN KEY ([MaDD]) REFERENCES [dbo].[DIADIEM] ([MaDD]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_HOATDONG_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_HOATDONG_TenHDKhongRong] CHECK (LEN(LTRIM(RTRIM([TenHD]))) > 0),
        CONSTRAINT [CK_HOATDONG_ThoiLuong] CHECK ([ThoiLuongPhut] > 0),
        CONSTRAINT [CK_HOATDONG_PhiThamKhao] CHECK ([PhiThamKhao] IS NULL OR [PhiThamKhao] >= 0)
    );

    -- 06. Bảng suất hoạt động: Lưu ngày giờ tổ chức và trạng thái công bố của hoạt động.
    CREATE TABLE [dbo].[SUAT]
    (
        [MaSuat] int IDENTITY(1,1) NOT NULL,
        [MaHD] int NOT NULL,
        [Ngay] date NOT NULL,
        [GioBatDau] time NOT NULL,
        [GioKetThuc] time NOT NULL,
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_SUAT_TrangThai] DEFAULT ('Published'),
        CONSTRAINT [PK_SUAT] PRIMARY KEY ([MaSuat]),
        CONSTRAINT [FK_SUAT_MaHD] FOREIGN KEY ([MaHD]) REFERENCES [dbo].[HOATDONG] ([MaHD]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [UQ_SUAT_MaHD_Ngay_GioBatDau] UNIQUE ([MaHD], [Ngay], [GioBatDau]),
        CONSTRAINT [CK_SUAT_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_SUAT_ThoiGian] CHECK ([GioKetThuc] > [GioBatDau]),
        CONSTRAINT [CK_SUAT_TrangThai] CHECK ([TrangThai] IN ('Published','Inactive'))
    );

    -- 07. Bảng tour: Lưu chương trình tour mẫu, giá tham khảo và điều kiện hủy.
    CREATE TABLE [dbo].[TOUR]
    (
        [MaTour] int IDENTITY(1,1) NOT NULL,
        [TenTour] nvarchar(150) NOT NULL,
        [KhuVuc] nvarchar(50) NOT NULL,
        [LoaiHinh] nvarchar(100) NOT NULL,
        [ThoiLuong] nvarchar(50) NOT NULL,
        [GiaThamKhao] decimal(18,0) NOT NULL,
        [TomTat] nvarchar(1000) NOT NULL,
        [DieuKienHuy] nvarchar(2000) NULL,
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_TOUR_TrangThai] DEFAULT ('Draft'),
        CONSTRAINT [PK_TOUR] PRIMARY KEY ([MaTour]),
        CONSTRAINT [CK_TOUR_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_TOUR_TenTourKhongRong] CHECK (LEN(LTRIM(RTRIM([TenTour]))) > 0),
        CONSTRAINT [CK_TOUR_GiaThamKhao] CHECK ([GiaThamKhao] >= 0),
        CONSTRAINT [CK_TOUR_ThoiLuongKhongRong] CHECK (LEN(LTRIM(RTRIM([ThoiLuong]))) > 0),
        CONSTRAINT [CK_TOUR_TomTatKhongRong] CHECK (LEN(LTRIM(RTRIM([TomTat]))) > 0)
    );

    -- 08. Bảng điểm dừng của tour: Liên kết tour với địa điểm, ngày thứ và thứ tự tham quan.
    CREATE TABLE [dbo].[TOUR_DIADIEM]
    (
        [MaTour] int NOT NULL,
        [MaDD] int NOT NULL,
        [NgayThu] int NOT NULL,
        [ThuTu] int NOT NULL,
        CONSTRAINT [PK_TOUR_DIADIEM] PRIMARY KEY ([MaTour], [MaDD]),
        CONSTRAINT [FK_TOUR_DIADIEM_MaTour] FOREIGN KEY ([MaTour]) REFERENCES [dbo].[TOUR] ([MaTour]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_TOUR_DIADIEM_MaDD] FOREIGN KEY ([MaDD]) REFERENCES [dbo].[DIADIEM] ([MaDD]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [UQ_TOUR_DIADIEM_MaTour_NgayThu_ThuTu] UNIQUE ([MaTour], [NgayThu], [ThuTu]),
        CONSTRAINT [CK_TOUR_DIADIEM_NgayThu] CHECK ([NgayThu] > 0),
        CONSTRAINT [CK_TOUR_DIADIEM_ThuTu] CHECK ([ThuTu] > 0)
    );

    -- 09. Bảng lịch khởi hành: Lưu từng đợt tổ chức tour, giá, sức chứa và số chỗ đã giữ.
    CREATE TABLE [dbo].[LICHKHOIHANH]
    (
        [MaLKH] int IDENTITY(1,1) NOT NULL,
        [MaTour] int NOT NULL,
        [NgayGioKhoiHanh] datetime2 NOT NULL,
        [Gia] decimal(18,0) NOT NULL,
        [SucChua] int NOT NULL,
        [SoChoDaGiu] int NOT NULL CONSTRAINT [DF_LICHKHOIHANH_SoChoDaGiu] DEFAULT (0),
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_LICHKHOIHANH_TrangThai] DEFAULT ('Draft'),
        CONSTRAINT [PK_LICHKHOIHANH] PRIMARY KEY ([MaLKH]),
        CONSTRAINT [FK_LICHKHOIHANH_MaTour] FOREIGN KEY ([MaTour]) REFERENCES [dbo].[TOUR] ([MaTour]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_LICHKHOIHANH_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_LICHKHOIHANH_Gia] CHECK ([Gia] >= 0),
        CONSTRAINT [CK_LICHKHOIHANH_SucChua] CHECK ([SucChua] > 0),
        CONSTRAINT [CK_LICHKHOIHANH_SoChoDaGiu] CHECK ([SoChoDaGiu] >= 0 AND [SoChoDaGiu] <= [SucChua])
    );

    -- 10. Bảng lịch trình: Lưu kế hoạch chuyến đi hiện tại và tài khoản sở hữu.
    CREATE TABLE [dbo].[LICHTRINH]
    (
        [MaLT] int IDENTITY(1,1) NOT NULL,
        [MaTK] int NOT NULL,
        [TenLT] nvarchar(100) NOT NULL,
        [NgayDi] date NOT NULL,
        [SoKhach] int NOT NULL,
        [NoiXuatPhat] nvarchar(250) NULL,
        [PhuongTien] nvarchar(50) NULL,
        [NganSach] decimal(18,0) NULL,
        [NgayCapNhat] datetime2 NOT NULL CONSTRAINT [DF_LICHTRINH_NgayCapNhat] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_LICHTRINH] PRIMARY KEY ([MaLT]),
        CONSTRAINT [FK_LICHTRINH_MaTK] FOREIGN KEY ([MaTK]) REFERENCES [dbo].[TAIKHOAN] ([MaTK]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_LICHTRINH_TenLTKhongRong] CHECK (LEN(LTRIM(RTRIM([TenLT]))) > 0),
        CONSTRAINT [CK_LICHTRINH_SoKhach] CHECK ([SoKhach] > 0),
        CONSTRAINT [CK_LICHTRINH_NganSach] CHECK ([NganSach] IS NULL OR [NganSach] >= 0)
    );

    -- 11. Bảng địa điểm của lịch trình: Lưu các điểm dự kiến tham quan và thứ tự của lịch trình cá nhân.
    CREATE TABLE [dbo].[LICHTRINH_DIADIEM]
    (
        [MaLT] int NOT NULL,
        [MaDD] int NOT NULL,
        [NgayThu] int NOT NULL,
        [ThuTu] int NOT NULL,
        [GhiChu] nvarchar(500) NULL,
        CONSTRAINT [PK_LICHTRINH_DIADIEM] PRIMARY KEY ([MaLT], [MaDD]),
        CONSTRAINT [FK_LICHTRINH_DIADIEM_MaLT] FOREIGN KEY ([MaLT]) REFERENCES [dbo].[LICHTRINH] ([MaLT]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_LICHTRINH_DIADIEM_MaDD] FOREIGN KEY ([MaDD]) REFERENCES [dbo].[DIADIEM] ([MaDD]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [UQ_LICHTRINH_DIADIEM_MaLT_NgayThu_ThuTu] UNIQUE ([MaLT], [NgayThu], [ThuTu]),
        CONSTRAINT [CK_LICHTRINH_DIADIEM_NgayThu] CHECK ([NgayThu] > 0),
        CONSTRAINT [CK_LICHTRINH_DIADIEM_ThuTu] CHECK ([ThuTu] > 0)
    );

    -- 12. Bảng suất được chọn trong lịch trình: Lưu các suất hoạt động khách mong muốn tham gia tại từng địa điểm.
    CREATE TABLE [dbo].[LICHTRINH_SUAT]
    (
        [MaLT] int NOT NULL,
        [MaSuat] int NOT NULL,
        [MaDD] int NOT NULL,
        [GhiChu] nvarchar(500) NULL,
        CONSTRAINT [PK_LICHTRINH_SUAT] PRIMARY KEY ([MaLT], [MaSuat]),
        CONSTRAINT [FK_LICHTRINH_SUAT_MaLT] FOREIGN KEY ([MaLT]) REFERENCES [dbo].[LICHTRINH] ([MaLT]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_LICHTRINH_SUAT_MaSuat] FOREIGN KEY ([MaSuat]) REFERENCES [dbo].[SUAT] ([MaSuat]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_LICHTRINH_SUAT_MaDD] FOREIGN KEY ([MaDD]) REFERENCES [dbo].[DIADIEM] ([MaDD]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_LICHTRINH_SUAT_DiemTrongLichTrinh] FOREIGN KEY ([MaLT], [MaDD]) REFERENCES [dbo].[LICHTRINH_DIADIEM] ([MaLT], [MaDD]) ON DELETE NO ACTION ON UPDATE NO ACTION
    );

    -- 13. Bảng phiên bản lịch trình: Lưu bản chụp lịch trình tại thời điểm gửi yêu cầu hoặc chốt phương án.
    CREATE TABLE [dbo].[PHIENBANLICHTRINH]
    (
        [MaPB] int IDENTITY(1,1) NOT NULL,
        [MaLT] int NOT NULL,
        [SoPhienBan] int NOT NULL,
        [ThoiDiemTao] datetime2 NOT NULL CONSTRAINT [DF_PHIENBANLICHTRINH_ThoiDiemTao] DEFAULT (SYSUTCDATETIME()),
        [NoiDungJSON] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PHIENBANLICHTRINH] PRIMARY KEY ([MaPB]),
        CONSTRAINT [FK_PHIENBANLICHTRINH_MaLT] FOREIGN KEY ([MaLT]) REFERENCES [dbo].[LICHTRINH] ([MaLT]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [UQ_PHIENBANLICHTRINH_MaLT_SoPhienBan] UNIQUE ([MaLT], [SoPhienBan]),
        CONSTRAINT [CK_PHIENBANLICHTRINH_SoPhienBan] CHECK ([SoPhienBan] > 0),
        CONSTRAINT [CK_PHIENBANLICHTRINH_JSON] CHECK (ISJSON([NoiDungJSON]) = 1)
    );

    -- 14. Bảng yêu cầu tour riêng: Lưu nhu cầu tổ chức chuyến đi dựa trên một phiên bản lịch trình.
    CREATE TABLE [dbo].[YEUCAUTOURRIENG]
    (
        [MaYC] int IDENTITY(1,1) NOT NULL,
        [MaPB] int NOT NULL,
        [SoKhach] int NOT NULL,
        [LienHe] nvarchar(250) NOT NULL,
        [NhuCauDichVu] nvarchar(2000) NULL,
        [NgayGui] datetime2 NOT NULL CONSTRAINT [DF_YEUCAUTOURRIENG_NgayGui] DEFAULT (SYSUTCDATETIME()),
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_YEUCAUTOURRIENG_TrangThai] DEFAULT ('Submitted'),
        CONSTRAINT [PK_YEUCAUTOURRIENG] PRIMARY KEY ([MaYC]),
        CONSTRAINT [FK_YEUCAUTOURRIENG_MaPB] FOREIGN KEY ([MaPB]) REFERENCES [dbo].[PHIENBANLICHTRINH] ([MaPB]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_YEUCAUTOURRIENG_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_YEUCAUTOURRIENG_SoKhach] CHECK ([SoKhach] > 0),
        CONSTRAINT [CK_YEUCAUTOURRIENG_LienHe] CHECK (LEN(LTRIM(RTRIM([LienHe]))) > 0)
    );

    -- 15. Bảng xác minh dịch vụ: Lưu kết quả kiểm tra từng dịch vụ với đơn vị cung cấp trước khi báo giá.
    CREATE TABLE [dbo].[XACMINHDICHVU]
    (
        [MaXM] int IDENTITY(1,1) NOT NULL,
        [MaYC] int NOT NULL,
        [HangMuc] nvarchar(250) NOT NULL,
        [DonViCungCap] nvarchar(250) NOT NULL,
        [ThoiDiem] datetime2 NOT NULL CONSTRAINT [DF_XACMINHDICHVU_ThoiDiem] DEFAULT (SYSUTCDATETIME()),
        [KetQua] varchar(20) NOT NULL,
        [GhiChu] nvarchar(1000) NULL,
        CONSTRAINT [PK_XACMINHDICHVU] PRIMARY KEY ([MaXM]),
        CONSTRAINT [FK_XACMINHDICHVU_MaYC] FOREIGN KEY ([MaYC]) REFERENCES [dbo].[YEUCAUTOURRIENG] ([MaYC]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_XACMINHDICHVU_HangMucKhongRong] CHECK (LEN(LTRIM(RTRIM([HangMuc]))) > 0),
        CONSTRAINT [CK_XACMINHDICHVU_DonViCungCapKhongRong] CHECK (LEN(LTRIM(RTRIM([DonViCungCap]))) > 0)
    );

    -- 16. Bảng báo giá: Lưu giá và điều kiện gắn với yêu cầu cùng phiên bản lịch trình được báo giá.
    CREATE TABLE [dbo].[BAOGIA]
    (
        [MaBG] int IDENTITY(1,1) NOT NULL,
        [MaYC] int NOT NULL,
        [MaPB] int NOT NULL,
        [TongGia] decimal(18,0) NOT NULL,
        [KhoanCanThu] decimal(18,0) NOT NULL,
        [DieuKienHuy] nvarchar(2000) NOT NULL,
        [HanHieuLuc] datetime2 NOT NULL,
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_BAOGIA_TrangThai] DEFAULT ('Draft'),
        CONSTRAINT [PK_BAOGIA] PRIMARY KEY ([MaBG]),
        CONSTRAINT [FK_BAOGIA_MaYC] FOREIGN KEY ([MaYC]) REFERENCES [dbo].[YEUCAUTOURRIENG] ([MaYC]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_BAOGIA_MaPB] FOREIGN KEY ([MaPB]) REFERENCES [dbo].[PHIENBANLICHTRINH] ([MaPB]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_BAOGIA_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_BAOGIA_TongGia] CHECK ([TongGia] >= 0),
        CONSTRAINT [CK_BAOGIA_KhoanCanThu] CHECK ([KhoanCanThu] >= 0 AND [KhoanCanThu] <= [TongGia])
    );

    -- 17. Bảng hạng mục báo giá: Lưu từng dịch vụ, số lượng và đơn giá cấu thành báo giá.
    CREATE TABLE [dbo].[HANGMUCBAOGIA]
    (
        [MaHM] int IDENTITY(1,1) NOT NULL,
        [MaBG] int NOT NULL,
        [MoTa] nvarchar(500) NOT NULL,
        [DonViTinh] nvarchar(50) NOT NULL,
        [SoLuong] int NOT NULL,
        [DonGia] decimal(18,0) NOT NULL,
        CONSTRAINT [PK_HANGMUCBAOGIA] PRIMARY KEY ([MaHM]),
        CONSTRAINT [FK_HANGMUCBAOGIA_MaBG] FOREIGN KEY ([MaBG]) REFERENCES [dbo].[BAOGIA] ([MaBG]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_HANGMUCBAOGIA_DonViTinhKhongRong] CHECK (LEN(LTRIM(RTRIM([DonViTinh]))) > 0),
        CONSTRAINT [CK_HANGMUCBAOGIA_MoTa] CHECK (LEN(LTRIM(RTRIM([MoTa]))) > 0),
        CONSTRAINT [CK_HANGMUCBAOGIA_SoLuong] CHECK ([SoLuong] > 0),
        CONSTRAINT [CK_HANGMUCBAOGIA_DonGia] CHECK ([DonGia] >= 0)
    );

    -- 18. Bảng đơn đặt tour: Lưu đơn tour có sẵn hoặc tour riêng cùng giá và điều kiện đã chốt.
    CREATE TABLE [dbo].[DONDAT]
    (
        [MaDon] int IDENTITY(1,1) NOT NULL,
        [MaTK] int NOT NULL,
        [MaLKH] int NULL,
        [MaBG] int NULL,
        [SoKhach] int NOT NULL,
        [GiaChot] decimal(18,0) NOT NULL,
        [DieuKienHuy] nvarchar(2000) NOT NULL,
        [HanGiuCho] datetime2 NULL,
        [LyDoHuy] nvarchar(1000) NULL,
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_DONDAT_TrangThai] DEFAULT ('Pending'),
        [NgayDat] datetime2 NOT NULL CONSTRAINT [DF_DONDAT_NgayDat] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_DONDAT] PRIMARY KEY ([MaDon]),
        CONSTRAINT [FK_DONDAT_MaTK] FOREIGN KEY ([MaTK]) REFERENCES [dbo].[TAIKHOAN] ([MaTK]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_DONDAT_MaLKH] FOREIGN KEY ([MaLKH]) REFERENCES [dbo].[LICHKHOIHANH] ([MaLKH]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_DONDAT_MaBG] FOREIGN KEY ([MaBG]) REFERENCES [dbo].[BAOGIA] ([MaBG]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_DONDAT_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_DONDAT_NguonDon] CHECK (([MaLKH] IS NOT NULL AND [MaBG] IS NULL) OR ([MaLKH] IS NULL AND [MaBG] IS NOT NULL)),
        CONSTRAINT [CK_DONDAT_SoKhach] CHECK ([SoKhach] > 0),
        CONSTRAINT [CK_DONDAT_GiaChot] CHECK ([GiaChot] >= 0)
    );

    -- 19. Bảng giao dịch tiền: Lưu các khoản thu và hoàn tiền của đơn, kèm người ghi nhận và mã đối soát.
    CREATE TABLE [dbo].[GIAODICHTIEN]
    (
        [MaGD] int IDENTITY(1,1) NOT NULL,
        [MaDon] int NOT NULL,
        [LoaiGD] varchar(20) NOT NULL,
        [SoTien] decimal(18,0) NOT NULL,
        [MaThamChieu] varchar(50) NOT NULL,
        [ThoiDiem] datetime2 NOT NULL CONSTRAINT [DF_GIAODICHTIEN_ThoiDiem] DEFAULT (SYSUTCDATETIME()),
        [MaTKGhiNhan] int NOT NULL,
        CONSTRAINT [PK_GIAODICHTIEN] PRIMARY KEY ([MaGD]),
        CONSTRAINT [FK_GIAODICHTIEN_MaDon] FOREIGN KEY ([MaDon]) REFERENCES [dbo].[DONDAT] ([MaDon]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_GIAODICHTIEN_MaTKGhiNhan] FOREIGN KEY ([MaTKGhiNhan]) REFERENCES [dbo].[TAIKHOAN] ([MaTK]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [UQ_GIAODICHTIEN_MaThamChieu] UNIQUE ([MaThamChieu]),
        CONSTRAINT [CK_GIAODICHTIEN_SoTien] CHECK ([SoTien] > 0),
        CONSTRAINT [CK_GIAODICHTIEN_LoaiGDKhongRong] CHECK (LEN(LTRIM(RTRIM([LoaiGD]))) > 0),
        CONSTRAINT [CK_GIAODICHTIEN_MaThamChieuKhongRong] CHECK (LEN(LTRIM(RTRIM([MaThamChieu]))) > 0)
    );

    -- 20. Bảng ngôn ngữ: Lưu mã và tên ngôn ngữ được hỗ trợ cho bản dịch.
    CREATE TABLE [dbo].[NGONNGU]
    (
        [MaNN] varchar(10) NOT NULL,
        [TenNN] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_NGONNGU] PRIMARY KEY ([MaNN]),
        CONSTRAINT [CK_NGONNGU_TenNNKhongRong] CHECK (LEN(LTRIM(RTRIM([TenNN]))) > 0),
        CONSTRAINT [CK_NGONNGU_MaNNKhongRong] CHECK (LEN(LTRIM(RTRIM([MaNN]))) > 0)
    );

    -- 21. Bảng thuyết minh: Lưu nội dung gốc và phiên bản thuyết minh của địa điểm.
    CREATE TABLE [dbo].[THUYETMINH]
    (
        [MaTM] int IDENTITY(1,1) NOT NULL,
        [MaDD] int NOT NULL,
        [TieuDe] nvarchar(200) NOT NULL,
        [NoiDungGoc] nvarchar(max) NOT NULL,
        [PhienBan] int NOT NULL,
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_THUYETMINH_TrangThai] DEFAULT ('Draft'),
        CONSTRAINT [PK_THUYETMINH] PRIMARY KEY ([MaTM]),
        CONSTRAINT [FK_THUYETMINH_MaDD] FOREIGN KEY ([MaDD]) REFERENCES [dbo].[DIADIEM] ([MaDD]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_THUYETMINH_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_THUYETMINH_TieuDeKhongRong] CHECK (LEN(LTRIM(RTRIM([TieuDe]))) > 0),
        CONSTRAINT [CK_THUYETMINH_PhienBan] CHECK ([PhienBan] > 0),
        CONSTRAINT [CK_THUYETMINH_NoiDungGoc] CHECK (LEN(LTRIM(RTRIM([NoiDungGoc]))) > 0)
    );

    -- 22. Bảng bản dịch: Lưu nội dung theo ngôn ngữ cho một phiên bản thuyết minh gốc.
    CREATE TABLE [dbo].[BANDICH]
    (
        [MaBD] int IDENTITY(1,1) NOT NULL,
        [MaTM] int NOT NULL,
        [MaNN] varchar(10) NOT NULL,
        [NoiDungDich] nvarchar(max) NOT NULL,
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_BANDICH_TrangThai] DEFAULT ('Draft'),
        CONSTRAINT [PK_BANDICH] PRIMARY KEY ([MaBD]),
        CONSTRAINT [FK_BANDICH_MaTM] FOREIGN KEY ([MaTM]) REFERENCES [dbo].[THUYETMINH] ([MaTM]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_BANDICH_MaNN] FOREIGN KEY ([MaNN]) REFERENCES [dbo].[NGONNGU] ([MaNN]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [UQ_BANDICH_MaTM_MaNN] UNIQUE ([MaTM], [MaNN]),
        CONSTRAINT [CK_BANDICH_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_BANDICH_NoiDungDich] CHECK (LEN(LTRIM(RTRIM([NoiDungDich]))) > 0)
    );

    -- 23. Bảng tệp audio: Lưu tệp giọng đọc gắn với bản dịch và trạng thái công bố.
    CREATE TABLE [dbo].[TEPAUDIO]
    (
        [MaTep] int IDENTITY(1,1) NOT NULL,
        [MaBD] int NOT NULL,
        [GiongDoc] varchar(100) NOT NULL,
        [DuongDan] nvarchar(500) NOT NULL,
        [TrangThai] varchar(20) NOT NULL CONSTRAINT [DF_TEPAUDIO_TrangThai] DEFAULT ('Pending'),
        [NgayCongBo] datetime2 NULL,
        CONSTRAINT [PK_TEPAUDIO] PRIMARY KEY ([MaTep]),
        CONSTRAINT [FK_TEPAUDIO_MaBD] FOREIGN KEY ([MaBD]) REFERENCES [dbo].[BANDICH] ([MaBD]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_TEPAUDIO_TrangThaiKhongRong] CHECK (LEN(LTRIM(RTRIM([TrangThai]))) > 0),
        CONSTRAINT [CK_TEPAUDIO_TrangThai] CHECK ([TrangThai] IN ('Pending','Published','Failed','Inactive')),
        CONSTRAINT [CK_TEPAUDIO_CongBo] CHECK ([TrangThai] <> 'Published' OR ([NgayCongBo] IS NOT NULL AND LEN(LTRIM(RTRIM([DuongDan]))) > 0)),
        CONSTRAINT [CK_TEPAUDIO_GiongDoc] CHECK (LEN(LTRIM(RTRIM([GiongDoc]))) > 0)
    );

    -- 24. Bảng quyền nghe: Lưu quyền nghe theo tài khoản, đơn đã xác nhận và khoảng hiệu lực.
    CREATE TABLE [dbo].[QUYENNGHE]
    (
        [MaQN] int IDENTITY(1,1) NOT NULL,
        [MaDon] int NOT NULL,
        [MaTK] int NOT NULL,
        [HieuLucTu] datetime2 NOT NULL,
        [HieuLucDen] datetime2 NOT NULL,
        CONSTRAINT [PK_QUYENNGHE] PRIMARY KEY ([MaQN]),
        CONSTRAINT [FK_QUYENNGHE_MaDon] FOREIGN KEY ([MaDon]) REFERENCES [dbo].[DONDAT] ([MaDon]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_QUYENNGHE_MaTK] FOREIGN KEY ([MaTK]) REFERENCES [dbo].[TAIKHOAN] ([MaTK]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_QUYENNGHE_HieuLuc] CHECK ([HieuLucDen] > [HieuLucTu])
    );

    -- 25. Nhật ký thao tác: Lưu người thực hiện, thời điểm, đối tượng, lý do và dữ liệu thay đổi để đáp ứng UC24 và BR13.
    CREATE TABLE [dbo].[NHATKYTHAOTAC]
    (
        [MaNK] bigint IDENTITY(1,1) NOT NULL,
        [MaTK] int NULL,
        [ThoiDiem] datetime2 NOT NULL CONSTRAINT [DF_NHATKYTHAOTAC_ThoiDiem] DEFAULT (SYSUTCDATETIME()),
        [HanhDong] nvarchar(100) NOT NULL,
        [LoaiDoiTuong] nvarchar(100) NOT NULL,
        [MaDoiTuong] nvarchar(128) NULL,
        [LyDo] nvarchar(2000) NULL,
        [DuLieuTruoc] nvarchar(max) NULL,
        [DuLieuSau] nvarchar(max) NULL,
        [DiaChiIP] varchar(45) NULL,
        CONSTRAINT [PK_NHATKYTHAOTAC] PRIMARY KEY ([MaNK]),
        CONSTRAINT [FK_NHATKYTHAOTAC_MaTK] FOREIGN KEY ([MaTK]) REFERENCES [dbo].[TAIKHOAN] ([MaTK]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [CK_NHATKYTHAOTAC_HanhDongKhongRong] CHECK (LEN(LTRIM(RTRIM([HanhDong]))) > 0),
        CONSTRAINT [CK_NHATKYTHAOTAC_LoaiDoiTuongKhongRong] CHECK (LEN(LTRIM(RTRIM([LoaiDoiTuong]))) > 0),
        CONSTRAINT [CK_NHATKYTHAOTAC_DuLieuTruocJSON] CHECK ([DuLieuTruoc] IS NULL OR ISJSON([DuLieuTruoc]) = 1),
        CONSTRAINT [CK_NHATKYTHAOTAC_DuLieuSauJSON] CHECK ([DuLieuSau] IS NULL OR ISJSON([DuLieuSau]) = 1)
    );

    -- Mot bao gia chi tao toi da mot don; don tour san co MaBG = NULL.
    CREATE UNIQUE INDEX [UX_DONDAT_MaBG_KhongRong]
        ON [dbo].[DONDAT] ([MaBG]) WHERE [MaBG] IS NOT NULL;

    -- Chi muc cho khoa ngoai / cac luong truy van chinh.
    CREATE INDEX [IX_TAIKHOAN_VAITRO_MaVaiTro] ON [dbo].[TAIKHOAN_VAITRO] ([MaVaiTro]);
    CREATE INDEX [IX_HOATDONG_MaDD] ON [dbo].[HOATDONG] ([MaDD]);
    CREATE INDEX [IX_TOUR_DIADIEM_MaDD] ON [dbo].[TOUR_DIADIEM] ([MaDD]);
    CREATE INDEX [IX_LICHKHOIHANH_MaTour] ON [dbo].[LICHKHOIHANH] ([MaTour]);
    CREATE INDEX [IX_LICHTRINH_MaTK] ON [dbo].[LICHTRINH] ([MaTK]);
    CREATE INDEX [IX_LICHTRINH_DIADIEM_MaDD] ON [dbo].[LICHTRINH_DIADIEM] ([MaDD]);
    CREATE INDEX [IX_LICHTRINH_SUAT_MaSuat] ON [dbo].[LICHTRINH_SUAT] ([MaSuat]);
    CREATE INDEX [IX_LICHTRINH_SUAT_MaDD] ON [dbo].[LICHTRINH_SUAT] ([MaDD]);
    CREATE INDEX [IX_YEUCAUTOURRIENG_MaPB] ON [dbo].[YEUCAUTOURRIENG] ([MaPB]);
    CREATE INDEX [IX_XACMINHDICHVU_MaYC] ON [dbo].[XACMINHDICHVU] ([MaYC]);
    CREATE INDEX [IX_BAOGIA_MaYC] ON [dbo].[BAOGIA] ([MaYC]);
    CREATE INDEX [IX_BAOGIA_MaPB] ON [dbo].[BAOGIA] ([MaPB]);
    CREATE INDEX [IX_HANGMUCBAOGIA_MaBG] ON [dbo].[HANGMUCBAOGIA] ([MaBG]);
    CREATE INDEX [IX_DONDAT_MaTK] ON [dbo].[DONDAT] ([MaTK]);
    CREATE INDEX [IX_DONDAT_MaLKH] ON [dbo].[DONDAT] ([MaLKH]);
    CREATE INDEX [IX_GIAODICHTIEN_MaDon] ON [dbo].[GIAODICHTIEN] ([MaDon]);
    CREATE INDEX [IX_GIAODICHTIEN_MaTKGhiNhan] ON [dbo].[GIAODICHTIEN] ([MaTKGhiNhan]);
    CREATE INDEX [IX_THUYETMINH_MaDD] ON [dbo].[THUYETMINH] ([MaDD]);
    CREATE INDEX [IX_BANDICH_MaNN] ON [dbo].[BANDICH] ([MaNN]);
    CREATE INDEX [IX_TEPAUDIO_MaBD] ON [dbo].[TEPAUDIO] ([MaBD]);
    CREATE INDEX [IX_QUYENNGHE_MaDon] ON [dbo].[QUYENNGHE] ([MaDon]);
    CREATE INDEX [IX_QUYENNGHE_MaTK] ON [dbo].[QUYENNGHE] ([MaTK]);
    CREATE INDEX [IX_NHATKYTHAOTAC_MaTK] ON [dbo].[NHATKYTHAOTAC] ([MaTK]);
    CREATE INDEX [IX_DONDAT_TrangThai_HanGiuCho] ON [dbo].[DONDAT] ([TrangThai], [HanGiuCho]) INCLUDE ([MaLKH], [SoKhach]);
    CREATE INDEX [IX_BAOGIA_TrangThai_HanHieuLuc] ON [dbo].[BAOGIA] ([TrangThai], [HanHieuLuc]);
    CREATE INDEX [IX_QUYENNGHE_TaiKhoan_HieuLuc] ON [dbo].[QUYENNGHE] ([MaTK], [HieuLucTu], [HieuLucDen]) INCLUDE ([MaDon]);
    CREATE INDEX [IX_NHATKYTHAOTAC_ThoiDiem] ON [dbo].[NHATKYTHAOTAC] ([ThoiDiem]);
    CREATE INDEX [IX_NHATKYTHAOTAC_DoiTuong] ON [dbo].[NHATKYTHAOTAC] ([LoaiDoiTuong], [MaDoiTuong], [ThoiDiem]);


    -- PHẦN 2: VAI TRÒ VÀ NGÔN NGỮ.
    INSERT INTO [dbo].[VAITRO] ([TenVaiTro], [MoTa]) VALUES
        (N'Admin',     N'Quản trị tài khoản, vai trò và vận hành hệ thống.'),
        (N'Customer',  N'Khách hàng tạo lịch trình, đặt tour và nghe theo quyền.'),
        (N'Operator',  N'Nhân viên tư vấn, điều hành, quản lý tour và xử lý đơn.'),
        (N'Editor',    N'Biên tập địa điểm, nội dung thuyết minh và bản dịch.'),
        (N'Finance',   N'Nhân viên được cấp quyền ghi nhận thu và hoàn tiền.'),
        (N'Publisher', N'Người được cấp quyền duyệt và công bố nội dung/audio.');

    INSERT INTO [dbo].[NGONNGU] ([MaNN], [TenNN]) VALUES
        ('vi', N'Tiếng Việt'),
        ('en', N'Tiếng Anh'),
        ('ko', N'Tiếng Hàn');


    -- PHẦN 3: TÀI KHOẢN KIỂM THỬ. Hash chuẩn IdentityV3, không phải SHA đơn.
    DECLARE @TaiKhoanMau TABLE (
        Email varchar(254) COLLATE Latin1_General_100_CI_AS PRIMARY KEY,
        HoTen nvarchar(100) NOT NULL,
        SoDienThoai varchar(15) NULL,
        TenVaiTro nvarchar(50) NOT NULL,
        MatKhauBam varchar(255) NOT NULL
    );
    INSERT INTO @TaiKhoanMau (Email, HoTen, SoDienThoai, TenVaiTro, MatKhauBam) VALUES
        (N'admin@dongnamtrip.test', N'Kiểm thử Admin', N'0900000001', N'Admin', N'AQAAAAIAAYagAAAAEAFF6CqZAPXj0ogpmpDcT1zbfhB2weibr+0Dqb9D/AhE9Drm1RMyN5AKPlr6NsY4kQ=='),
        (N'operator@dongnamtrip.test', N'Kiểm thử Operator', N'0900000002', N'Operator', N'AQAAAAIAAYagAAAAEEb4vk4vmA8LFPw2A81oSOTKsVfAlLVKFpkfTUr8r/IyjJ3YQ66z13q9w4/7V3LewA=='),
        (N'editor@dongnamtrip.test', N'Kiểm thử Editor', N'0900000003', N'Editor', N'AQAAAAIAAYagAAAAEMu1y9oC5GLYCJZZzpY9p7m4vxyRXnQ761ZXJz+AHgmaHaXhMwnF2XY/J65ZNZ+s1w=='),
        (N'finance@dongnamtrip.test', N'Kiểm thử Finance', N'0900000004', N'Finance', N'AQAAAAIAAYagAAAAEMUJWeg1Nq6zZq1qpUza4lEfZx7mvPjhUFBHoZXS/UlLKahLGUlbB34SPrgY/wmXYA=='),
        (N'publisher@dongnamtrip.test', N'Kiểm thử Publisher', N'0900000005', N'Publisher', N'AQAAAAIAAYagAAAAEHZj9cLQd7n++N2+diGX2mfzlCryHQs4KPX+xXz9MD/XUMhlGtX7bpz0Y5pC58ck9w=='),
        (N'customer1@dongnamtrip.test', N'Kiểm thử khách hàng 1', N'0900000006', N'Customer', N'AQAAAAIAAYagAAAAEBjPk4dJkrQWjJ4A9wQeaH3ib26nl4HLWte0Ao5fggt9xrg7tKqpDYj9zOxcZfIgvQ=='),
        (N'customer2@dongnamtrip.test', N'Kiểm thử khách hàng 2', N'0900000007', N'Customer', N'AQAAAAIAAYagAAAAEHRv/dXQf+pAbN8QUXn1hxHPA3HW1E7ppEN8tiNYWfiEseCf+ABLY/W9rOqLN2YrsA==');

    INSERT INTO dbo.TAIKHOAN (HoTen, Email, MatKhauBam, SoDienThoai, TrangThai)
    SELECT HoTen, Email, MatKhauBam, SoDienThoai, 'Active'
    FROM @TaiKhoanMau;

    INSERT INTO dbo.TAIKHOAN_VAITRO (MaTK, MaVaiTro)
    SELECT t.MaTK, v.MaVaiTro FROM @TaiKhoanMau s
    JOIN dbo.TAIKHOAN t ON t.Email = s.Email
    JOIN dbo.VAITRO v ON v.TenVaiTro = s.TenVaiTro;

    IF (SELECT COUNT(*) FROM dbo.TAIKHOAN) <> 7
       OR (SELECT COUNT(*) FROM dbo.TAIKHOAN_VAITRO) <> 7
    BEGIN
        ;THROW 51205, N'Số tài khoản/vai trò không đúng. Hoàn tác khởi tạo.', 1;
    END;

    -- PHẦN 4: DỮ LIỆU DANH MỤC VÀ NGUỒN CÔNG KHAI.
DECLARE @NgayTraCuu date = '20261028';
DECLARE @Nguon TABLE (MaNguon varchar(10) PRIMARY KEY, TenNguon nvarchar(200), URL nvarchar(1000));
INSERT INTO @Nguon (MaNguon, TenNguon, URL) VALUES
    (N'H1', N'Visit HCMC – Dinh Độc Lập', N'https://visithcmc.vn/vi/dinh-doc-lap/'),
    (N'H2', N'Visit HCMC – Bảo tàng Chứng tích Chiến tranh', N'https://visithcmc.vn/vi/bao-tang-chung-tich-chien-tranh/'),
    (N'H3', N'Visit HCMC – Kiến trúc Đông Dương', N'https://visithcmc.vn/vi/di-san-phong-cach-kien-truc-dong-duong-giua-long-thanh-pho/'),
    (N'H4', N'Visit HCMC – Chợ Bến Thành', N'https://visithcmc.vn/vi/kham-pha-cho-ben-thanh-tphcm-tu-a-z-5-phut-doc/'),
    (N'H5', N'Visit HCMC – Địa đạo Củ Chi', N'https://visithcmc.vn/vi/tham-quan-dia-dao-cu-chi-thanh-pho-duoi-long-dat/'),
    (N'T1', N'Ban quản lý KDL quốc gia Núi Bà Đen', N'https://khudulichnuibaden.tayninh.gov.vn/gioi-thieu-nui-ba-den'),
    (N'T2', N'Cục Du lịch Quốc gia – Điểm đến Tây Ninh', N'https://dantoc.vietnamtourism.gov.vn/tay-ninh-phat-huy-gia-tri-di-san-van-hoa-cua-dong-bao-dan-toc-thieu-so-gan-voi-phat-trien-du-lich/'),
    (N'T3', N'Cổng du lịch Tây Ninh – Hồ Dầu Tiếng', N'https://dulich.tayninh.gov.vn/tin-tuc/hanh-trinh-san-hoang-hon-ho-dau-tieng-ban-giao-huong-anh-sang-giua-long-bien-ho-1594'),
    (N'T4', N'Cổng du lịch Tây Ninh – Lò Gò Xa Mát', N'https://dulich.tayninh.gov.vn/tin-tuc/vuon-quoc-gia-lo-go-xa-mat-diem-den-sinh-thai-hap-dan-cua-du-lich-tay-ninh-1619'),
    (N'T5', N'Vietnam Airlines – Chùa Gò Kén', N'https://www.vietnamairlines.com/de/vi/useful-information/travel-guide/chua-go-ken'),
    (N'V1', N'Vietnam Tourism – Vũng Tàu', N'https://vietnam.travel/things-to-do/vung-tau-sightseeing-wonderland/'),
    (N'V2', N'Viet Fun Travel – Chương trình Vũng Tàu VF184', N'https://www.vietfuntravel.com.vn/tour-vung-tau/tour-du-lich-vung-tau-1-ngay.html'),
    (N'V3', N'Viet Fun Travel – Bình Châu VF181', N'https://www.vietfuntravel.com.vn/tour-vung-tau/tour-du-lich-binh-chau-ho-coc-1-ngay.html'),
    (N'V4', N'Viet Fun Travel – Hồ Cóc VF182', N'https://www.vietfuntravel.com.vn/tour-vung-tau/tour-du-lich-ho-coc-binh-chau-2-ngay-1-dem.html'),
    (N'V5', N'Thông tấn xã Việt Nam – Tháp Tam Thắng', N'https://vietnam.vnanet.vn/vietnamese/tin-tuc/thap-tam-thang-bieu-tuong-moi-niem-tu-hao-cua-tp-ho-chi-minh-414516.html'),
    (N'D1', N'Khu du lịch Bửu Long – Liên hệ', N'https://buulong.com.vn/lien-he'),
    (N'D2', N'Sở VHTTDL Đồng Nai – Các di tích trực tiếp quản lý', N'https://svhttdl.dongnai.gov.vn/vi/news/thong-tin-chi-dao-dieu-hanh/ban-hanh-quy-che-phan-cap-quan-ly-bao-ve-va-phat-huy-gia-tri-di-tich-lich-su-van-hoa-danh-lam-thang-canh-tren-dia-ban-tinh-dong-nai-218.html'),
    (N'D3', N'Vườn quốc gia Cát Tiên – Chương trình 2N1D', N'https://cattiennationalpark.com.vn/wp-content/uploads/2023/01/2N1D.BS_.pdf'),
    (N'D4', N'Cổng thông tin Đồng Nai – Các điểm du lịch', N'https://dongnai.gov.vn/vi/wcag/Chuong-trinh-Ke-hoach-phat-trien-KTXH/du-lich-dong-nai-nhung-diem-den-tranh-nong-hap-dan-2324.html'),
    (N'D5', N'Trung tâm dịch vụ việc làm Đồng Nai – Đơn vị Giang Điền', N'https://vieclamdongnai.gov.vn/nha-tuyen-dung/c-212-ng-ty-co-phan-du-lich-giang-dien-6409.html'),
    (N'B1', N'Cổng du lịch Bình Dương – Địa chỉ di tích sau sáp nhập', N'https://dulich.binhduong.gov.vn/en/detailevents/?id=news_2017&t=cap-nhat-dia-chi-moi-cua-cac-di-tich-lich-su-van-hoa-cap-quoc-gia-cua-tinh-binh-duong-cu-sau-sap-nhap-voi-tp-ho-chi-minh'),
    (N'B2', N'Xúc tiến du lịch Bình Dương – Chùa Bà Thiên Hậu', N'https://dulichbinhduong.org.vn/du-lich/le-hoi-chua-ba-thien-hau/ct'),
    (N'B3', N'Xúc tiến du lịch Bình Dương – Nhà cổ', N'https://dulichbinhduong.org.vn/du-lich/tham-quan-nhung-ngoi-nha-co-o-binh-duong/ct'),
    (N'B4', N'Cổng du lịch Bình Dương – Sơn mài Tương Bình Hiệp', N'https://dulich.binhduong.gov.vn/vi/langsonmai'),
    (N'B5', N'Xúc tiến du lịch Bình Dương – Chương trình tham quan Đại Nam', N'https://dulichbinhduong.org.vn/du-lich/chuong-trinh-tour-du-lich-binh-duong/ct'),
    (N'P1', N'Viet Fun Travel – VF03', N'https://www.vietfuntravel.com.vn/tour-saigon/kham-pha-dia-dao-cu-chi-nua-ngay.html'),
    (N'P2', N'Viet Fun Travel – VF04', N'https://www.vietfuntravel.com.vn/tour-saigon/tham-quan-toa-thanh-cao-dai-tay-ninh-dia-dao-cu-chi-1-ngay.html'),
    (N'P3', N'Viet Fun Travel – VF393', N'https://www.vietfuntravel.com.vn/tour-saigon/tour-du-lich-nui-ba-den-toa-thanh-tay-ninh-cu-chi-1-ngay-viet-fun-travel.html'),
    (N'P4', N'Viet Fun Travel – VF180', N'https://www.vietfuntravel.com.vn/tour-vung-tau/tour-du-lich-binh-chau-ho-coc-2-ngay-1-dem.html'),
    (N'P5', N'Nam Á Châu – Sản phẩm 5157', N'https://dulichnamachau.vn/tour-du-lich/tour-cu-chi-tay-ninh-1-ngay-dia-dao-nui-ba-den-toa-thanh/');

DECLARE @Diem TABLE (
    MaBienSoan varchar(10) PRIMARY KEY, TenDD nvarchar(120), KhuVuc nvarchar(50),
    LoaiHinh nvarchar(100), DiaChi nvarchar(250), MoTa nvarchar(3000),
    ThoiLuongPhut int, MaNguon varchar(10)
);
INSERT INTO @Diem VALUES
    (N'HCM01', N'Dinh Độc Lập', N'TP. Hồ Chí Minh', N'Văn hóa', N'135 Nam Kỳ Khởi Nghĩa, TP. Hồ Chí Minh', N'Di tích lịch sử và kiến trúc tại trung tâm thành phố, gắn với sự kiện thống nhất đất nước.', 90, N'H1'),
    (N'HCM02', N'Bảo tàng Chứng tích Chiến tranh', N'TP. Hồ Chí Minh', N'Văn hóa', N'28 Võ Văn Tần, TP. Hồ Chí Minh', N'Bảo tàng trưng bày tư liệu và hiện vật về chiến tranh, hướng đến thông điệp hòa bình.', 90, N'H2'),
    (N'HCM03', N'Bưu điện Trung tâm Sài Gòn', N'TP. Hồ Chí Minh', N'Văn hóa', N'02 Công trường Công xã Paris, TP. Hồ Chí Minh', N'Công trình bưu chính có giá trị kiến trúc tại khu vực trung tâm thành phố.', 45, N'H3'),
    (N'HCM04', N'Chợ Bến Thành', N'TP. Hồ Chí Minh', N'Kết hợp', N'Đường Lê Lợi, khu vực Bến Thành, TP. Hồ Chí Minh', N'Khu chợ truyền thống với các gian hàng, sản phẩm thủ công và ẩm thực địa phương.', 60, N'H4'),
    (N'HCM05', N'Địa đạo Củ Chi', N'TP. Hồ Chí Minh', N'Văn hóa', N'Tỉnh lộ 15, khu vực Củ Chi, TP. Hồ Chí Minh', N'Di tích đường hầm cùng các không gian giới thiệu đời sống và lịch sử kháng chiến.', 120, N'H5'),
    (N'TN01', N'Núi Bà Đen', N'Tây Ninh', N'Kết hợp', N'Khu phố Ninh Phú, phường Bình Minh, Tây Ninh', N'Quần thể núi, cảnh quan và công trình tâm linh tiêu biểu của Tây Ninh.', 180, N'T1'),
    (N'TN02', N'Tòa Thánh Cao Đài Tây Ninh', N'Tây Ninh', N'Văn hóa', N'Khu vực Hòa Thành, Tây Ninh', N'Trung tâm tôn giáo Cao Đài với kiến trúc đặc trưng, thuộc nhóm điểm đến văn hóa của Tây Ninh.', 60, N'T2'),
    (N'TN03', N'Hồ Dầu Tiếng', N'Tây Ninh', N'Thiên nhiên', N'Khu vực ven hồ Dầu Tiếng phía Tây Ninh', N'Không gian mặt nước và bờ hồ phù hợp ngắm cảnh, đón bình minh hoặc hoàng hôn.', 90, N'T3'),
    (N'TN04', N'Vườn quốc gia Lò Gò - Xa Mát', N'Tây Ninh', N'Thiên nhiên', N'Khu vực Tân Biên, Tây Ninh', N'Khu bảo tồn với rừng và sinh cảnh đất ngập nước, có các hoạt động khám phá thiên nhiên.', 180, N'T4'),
    (N'TN05', N'Chùa Gò Kén', N'Tây Ninh', N'Văn hóa', N'Khu vực Long Thành Trung, Hòa Thành, Tây Ninh', N'Chùa còn được gọi là Thiền Lâm Tự, là điểm tham quan kiến trúc và tín ngưỡng.', 60, N'T5'),
    (N'VT01', N'Bãi Sau Vũng Tàu', N'Vũng Tàu', N'Biển', N'Đường Thùy Vân, khu vực Vũng Tàu', N'Bãi biển của Vũng Tàu với không gian dạo bộ ven bờ.', 90, N'V1'),
    (N'VT02', N'Bạch Dinh', N'Vũng Tàu', N'Văn hóa', N'Khu vực Núi Lớn, Vũng Tàu', N'Biệt thự lịch sử mang kiến trúc châu Âu, có khuôn viên nhìn ra biển.', 60, N'V1'),
    (N'VT03', N'Hải đăng Vũng Tàu', N'Vũng Tàu', N'Kết hợp', N'Khu vực Núi Nhỏ, Vũng Tàu', N'Công trình hải đăng trên Núi Nhỏ, là điểm nhìn bao quát cảnh quan.', 60, N'V1'),
    (N'VT04', N'Tượng Chúa Kitô Vua Vũng Tàu', N'Vũng Tàu', N'Văn hóa', N'Khu vực Núi Nhỏ, Vũng Tàu', N'Công trình tôn giáo trên Núi Nhỏ, kết hợp cảnh quan biển.', 90, N'V1'),
    (N'VT05', N'Mũi Nghinh Phong', N'Vũng Tàu', N'Thiên nhiên', N'Khu vực đường Hạ Long, Vũng Tàu', N'Điểm ngắm biển nằm trong chương trình tham quan Vũng Tàu được đơn vị lữ hành công bố.', 45, N'V2'),
    (N'VT06', N'Tháp Tam Thắng', N'Vũng Tàu', N'Văn hóa', N'Không gian công viên Bãi Sau, đường Thùy Vân, Vũng Tàu', N'Công trình biểu tượng và không gian công cộng bên Bãi Sau.', 45, N'V5'),
    (N'VT07', N'Chùa Đại Tòng Lâm', N'Vũng Tàu', N'Văn hóa', N'Khu vực Phú Mỹ', N'Điểm tham quan tôn giáo được đưa vào chương trình du lịch tuyến Vũng Tàu.', 60, N'V2'),
    (N'VT08', N'Chợ Bà Rịa', N'Vũng Tàu', N'Kết hợp', N'Khu vực trung tâm Bà Rịa', N'Khu chợ được lựa chọn làm điểm dừng trong chương trình tham quan tuyến Vũng Tàu.', 45, N'V2'),
    (N'VT09', N'Suối khoáng nóng Bình Châu', N'Vũng Tàu', N'Thiên nhiên', N'Khu vực Bình Châu', N'Điểm du lịch khoáng nóng có dịch vụ ngâm khoáng được đơn vị lữ hành giới thiệu.', 120, N'V3'),
    (N'VT10', N'Biển Hồ Cốc', N'Vũng Tàu', N'Biển', N'Khu vực Hồ Cốc', N'Điểm đến ven biển trong chương trình nghỉ dưỡng Bình Châu - Hồ Cốc.', 120, N'V4'),
    (N'DN01', N'Khu du lịch Bửu Long', N'Đồng Nai', N'Thiên nhiên', N'Khu phố Tân Bửu, đường Huỳnh Văn Nghệ, phường Trấn Biên, Đồng Nai', N'Khu du lịch có hồ, núi đá và không gian cảnh quan phục vụ tham quan, dã ngoại.', 180, N'D1'),
    (N'DN02', N'Văn miếu Trấn Biên', N'Đồng Nai', N'Văn hóa', N'Khu vực Biên Hòa, Đồng Nai', N'Di tích văn hóa của Đồng Nai gắn với truyền thống học tập và tôn vinh hiền tài.', 60, N'D2'),
    (N'DN03', N'Vườn quốc gia Cát Tiên', N'Đồng Nai', N'Thiên nhiên', N'Khu Nam Cát Tiên, Đồng Nai', N'Vườn quốc gia có các tuyến thực vật và tham quan vùng đất ngập nước Bàu Sấu.', 360, N'D3'),
    (N'DN04', N'Khu du lịch thác Giang Điền', N'Đồng Nai', N'Thiên nhiên', N'104/4 khu vực Hòa Bình, Giang Điền, Đồng Nai', N'Khu du lịch sinh thái gắn với cảnh quan thác nước và không gian xanh.', 180, N'D5'),
    (N'DN05', N'Khu du lịch Vườn Xoài', N'Đồng Nai', N'Thiên nhiên', N'Khu vực Biên Hòa, Đồng Nai', N'Khu du lịch được cổng thông tin Đồng Nai giới thiệu trong nhóm điểm đến vui chơi, dã ngoại.', 180, N'D4'),
    (N'BD01', N'Chùa Hội Khánh', N'Bình Dương', N'Văn hóa', N'Phường Thủ Dầu Một, TP. Hồ Chí Minh', N'Di tích kiến trúc nghệ thuật thuộc khu vực Bình Dương, nay thuộc TP. Hồ Chí Minh.', 60, N'B1'),
    (N'BD02', N'Chùa Bà Thiên Hậu Thủ Dầu Một', N'Bình Dương', N'Văn hóa', N'04 Nguyễn Du, khu vực Thủ Dầu Một', N'Cơ sở tín ngưỡng thờ Thiên Hậu, gắn với lễ hội của cộng đồng người Hoa.', 60, N'B2'),
    (N'BD03', N'Nhà cổ Trần Văn Hổ', N'Bình Dương', N'Văn hóa', N'18 Bạch Đằng, phường Thủ Dầu Một, TP. Hồ Chí Minh', N'Nhà cổ có kết cấu và trang trí gỗ truyền thống, là một di tích của khu vực Thủ Dầu Một.', 60, N'B3'),
    (N'BD04', N'Làng sơn mài Tương Bình Hiệp', N'Bình Dương', N'Văn hóa', N'Khu vực Tương Bình Hiệp, Thủ Dầu Một', N'Làng nghề nổi tiếng với sản phẩm sơn mài và các kỹ thuật trang trí thủ công.', 120, N'B4'),
    (N'BD05', N'Khu du lịch Đại Nam', N'Bình Dương', N'Kết hợp', N'Khu vực Thủ Dầu Một', N'Khu du lịch được giới thiệu với các không gian tham quan, khu vui chơi và biển nhân tạo.', 240, N'B5');

DECLARE @HoatDong TABLE (
    MaDiem varchar(10), TenHD nvarchar(150), MoTa nvarchar(2000),
    ThoiLuongPhut int, DieuKien nvarchar(500), MaNguon varchar(10),
    PRIMARY KEY (MaDiem, TenHD)
);
INSERT INTO @HoatDong VALUES
    (N'HCM01', N'Tham quan không gian di tích', N'Gợi ý trải nghiệm tham khảo: tham quan không gian di tích. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'H1'),
    (N'HCM01', N'Quan sát kiến trúc và khuôn viên', N'Gợi ý trải nghiệm tham khảo: quan sát kiến trúc và khuôn viên. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'H1'),
    (N'HCM02', N'Xem các phòng trưng bày', N'Gợi ý trải nghiệm tham khảo: xem các phòng trưng bày. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'H2'),
    (N'HCM02', N'Quan sát hiện vật ngoài trời', N'Gợi ý trải nghiệm tham khảo: quan sát hiện vật ngoài trời. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'H2'),
    (N'HCM03', N'Quan sát không gian kiến trúc', N'Gợi ý trải nghiệm tham khảo: quan sát không gian kiến trúc. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'H3'),
    (N'HCM03', N'Chụp ảnh mặt tiền công trình', N'Gợi ý trải nghiệm tham khảo: chụp ảnh mặt tiền công trình. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 15, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'H3'),
    (N'HCM04', N'Khám phá các gian hàng', N'Gợi ý trải nghiệm tham khảo: khám phá các gian hàng. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'H4'),
    (N'HCM04', N'Tìm hiểu món ăn tại chợ', N'Gợi ý trải nghiệm tham khảo: tìm hiểu món ăn tại chợ. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'H4'),
    (N'HCM05', N'Tìm hiểu hệ thống địa đạo', N'Gợi ý trải nghiệm tham khảo: tìm hiểu hệ thống địa đạo. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'H5'),
    (N'HCM05', N'Tham quan khu tái hiện lịch sử', N'Gợi ý trải nghiệm tham khảo: tham quan khu tái hiện lịch sử. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'H5'),
    (N'TN01', N'Ngắm cảnh khu vực đỉnh núi', N'Gợi ý trải nghiệm tham khảo: ngắm cảnh khu vực đỉnh núi. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 90, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'T1'),
    (N'TN01', N'Tham quan quần thể chùa', N'Gợi ý trải nghiệm tham khảo: tham quan quần thể chùa. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 90, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'T1'),
    (N'TN02', N'Quan sát kiến trúc Tòa Thánh', N'Gợi ý trải nghiệm tham khảo: quan sát kiến trúc tòa thánh. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'T2'),
    (N'TN02', N'Tìm hiểu không gian văn hóa Cao Đài', N'Gợi ý trải nghiệm tham khảo: tìm hiểu không gian văn hóa cao đài. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'T2'),
    (N'TN03', N'Ngắm cảnh ven hồ', N'Gợi ý trải nghiệm tham khảo: ngắm cảnh ven hồ. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 45, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'T3'),
    (N'TN03', N'Chụp ảnh hoàng hôn', N'Gợi ý trải nghiệm tham khảo: chụp ảnh hoàng hôn. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 45, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'T3'),
    (N'TN04', N'Tìm hiểu hệ sinh thái rừng', N'Gợi ý trải nghiệm tham khảo: tìm hiểu hệ sinh thái rừng. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 120, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên. Tuyến trong rừng cần xác nhận với ban quản lý trước khi tham gia.', N'T4'),
    (N'TN04', N'Quan sát chim theo tuyến cho phép', N'Gợi ý trải nghiệm tham khảo: quan sát chim theo tuyến cho phép. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên. Tuyến trong rừng cần xác nhận với ban quản lý trước khi tham gia.', N'T4'),
    (N'TN05', N'Vãn cảnh khuôn viên chùa', N'Gợi ý trải nghiệm tham khảo: vãn cảnh khuôn viên chùa. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'T5'),
    (N'TN05', N'Quan sát kiến trúc và tượng Phật', N'Gợi ý trải nghiệm tham khảo: quan sát kiến trúc và tượng phật. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'T5'),
    (N'VT01', N'Dạo bộ ven biển', N'Gợi ý trải nghiệm tham khảo: dạo bộ ven biển. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 45, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V1'),
    (N'VT01', N'Ngắm bình minh trên bãi biển', N'Gợi ý trải nghiệm tham khảo: ngắm bình minh trên bãi biển. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 45, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V1'),
    (N'VT02', N'Quan sát kiến trúc biệt thự', N'Gợi ý trải nghiệm tham khảo: quan sát kiến trúc biệt thự. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V1'),
    (N'VT02', N'Dạo bộ khuôn viên và ngắm biển', N'Gợi ý trải nghiệm tham khảo: dạo bộ khuôn viên và ngắm biển. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V1'),
    (N'VT03', N'Tìm hiểu công trình hải đăng', N'Gợi ý trải nghiệm tham khảo: tìm hiểu công trình hải đăng. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V1'),
    (N'VT03', N'Ngắm cảnh từ khu vực được phép', N'Gợi ý trải nghiệm tham khảo: ngắm cảnh từ khu vực được phép. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V1'),
    (N'VT04', N'Tham quan công trình tôn giáo', N'Gợi ý trải nghiệm tham khảo: tham quan công trình tôn giáo. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V1'),
    (N'VT04', N'Ngắm cảnh khu vực Núi Nhỏ', N'Gợi ý trải nghiệm tham khảo: ngắm cảnh khu vực núi nhỏ. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V1'),
    (N'VT05', N'Ngắm cảnh biển', N'Gợi ý trải nghiệm tham khảo: ngắm cảnh biển. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V2'),
    (N'VT05', N'Chụp ảnh tại khu vực cho phép', N'Gợi ý trải nghiệm tham khảo: chụp ảnh tại khu vực cho phép. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 15, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V2'),
    (N'VT06', N'Quan sát công trình biểu tượng', N'Gợi ý trải nghiệm tham khảo: quan sát công trình biểu tượng. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 20, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V5'),
    (N'VT06', N'Dạo bộ không gian công viên', N'Gợi ý trải nghiệm tham khảo: dạo bộ không gian công viên. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 25, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V5'),
    (N'VT07', N'Vãn cảnh khuôn viên chùa', N'Gợi ý trải nghiệm tham khảo: vãn cảnh khuôn viên chùa. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V2'),
    (N'VT07', N'Tìm hiểu kiến trúc Phật giáo', N'Gợi ý trải nghiệm tham khảo: tìm hiểu kiến trúc phật giáo. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V2'),
    (N'VT08', N'Tham quan các gian hàng', N'Gợi ý trải nghiệm tham khảo: tham quan các gian hàng. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 25, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V2'),
    (N'VT08', N'Tìm hiểu sản phẩm địa phương', N'Gợi ý trải nghiệm tham khảo: tìm hiểu sản phẩm địa phương. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 20, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V2'),
    (N'VT09', N'Tìm hiểu không gian khoáng nóng', N'Gợi ý trải nghiệm tham khảo: tìm hiểu không gian khoáng nóng. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V3'),
    (N'VT09', N'Trải nghiệm ngâm khoáng theo dịch vụ', N'Gợi ý trải nghiệm tham khảo: trải nghiệm ngâm khoáng theo dịch vụ. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 90, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V3'),
    (N'VT10', N'Dạo bộ ven biển', N'Gợi ý trải nghiệm tham khảo: dạo bộ ven biển. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V4'),
    (N'VT10', N'Ngắm cảnh bãi biển', N'Gợi ý trải nghiệm tham khảo: ngắm cảnh bãi biển. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'V4'),
    (N'DN01', N'Khám phá cảnh quan hồ và núi đá', N'Gợi ý trải nghiệm tham khảo: khám phá cảnh quan hồ và núi đá. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 90, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'D1'),
    (N'DN01', N'Dạo bộ và chụp ảnh khuôn viên', N'Gợi ý trải nghiệm tham khảo: dạo bộ và chụp ảnh khuôn viên. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 90, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'D1'),
    (N'DN02', N'Tham quan không gian văn miếu', N'Gợi ý trải nghiệm tham khảo: tham quan không gian văn miếu. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'D2'),
    (N'DN02', N'Tìm hiểu truyền thống hiếu học', N'Gợi ý trải nghiệm tham khảo: tìm hiểu truyền thống hiếu học. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'D2'),
    (N'DN03', N'Khám phá tuyến thực vật', N'Gợi ý trải nghiệm tham khảo: khám phá tuyến thực vật. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 120, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên. Tuyến trong rừng cần xác nhận với ban quản lý trước khi tham gia.', N'D3'),
    (N'DN03', N'Tham quan tuyến Bàu Sấu theo hướng dẫn', N'Gợi ý trải nghiệm tham khảo: tham quan tuyến bàu sấu theo hướng dẫn. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 240, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên. Tuyến trong rừng cần xác nhận với ban quản lý trước khi tham gia.', N'D3'),
    (N'DN04', N'Ngắm cảnh thác nước', N'Gợi ý trải nghiệm tham khảo: ngắm cảnh thác nước. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'D5'),
    (N'DN04', N'Dạo bộ không gian sinh thái', N'Gợi ý trải nghiệm tham khảo: dạo bộ không gian sinh thái. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 120, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'D5'),
    (N'DN05', N'Tham quan cảnh quan khu du lịch', N'Gợi ý trải nghiệm tham khảo: tham quan cảnh quan khu du lịch. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 90, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'D4'),
    (N'DN05', N'Dạo bộ và chụp ảnh ngoài trời', N'Gợi ý trải nghiệm tham khảo: dạo bộ và chụp ảnh ngoài trời. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 90, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'D4'),
    (N'BD01', N'Tham quan kiến trúc chùa', N'Gợi ý trải nghiệm tham khảo: tham quan kiến trúc chùa. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'B1'),
    (N'BD01', N'Tìm hiểu không gian di tích', N'Gợi ý trải nghiệm tham khảo: tìm hiểu không gian di tích. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'B1'),
    (N'BD02', N'Tìm hiểu không gian tín ngưỡng', N'Gợi ý trải nghiệm tham khảo: tìm hiểu không gian tín ngưỡng. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'B2'),
    (N'BD02', N'Quan sát kiến trúc và trang trí', N'Gợi ý trải nghiệm tham khảo: quan sát kiến trúc và trang trí. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'B2'),
    (N'BD03', N'Quan sát kiến trúc nhà cổ', N'Gợi ý trải nghiệm tham khảo: quan sát kiến trúc nhà cổ. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'B3'),
    (N'BD03', N'Tìm hiểu trang trí nội thất gỗ', N'Gợi ý trải nghiệm tham khảo: tìm hiểu trang trí nội thất gỗ. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 30, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'B3'),
    (N'BD04', N'Tìm hiểu sản phẩm sơn mài', N'Gợi ý trải nghiệm tham khảo: tìm hiểu sản phẩm sơn mài. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên. Việc vào xưởng phải được cơ sở đồng ý trước.', N'B4'),
    (N'BD04', N'Tìm hiểu quy trình làm sơn mài tại cơ sở', N'Gợi ý trải nghiệm tham khảo: tìm hiểu quy trình làm sơn mài tại cơ sở. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 60, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên. Việc vào xưởng phải được cơ sở đồng ý trước.', N'B4'),
    (N'BD05', N'Tham quan không gian văn hóa', N'Gợi ý trải nghiệm tham khảo: tham quan không gian văn hóa. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 120, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'B5'),
    (N'BD05', N'Khám phá khu vui chơi theo dịch vụ', N'Gợi ý trải nghiệm tham khảo: khám phá khu vui chơi theo dịch vụ. Thời lượng là ước lượng để lập lịch; chưa có suất hoặc dịch vụ được xác nhận.', 120, N'Tuân thủ nội quy và khu vực được phép; liên hệ đơn vị quản lý để xác nhận khả năng tiếp nhận và phí. Không bao gồm vé, đặt chỗ hoặc hướng dẫn viên.', N'B5');

DECLARE @Tour TABLE (
    MaBienSoan varchar(10) PRIMARY KEY, TenTour nvarchar(150), KhuVuc nvarchar(50),
    LoaiHinh nvarchar(100), ThoiLuong nvarchar(50), GiaThamKhao decimal(18,0),
    TomTat nvarchar(1000), DieuKienHuy nvarchar(2000), MaNguon varchar(10)
);
INSERT INTO @Tour VALUES
    (N'VF03', N'Tham khảo Viet Fun VF03 - Khám phá địa đạo Củ Chi', N'TP. Hồ Chí Minh', N'Văn hóa', N'Nửa ngày', 500000, N'Nguồn: Viet Fun Travel; mã VF03. Khám phá di tích địa đạo Củ Chi; phương án khu tham quan do nhà cung cấp xác nhận. Giá từ trên website nguồn, tra cứu 28/10/2026; không phải giá bán đã xác nhận của DongNamTrip. https://www.vietfuntravel.com.vn/tour-saigon/kham-pha-dia-dao-cu-chi-nua-ngay.html', N'Chưa xác nhận chính sách áp dụng. Liên hệ nhà cung cấp và chốt điều kiện trước khi mở bán; không dùng bản tham khảo để xác nhận đơn.', N'P1'),
    (N'VF04', N'Tham khảo Viet Fun VF04 - Cao Đài và Củ Chi', N'Tây Ninh', N'Văn hóa', N'1 ngày', 730000, N'Nguồn: Viet Fun Travel; mã VF04. Kết hợp Tòa Thánh Cao Đài và di tích địa đạo Củ Chi. Giá từ trên website nguồn, tra cứu 28/10/2026; không phải giá bán đã xác nhận của DongNamTrip. https://www.vietfuntravel.com.vn/tour-saigon/tham-quan-toa-thanh-cao-dai-tay-ninh-dia-dao-cu-chi-1-ngay.html', N'Chưa xác nhận chính sách áp dụng. Liên hệ nhà cung cấp và chốt điều kiện trước khi mở bán; không dùng bản tham khảo để xác nhận đơn.', N'P2'),
    (N'VF393', N'Tham khảo Viet Fun VF393 - Núi Bà Đen, Cao Đài, Củ Chi', N'Tây Ninh', N'Kết hợp', N'1 ngày', 1230000, N'Nguồn: Viet Fun Travel; mã VF393. Kết hợp tham quan núi Bà Đen, Tòa Thánh Cao Đài và địa đạo Củ Chi. Giá từ trên website nguồn, tra cứu 28/10/2026; không phải giá bán đã xác nhận của DongNamTrip. https://www.vietfuntravel.com.vn/tour-saigon/tour-du-lich-nui-ba-den-toa-thanh-tay-ninh-cu-chi-1-ngay-viet-fun-travel.html', N'Chưa xác nhận chính sách áp dụng. Liên hệ nhà cung cấp và chốt điều kiện trước khi mở bán; không dùng bản tham khảo để xác nhận đơn.', N'P3'),
    (N'VF184', N'Tham khảo Viet Fun VF184 - Vũng Tàu một ngày', N'Vũng Tàu', N'Kết hợp', N'1 ngày', 1050000, N'Nguồn: Viet Fun Travel; mã VF184. Tham khảo tuyến Vũng Tàu với các điểm biển, công trình và chợ địa phương. Giá từ trên website nguồn, tra cứu 28/10/2026; không phải giá bán đã xác nhận của DongNamTrip. https://www.vietfuntravel.com.vn/tour-vung-tau/tour-du-lich-vung-tau-1-ngay.html', N'Chưa xác nhận chính sách áp dụng. Liên hệ nhà cung cấp và chốt điều kiện trước khi mở bán; không dùng bản tham khảo để xác nhận đơn.', N'V2'),
    (N'VF181', N'Tham khảo Viet Fun VF181 - Khoáng nóng Bình Châu', N'Vũng Tàu', N'Thiên nhiên', N'1 ngày', 1120000, N'Nguồn: Viet Fun Travel; mã VF181. Chương trình tham khảo nghỉ ngơi và trải nghiệm khoáng nóng tại Bình Châu. Giá từ trên website nguồn, tra cứu 28/10/2026; không phải giá bán đã xác nhận của DongNamTrip. https://www.vietfuntravel.com.vn/tour-vung-tau/tour-du-lich-binh-chau-ho-coc-1-ngay.html', N'Chưa xác nhận chính sách áp dụng. Liên hệ nhà cung cấp và chốt điều kiện trước khi mở bán; không dùng bản tham khảo để xác nhận đơn.', N'V3'),
    (N'VF180', N'Tham khảo Viet Fun VF180 - Bình Châu, Hồ Cốc; ngủ Bình Châu', N'Vũng Tàu', N'Kết hợp', N'2 ngày 1 đêm', 2300000, N'Nguồn: Viet Fun Travel; mã VF180. Ngày đầu Bình Châu; ngày sau tham quan biển Hồ Cốc. Lưu trú theo gói của nhà cung cấp. Giá từ trên website nguồn, tra cứu 28/10/2026; không phải giá bán đã xác nhận của DongNamTrip. https://www.vietfuntravel.com.vn/tour-vung-tau/tour-du-lich-binh-chau-ho-coc-2-ngay-1-dem.html', N'Chưa xác nhận chính sách áp dụng. Liên hệ nhà cung cấp và chốt điều kiện trước khi mở bán; không dùng bản tham khảo để xác nhận đơn.', N'P4'),
    (N'VF182', N'Tham khảo Viet Fun VF182 - Bình Châu, Hồ Cốc; ngủ Hồ Cốc', N'Vũng Tàu', N'Kết hợp', N'2 ngày 1 đêm', 2300000, N'Nguồn: Viet Fun Travel; mã VF182. Ngày đầu đi Bình Châu rồi đến Hồ Cốc; lưu trú Hồ Cốc theo chương trình công khai. Giá từ trên website nguồn, tra cứu 28/10/2026; không phải giá bán đã xác nhận của DongNamTrip. https://www.vietfuntravel.com.vn/tour-vung-tau/tour-du-lich-ho-coc-binh-chau-2-ngay-1-dem.html', N'Chưa xác nhận chính sách áp dụng. Liên hệ nhà cung cấp và chốt điều kiện trước khi mở bán; không dùng bản tham khảo để xác nhận đơn.', N'V4'),
    (N'NAC5157', N'Tham khảo Nam Á Châu 5157 - Củ Chi và Tây Ninh', N'Tây Ninh', N'Kết hợp', N'1 ngày', 1690000, N'Nguồn: Nam Á Châu; mã NAC5157. Chương trình công khai đi Củ Chi, Tòa Thánh Cao Đài rồi núi Bà Đen; cần xác nhận lại trước khi đặt. Giá từ trên website nguồn, tra cứu 28/10/2026; không phải giá bán đã xác nhận của DongNamTrip. https://dulichnamachau.vn/tour-du-lich/tour-cu-chi-tay-ninh-1-ngay-dia-dao-nui-ba-den-toa-thanh/', N'Chưa xác nhận chính sách áp dụng. Liên hệ nhà cung cấp và chốt điều kiện trước khi mở bán; không dùng bản tham khảo để xác nhận đơn.', N'P5');

DECLARE @DiemTour TABLE (
    MaTourBienSoan varchar(10), MaDiem varchar(10), NgayThu int, ThuTu int,
    PRIMARY KEY (MaTourBienSoan, MaDiem), UNIQUE (MaTourBienSoan, NgayThu, ThuTu)
);
INSERT INTO @DiemTour VALUES
    (N'VF03', N'HCM05', 1, 1),
    (N'VF04', N'TN02', 1, 1),
    (N'VF04', N'HCM05', 1, 2),
    (N'VF393', N'TN01', 1, 1),
    (N'VF393', N'TN02', 1, 2),
    (N'VF393', N'HCM05', 1, 3),
    (N'VF184', N'VT01', 1, 1),
    (N'VF184', N'VT06', 1, 2),
    (N'VF184', N'VT02', 1, 3),
    (N'VF184', N'VT05', 1, 4),
    (N'VF184', N'VT07', 1, 5),
    (N'VF184', N'VT08', 1, 6),
    (N'VF181', N'VT09', 1, 1),
    (N'VF180', N'VT09', 1, 1),
    (N'VF180', N'VT10', 2, 1),
    (N'VF182', N'VT09', 1, 1),
    (N'VF182', N'VT10', 1, 2),
    (N'NAC5157', N'HCM05', 1, 1),
    (N'NAC5157', N'TN02', 1, 2),
    (N'NAC5157', N'TN01', 1, 3);


    DECLARE @MapDiem TABLE (MaBienSoan varchar(10) PRIMARY KEY, MaDD int UNIQUE);
    DECLARE @MapTour TABLE (MaBienSoan varchar(10) PRIMARY KEY, MaTour int UNIQUE);
    DECLARE @DiemMoi TABLE (MaDD int PRIMARY KEY);
    DECLARE @HoatDongMoi TABLE (MaHD int PRIMARY KEY);
    DECLARE @TourMoi TABLE (MaTour int PRIMARY KEY);
    DECLARE @LienKetMoi TABLE (MaTour int, MaDD int);
    DECLARE @Dem bigint, @SoThemDiem int, @SoThemHD int, @SoThemTour int, @SoThemLK int;
    -- Bao ve khoa tu nhien chua co UNIQUE trong schema goc.
    SELECT @Dem = COUNT_BIG(*) FROM dbo.DIADIEM WITH (TABLOCKX, HOLDLOCK);
    SELECT @Dem = COUNT_BIG(*) FROM dbo.HOATDONG WITH (TABLOCKX, HOLDLOCK);
    SELECT @Dem = COUNT_BIG(*) FROM dbo.TOUR WITH (TABLOCKX, HOLDLOCK);
    SELECT @Dem = COUNT_BIG(*) FROM dbo.TOUR_DIADIEM WITH (TABLOCKX, HOLDLOCK);

    IF EXISTS (
        SELECT s.MaBienSoan FROM @Diem s JOIN dbo.DIADIEM d
          ON d.TenDD COLLATE Vietnamese_100_CI_AI = s.TenDD COLLATE Vietnamese_100_CI_AI
         AND d.KhuVuc COLLATE Vietnamese_100_CI_AI = s.KhuVuc COLLATE Vietnamese_100_CI_AI
        GROUP BY s.MaBienSoan HAVING COUNT_BIG(*) > 1
    ) THROW 51104, N'Dia diem co nhieu ban ghi trung ten/vung. Khong tu dong gop.', 1;

    INSERT INTO dbo.DIADIEM
        (TenDD, KhuVuc, LoaiHinh, DiaChi, ViDo, KinhDo, MoTa,
         ThoiLuongPhut, GioMoCua, GioDongCua, AnhMinhHoa, TrangThai)
    OUTPUT inserted.MaDD INTO @DiemMoi
    SELECT s.TenDD, s.KhuVuc, s.LoaiHinh, s.DiaChi, NULL, NULL, s.MoTa,
           s.ThoiLuongPhut, NULL, NULL, NULL, 'Published'
    FROM @Diem s WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DIADIEM d
        WHERE d.TenDD COLLATE Vietnamese_100_CI_AI = s.TenDD COLLATE Vietnamese_100_CI_AI
          AND d.KhuVuc COLLATE Vietnamese_100_CI_AI = s.KhuVuc COLLATE Vietnamese_100_CI_AI
    );
    SET @SoThemDiem = @@ROWCOUNT;

    INSERT INTO @MapDiem
    SELECT s.MaBienSoan, d.MaDD FROM @Diem s JOIN dbo.DIADIEM d
      ON d.TenDD COLLATE Vietnamese_100_CI_AI = s.TenDD COLLATE Vietnamese_100_CI_AI
     AND d.KhuVuc COLLATE Vietnamese_100_CI_AI = s.KhuVuc COLLATE Vietnamese_100_CI_AI;

    IF (SELECT COUNT(*) FROM @MapDiem) <> (SELECT COUNT(*) FROM @Diem)
        THROW 51105, N'Khong anh xa duoc day du dia diem.', 1;
    IF EXISTS (
        SELECT m.MaDD, s.TenHD FROM @HoatDong s
        JOIN @MapDiem m ON m.MaBienSoan = s.MaDiem
        JOIN dbo.HOATDONG h ON h.MaDD = m.MaDD
         AND h.TenHD COLLATE Vietnamese_100_CI_AI = s.TenHD COLLATE Vietnamese_100_CI_AI
        GROUP BY m.MaDD, s.TenHD HAVING COUNT_BIG(*) > 1
    ) THROW 51106, N'Hoat dong co ban ghi trung. Hay doi chieu thu cong.', 1;

    INSERT INTO dbo.HOATDONG (MaDD, TenHD, MoTa, ThoiLuongPhut, DieuKien, PhiThamKhao, TrangThai)
    OUTPUT inserted.MaHD INTO @HoatDongMoi
    SELECT m.MaDD, s.TenHD, s.MoTa, s.ThoiLuongPhut, s.DieuKien, NULL,
           CASE WHEN d.TrangThai = 'Published' THEN 'Published' ELSE 'Draft' END
    FROM @HoatDong s JOIN @MapDiem m ON m.MaBienSoan = s.MaDiem
    JOIN dbo.DIADIEM d ON d.MaDD = m.MaDD
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.HOATDONG h WHERE h.MaDD = m.MaDD
          AND h.TenHD COLLATE Vietnamese_100_CI_AI = s.TenHD COLLATE Vietnamese_100_CI_AI
    );
    SET @SoThemHD = @@ROWCOUNT;

    IF EXISTS (
        SELECT s.MaBienSoan FROM @Tour s JOIN dbo.TOUR t
          ON t.TenTour COLLATE Vietnamese_100_CI_AI = s.TenTour COLLATE Vietnamese_100_CI_AI
         AND t.KhuVuc COLLATE Vietnamese_100_CI_AI = s.KhuVuc COLLATE Vietnamese_100_CI_AI
        GROUP BY s.MaBienSoan HAVING COUNT_BIG(*) > 1
    ) THROW 51107, N'Tour co nhieu ban ghi trung. Khong tu dong gop.', 1;

    INSERT INTO dbo.TOUR
        (TenTour, KhuVuc, LoaiHinh, ThoiLuong, GiaThamKhao, TomTat, DieuKienHuy, TrangThai)
    OUTPUT inserted.MaTour INTO @TourMoi
    SELECT s.TenTour, s.KhuVuc, s.LoaiHinh, s.ThoiLuong, s.GiaThamKhao,
           s.TomTat, s.DieuKienHuy, 'Draft'
    FROM @Tour s WHERE NOT EXISTS (
        SELECT 1 FROM dbo.TOUR t
        WHERE t.TenTour COLLATE Vietnamese_100_CI_AI = s.TenTour COLLATE Vietnamese_100_CI_AI
          AND t.KhuVuc COLLATE Vietnamese_100_CI_AI = s.KhuVuc COLLATE Vietnamese_100_CI_AI
    );
    SET @SoThemTour = @@ROWCOUNT;

    INSERT INTO @MapTour
    SELECT s.MaBienSoan, t.MaTour FROM @Tour s JOIN dbo.TOUR t
      ON t.TenTour COLLATE Vietnamese_100_CI_AI = s.TenTour COLLATE Vietnamese_100_CI_AI
     AND t.KhuVuc COLLATE Vietnamese_100_CI_AI = s.KhuVuc COLLATE Vietnamese_100_CI_AI;

    INSERT INTO dbo.TOUR_DIADIEM (MaTour, MaDD, NgayThu, ThuTu)
    OUTPUT inserted.MaTour, inserted.MaDD INTO @LienKetMoi
    SELECT t.MaTour, d.MaDD, s.NgayThu, s.ThuTu FROM @DiemTour s
    JOIN @MapTour t ON t.MaBienSoan = s.MaTourBienSoan
    JOIN @MapDiem d ON d.MaBienSoan = s.MaDiem
    JOIN @TourMoi n ON n.MaTour = t.MaTour;
    SET @SoThemLK = @@ROWCOUNT;

    IF @SoThemDiem + @SoThemHD + @SoThemTour + @SoThemLK > 0
       AND OBJECT_ID(N'dbo.NHATKYTHAOTAC', N'U') IS NOT NULL
    BEGIN
        DECLARE @NguonJSON nvarchar(max) = (
            SELECT MaNguon, TenNguon, URL FROM @Nguon ORDER BY MaNguon FOR JSON PATH
        );
        DECLARE @DiemJSON nvarchar(max) = (
            SELECT m.MaDD, s.MaBienSoan, s.MaNguon FROM @MapDiem m
            JOIN @Diem s ON s.MaBienSoan = m.MaBienSoan FOR JSON PATH
        );
        DECLARE @TourJSON nvarchar(max) = (
            SELECT m.MaTour, s.MaBienSoan, s.MaNguon FROM @MapTour m
            JOIN @Tour s ON s.MaBienSoan = m.MaBienSoan FOR JSON PATH
        );
        DECLARE @DuLieuSau nvarchar(max) = (
            SELECT @NgayTraCuu AS ngayTraCuu, @SoThemDiem AS diaDiemMoi,
                   @SoThemHD AS goiYTraiNghiemMoi, @SoThemTour AS tourThamKhaoMoi,
                   @SoThemLK AS lienKetMoi, JSON_QUERY(@NguonJSON) AS nguon,
                   JSON_QUERY(@DiemJSON) AS doiChieuDiaDiem,
                   JSON_QUERY(@TourJSON) AS doiChieuTour
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );
        INSERT INTO dbo.NHATKYTHAOTAC
            (MaTK, HanhDong, LoaiDoiTuong, MaDoiTuong, LyDo, DuLieuSau)
        VALUES (NULL, N'SeedCatalogPublicData', N'Catalog', N'PublicCatalog.20261028',
                N'Bổ sung danh mục từ nguồn công khai; tour nháp, không tạo lịch hoặc giao dịch.', @DuLieuSau);
    END;


    -- PHẦN 5: MÔ TẢ SCHEMA VÀ KIỂM TRA TRƯỚC KHI COMMIT.
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu danh mục vai trò và ý nghĩa của từng vai trò.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'VAITRO';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu thông tin đăng nhập, liên hệ và trạng thái tài khoản.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'TAIKHOAN';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu việc gán một hoặc nhiều vai trò cho tài khoản.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'TAIKHOAN_VAITRO';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu thông tin địa điểm, vị trí, thời lượng và trạng thái công bố.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'DIADIEM';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu hoạt động tại địa điểm, điều kiện tham gia và phí tham khảo.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'HOATDONG';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu ngày giờ tổ chức và trạng thái công bố của hoạt động.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'SUAT';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu chương trình tour mẫu, giá tham khảo và điều kiện hủy.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'TOUR';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Liên kết tour với địa điểm, ngày thứ và thứ tự tham quan.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'TOUR_DIADIEM';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu từng đợt tổ chức tour, giá, sức chứa và số chỗ đã giữ.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'LICHKHOIHANH';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu kế hoạch chuyến đi hiện tại và tài khoản sở hữu.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'LICHTRINH';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu các điểm dự kiến tham quan và thứ tự của lịch trình cá nhân.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'LICHTRINH_DIADIEM';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu các suất hoạt động khách mong muốn tham gia tại từng địa điểm.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'LICHTRINH_SUAT';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu bản chụp lịch trình tại thời điểm gửi yêu cầu hoặc chốt phương án.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'PHIENBANLICHTRINH';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu nhu cầu tổ chức chuyến đi dựa trên một phiên bản lịch trình.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'YEUCAUTOURRIENG';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu kết quả kiểm tra từng dịch vụ với đơn vị cung cấp trước khi báo giá.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'XACMINHDICHVU';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu giá và điều kiện gắn với yêu cầu cùng phiên bản lịch trình được báo giá.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'BAOGIA';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu từng dịch vụ, số lượng và đơn giá cấu thành báo giá.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'HANGMUCBAOGIA';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu đơn tour có sẵn hoặc tour riêng cùng giá và điều kiện đã chốt.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'DONDAT';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu các khoản thu và hoàn tiền của đơn, kèm người ghi nhận và mã đối soát.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'GIAODICHTIEN';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu mã và tên ngôn ngữ được hỗ trợ cho bản dịch.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'NGONNGU';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu nội dung gốc và phiên bản thuyết minh của địa điểm.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'THUYETMINH';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu nội dung theo ngôn ngữ cho một phiên bản thuyết minh gốc.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'BANDICH';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu tệp giọng đọc gắn với bản dịch và trạng thái công bố.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'TEPAUDIO';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu quyền nghe theo tài khoản, đơn đã xác nhận và khoảng hiệu lực.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'QUYENNGHE';
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Lưu người thực hiện, thời điểm, đối tượng, lý do và dữ liệu thay đổi để đáp ứng UC24 và BR13.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'NHATKYTHAOTAC';

    IF (SELECT COUNT(*) FROM dbo.VAITRO) <> 6
       OR (SELECT COUNT(*) FROM dbo.NGONNGU) <> 3
       OR (SELECT COUNT(*) FROM dbo.DIADIEM) <> 30
       OR (SELECT COUNT(*) FROM dbo.HOATDONG) <> 60
       OR (SELECT COUNT(*) FROM dbo.TOUR) <> 8
       OR (SELECT COUNT(*) FROM dbo.TOUR_DIADIEM) <> 20
    BEGIN
        ;THROW 51206, N'Số lượng dữ liệu khởi tạo không đúng. Hoàn tác toàn bộ bảng/dữ liệu.', 1;
    END;
    IF EXISTS (
        SELECT 1 FROM @CacBang b WHERE OBJECT_ID(N'dbo.' + QUOTENAME(b.TenBang), N'U') IS NULL
    ) OR (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) <> 25
    BEGIN
        ;THROW 51207, N'Cấu trúc phải có đủ 25 bảng.', 1;
    END;

    EXEC sys.sp_addextendedproperty
        @name=N'DongNamTrip.SchemaVersion', @value=N'2026-10-27.1';
    EXEC sys.sp_addextendedproperty
        @name=N'DongNamTrip.FullBundleVersion', @value=@PhienBanFull;

    COMMIT TRANSACTION;

    PRINT N'HOÀN THÀNH: tạo CSDL, 25 bảng và nạp dữ liệu dùng chung.';
    PRINT N'7 tài khoản kiểm thử mới: mật khẩu ban đầu chung DntTest@2026!';
    PRINT N'8 tour tham khảo đang Draft; chưa seed lịch khởi hành, đơn, giao dịch hoặc audio.';

    SELECT DB_NAME() AS CoSoDuLieu, @PhienBanFull AS PhienBan,
           (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) AS SoBang,
           @SoThemDiem AS DiaDiem, @SoThemHD AS GoiYTraiNghiem,
           @SoThemTour AS TourThamKhao, @SoThemLK AS LienKetTourDiaDiem;

    -- Số dòng từng bảng: nhóm nghiệp vụ chưa chạy được phép bằng 0.
    SELECT t.name AS TenBang,
           COALESCE(SUM(CASE WHEN p.index_id IN (0,1) THEN p.rows ELSE 0 END),0) AS SoDong
    FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
    LEFT JOIN sys.partitions p ON p.object_id=t.object_id
    WHERE s.name=N'dbo' AND t.name IN (SELECT TenBang FROM @CacBang)
    GROUP BY t.name ORDER BY t.name;

    -- Xem tài khoản kèm vai trò, không trả cột hash mật khẩu.
    SELECT t.MaTK, t.HoTen, t.Email, t.SoDienThoai, v.TenVaiTro AS VaiTro, t.TrangThai
    FROM dbo.TAIKHOAN t
    JOIN dbo.TAIKHOAN_VAITRO tv ON tv.MaTK=t.MaTK
    JOIN dbo.VAITRO v ON v.MaVaiTro=tv.MaVaiTro
    ORDER BY t.MaTK;

    SELECT KhuVuc, COUNT(*) AS SoDiaDiem FROM dbo.DIADIEM GROUP BY KhuVuc ORDER BY KhuVuc;
    SELECT TenTour, GiaThamKhao, TrangThai FROM dbo.TOUR ORDER BY MaTour;
    SELECT MaNguon, TenNguon, URL FROM @Nguon ORDER BY MaNguon;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
