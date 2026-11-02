/*
 V001 - KHU VỰC TÀI KHOẢN: yêu thích, lịch sử, tùy chọn, thông báo.
 Chạy sau DongNamTrip_Full.sql. An toàn khi chạy lại: chỉ tạo bảng/dữ liệu chưa có.
 Cột và kiểu khớp DongNamTrip.Modules/Administration/DataAccess/AccountArea.cs.
*/
USE [DongNamTrip];
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ACCOUNT_FAVORITE', N'U') IS NULL
CREATE TABLE dbo.ACCOUNT_FAVORITE (
    AccountId int         NOT NULL,
    Kind      varchar(12) NOT NULL,   -- 'tour' | 'destination'
    ItemId    int         NOT NULL,
    SavedAt   datetime2   NOT NULL CONSTRAINT DF_ACCOUNT_FAVORITE_SavedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ACCOUNT_FAVORITE PRIMARY KEY (AccountId, Kind, ItemId),
    CONSTRAINT FK_ACCOUNT_FAVORITE_TAIKHOAN FOREIGN KEY (AccountId) REFERENCES dbo.TAIKHOAN (MaTK),
    CONSTRAINT CK_ACCOUNT_FAVORITE_Kind CHECK (Kind IN ('tour', 'destination'))
);
GO

-- Kind không có CHECK để dễ mở rộng; API tự kiểm tra giá trị hợp lệ.
IF OBJECT_ID(N'dbo.ACCOUNT_ACTIVITY', N'U') IS NULL
CREATE TABLE dbo.ACCOUNT_ACTIVITY (
    Id        bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ACCOUNT_ACTIVITY PRIMARY KEY,
    AccountId int           NOT NULL,
    Kind      varchar(12)   NOT NULL,   -- 'search' | 'tour' | 'destination'
    ItemId    int           NULL,
    Query     nvarchar(200) NULL,
    CreatedAt datetime2     NOT NULL CONSTRAINT DF_ACCOUNT_ACTIVITY_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_ACCOUNT_ACTIVITY_TAIKHOAN FOREIGN KEY (AccountId) REFERENCES dbo.TAIKHOAN (MaTK)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ACCOUNT_ACTIVITY_Account' AND object_id = OBJECT_ID(N'dbo.ACCOUNT_ACTIVITY'))
    CREATE INDEX IX_ACCOUNT_ACTIVITY_Account ON dbo.ACCOUNT_ACTIVITY (AccountId, CreatedAt DESC, Id DESC);
GO

IF OBJECT_ID(N'dbo.ACCOUNT_PREFERENCE', N'U') IS NULL
CREATE TABLE dbo.ACCOUNT_PREFERENCE (
    AccountId       int NOT NULL CONSTRAINT PK_ACCOUNT_PREFERENCE PRIMARY KEY,
    SecurityNotices bit NOT NULL CONSTRAINT DF_ACCOUNT_PREFERENCE_SecurityNotices DEFAULT (1),
    CONSTRAINT FK_ACCOUNT_PREFERENCE_TAIKHOAN FOREIGN KEY (AccountId) REFERENCES dbo.TAIKHOAN (MaTK)
);
GO

IF OBJECT_ID(N'dbo.ACCOUNT_NOTICE', N'U') IS NULL
CREATE TABLE dbo.ACCOUNT_NOTICE (
    Id        bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ACCOUNT_NOTICE PRIMARY KEY,
    AccountId int           NOT NULL,
    Title     nvarchar(160) NOT NULL,
    Body      nvarchar(500) NOT NULL,
    CreatedAt datetime2     NOT NULL CONSTRAINT DF_ACCOUNT_NOTICE_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ReadAt    datetime2     NULL,
    CONSTRAINT FK_ACCOUNT_NOTICE_TAIKHOAN FOREIGN KEY (AccountId) REFERENCES dbo.TAIKHOAN (MaTK)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ACCOUNT_NOTICE_Account' AND object_id = OBJECT_ID(N'dbo.ACCOUNT_NOTICE'))
    CREATE INDEX IX_ACCOUNT_NOTICE_Account ON dbo.ACCOUNT_NOTICE (AccountId, CreatedAt DESC);
GO

-- Dữ liệu mẫu cho tài khoản kiểm thử customer1/customer2 (đã có trong DongNamTrip_Full.sql).
INSERT dbo.ACCOUNT_PREFERENCE (AccountId, SecurityNotices)
SELECT t.MaTK, 1
FROM dbo.TAIKHOAN t
WHERE t.Email IN (N'customer1@dongnamtrip.test', N'customer2@dongnamtrip.test')
  AND NOT EXISTS (SELECT 1 FROM dbo.ACCOUNT_PREFERENCE p WHERE p.AccountId = t.MaTK);

INSERT dbo.ACCOUNT_NOTICE (AccountId, Title, Body)
SELECT t.MaTK, N'Chào mừng đến DongNamTrip',
       N'Tài khoản kiểm thử đã sẵn sàng. Bạn có thể lưu địa điểm, tour yêu thích và xem lại lịch sử tại đây.'
FROM dbo.TAIKHOAN t
WHERE t.Email IN (N'customer1@dongnamtrip.test', N'customer2@dongnamtrip.test')
  AND NOT EXISTS (SELECT 1 FROM dbo.ACCOUNT_NOTICE n WHERE n.AccountId = t.MaTK);

INSERT dbo.ACCOUNT_FAVORITE (AccountId, Kind, ItemId)
SELECT t.MaTK, 'destination', d.MaDD
FROM dbo.TAIKHOAN t
CROSS APPLY (SELECT TOP (2) MaDD FROM dbo.DIADIEM WHERE TrangThai = 'Published' ORDER BY MaDD) d
WHERE t.Email = N'customer1@dongnamtrip.test'
  AND NOT EXISTS (SELECT 1 FROM dbo.ACCOUNT_FAVORITE f WHERE f.AccountId = t.MaTK);

INSERT dbo.ACCOUNT_ACTIVITY (AccountId, Kind, ItemId, Query)
SELECT t.MaTK, 'search', NULL, N'Vũng Tàu'
FROM dbo.TAIKHOAN t
WHERE t.Email = N'customer1@dongnamtrip.test'
  AND NOT EXISTS (SELECT 1 FROM dbo.ACCOUNT_ACTIVITY a WHERE a.AccountId = t.MaTK);

INSERT dbo.ACCOUNT_ACTIVITY (AccountId, Kind, ItemId, Query)
SELECT t.MaTK, 'destination', d.MaDD, NULL
FROM dbo.TAIKHOAN t
CROSS APPLY (SELECT TOP (1) MaDD FROM dbo.DIADIEM WHERE TrangThai = 'Published' ORDER BY MaDD) d
WHERE t.Email = N'customer1@dongnamtrip.test'
  AND NOT EXISTS (SELECT 1 FROM dbo.ACCOUNT_ACTIVITY a WHERE a.AccountId = t.MaTK AND a.Kind = 'destination');
GO
