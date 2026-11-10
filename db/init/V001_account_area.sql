/*
 V001 - KHU VỰC TÀI KHOẢN: yêu thích, lịch sử, tùy chọn, thông báo.
 Chạy sau DongNamTrip_Full.sql. An toàn khi chạy lại: chỉ tạo bảng/dữ liệu chưa có.
 Tên bảng/cột tiếng Việt như schema gốc; EF ánh xạ trong Administration/DataAccess/AccountArea.cs.
*/
USE [DongNamTrip];
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.TAIKHOAN_YEUTHICH', N'U') IS NULL
CREATE TABLE dbo.TAIKHOAN_YEUTHICH (
    MaTK         int         NOT NULL,
    LoaiDoiTuong varchar(12) NOT NULL,   -- 'tour' | 'destination'
    MaDoiTuong   int         NOT NULL,
    NgayLuu      datetime2   NOT NULL CONSTRAINT DF_TAIKHOAN_YEUTHICH_NgayLuu DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_TAIKHOAN_YEUTHICH PRIMARY KEY (MaTK, LoaiDoiTuong, MaDoiTuong),
    CONSTRAINT FK_TAIKHOAN_YEUTHICH_MaTK FOREIGN KEY (MaTK) REFERENCES dbo.TAIKHOAN (MaTK),
    CONSTRAINT CK_TAIKHOAN_YEUTHICH_LoaiDoiTuong CHECK (LoaiDoiTuong IN ('tour', 'destination'))
);
GO

-- LoaiDoiTuong không có CHECK để dễ mở rộng; API tự kiểm tra giá trị hợp lệ.
IF OBJECT_ID(N'dbo.TAIKHOAN_LICHSU', N'U') IS NULL
CREATE TABLE dbo.TAIKHOAN_LICHSU (
    MaLS       bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TAIKHOAN_LICHSU PRIMARY KEY,
    MaTK         int           NOT NULL,
    LoaiDoiTuong varchar(12)   NOT NULL,   -- 'search' | 'tour' | 'destination'
    MaDoiTuong   int           NULL,
    TuKhoa       nvarchar(200) NULL,
    ThoiDiem     datetime2     NOT NULL CONSTRAINT DF_TAIKHOAN_LICHSU_ThoiDiem DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_TAIKHOAN_LICHSU_MaTK FOREIGN KEY (MaTK) REFERENCES dbo.TAIKHOAN (MaTK)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TAIKHOAN_LICHSU_MaTK' AND object_id = OBJECT_ID(N'dbo.TAIKHOAN_LICHSU'))
    CREATE INDEX IX_TAIKHOAN_LICHSU_MaTK ON dbo.TAIKHOAN_LICHSU (MaTK, ThoiDiem DESC, MaLS DESC);
GO

IF OBJECT_ID(N'dbo.TAIKHOAN_TUYCHON', N'U') IS NULL
CREATE TABLE dbo.TAIKHOAN_TUYCHON (
    MaTK            int NOT NULL CONSTRAINT PK_TAIKHOAN_TUYCHON PRIMARY KEY,
    NhanTBBaoMat    bit NOT NULL CONSTRAINT DF_TAIKHOAN_TUYCHON_NhanTBBaoMat DEFAULT (1),
    CONSTRAINT FK_TAIKHOAN_TUYCHON_MaTK FOREIGN KEY (MaTK) REFERENCES dbo.TAIKHOAN (MaTK)
);
GO

IF OBJECT_ID(N'dbo.TAIKHOAN_THONGBAO', N'U') IS NULL
CREATE TABLE dbo.TAIKHOAN_THONGBAO (
    MaTB      bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TAIKHOAN_THONGBAO PRIMARY KEY,
    MaTK      int           NOT NULL,
    TieuDe    nvarchar(160) NOT NULL,
    NoiDung   nvarchar(500) NOT NULL,
    NgayTao   datetime2     NOT NULL CONSTRAINT DF_TAIKHOAN_THONGBAO_NgayTao DEFAULT (SYSUTCDATETIME()),
    NgayDoc   datetime2     NULL,
    CONSTRAINT FK_TAIKHOAN_THONGBAO_MaTK FOREIGN KEY (MaTK) REFERENCES dbo.TAIKHOAN (MaTK)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TAIKHOAN_THONGBAO_MaTK' AND object_id = OBJECT_ID(N'dbo.TAIKHOAN_THONGBAO'))
    CREATE INDEX IX_TAIKHOAN_THONGBAO_MaTK ON dbo.TAIKHOAN_THONGBAO (MaTK, NgayTao DESC);
GO

-- Dữ liệu mẫu cho tài khoản kiểm thử customer1/customer2 (đã có trong DongNamTrip_Full.sql).
INSERT dbo.TAIKHOAN_TUYCHON (MaTK, NhanTBBaoMat)
SELECT t.MaTK, 1
FROM dbo.TAIKHOAN t
WHERE t.Email IN (N'customer1@dongnamtrip.test', N'customer2@dongnamtrip.test')
  AND NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN_TUYCHON p WHERE p.MaTK = t.MaTK);

INSERT dbo.TAIKHOAN_THONGBAO (MaTK, TieuDe, NoiDung)
SELECT t.MaTK, N'Chào mừng đến DongNamTrip',
       N'Tài khoản kiểm thử đã sẵn sàng. Bạn có thể lưu địa điểm, tour yêu thích và xem lại lịch sử tại đây.'
FROM dbo.TAIKHOAN t
WHERE t.Email IN (N'customer1@dongnamtrip.test', N'customer2@dongnamtrip.test')
  AND NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN_THONGBAO n WHERE n.MaTK = t.MaTK);

INSERT dbo.TAIKHOAN_YEUTHICH (MaTK, LoaiDoiTuong, MaDoiTuong)
SELECT t.MaTK, 'destination', d.MaDD
FROM dbo.TAIKHOAN t
CROSS APPLY (SELECT TOP (2) MaDD FROM dbo.DIADIEM WHERE TrangThai = 'Published' ORDER BY MaDD) d
WHERE t.Email = N'customer1@dongnamtrip.test'
  AND NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN_YEUTHICH f WHERE f.MaTK = t.MaTK);

INSERT dbo.TAIKHOAN_LICHSU (MaTK, LoaiDoiTuong, MaDoiTuong, TuKhoa)
SELECT t.MaTK, 'search', NULL, N'Vũng Tàu'
FROM dbo.TAIKHOAN t
WHERE t.Email = N'customer1@dongnamtrip.test'
  AND NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN_LICHSU a WHERE a.MaTK = t.MaTK);

INSERT dbo.TAIKHOAN_LICHSU (MaTK, LoaiDoiTuong, MaDoiTuong, TuKhoa)
SELECT t.MaTK, 'destination', d.MaDD, NULL
FROM dbo.TAIKHOAN t
CROSS APPLY (SELECT TOP (1) MaDD FROM dbo.DIADIEM WHERE TrangThai = 'Published' ORDER BY MaDD) d
WHERE t.Email = N'customer1@dongnamtrip.test'
  AND NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN_LICHSU a WHERE a.MaTK = t.MaTK AND a.LoaiDoiTuong = 'destination');
GO
