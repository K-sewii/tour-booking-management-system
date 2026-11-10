/*
 V002 - LỰA CHỌN PHƯƠNG THỨC THANH TOÁN của tài khoản.
 Chạy sau V001. An toàn khi chạy lại. PhuongThuc khớp BookingEndpoints: bank_transfer | cash.
*/
USE [DongNamTrip];
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.TAIKHOAN_THANHTOAN', N'U') IS NULL
CREATE TABLE dbo.TAIKHOAN_THANHTOAN (
    MaTK         int         NOT NULL CONSTRAINT PK_TAIKHOAN_THANHTOAN PRIMARY KEY,
    PhuongThuc   varchar(20) NOT NULL,
    NgayCapNhat  datetime2   NOT NULL CONSTRAINT DF_TAIKHOAN_THANHTOAN_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_TAIKHOAN_THANHTOAN_MaTK FOREIGN KEY (MaTK) REFERENCES dbo.TAIKHOAN (MaTK),
    CONSTRAINT CK_TAIKHOAN_THANHTOAN_PhuongThuc CHECK (PhuongThuc IN ('bank_transfer', 'cash'))
);
GO

INSERT dbo.TAIKHOAN_THANHTOAN (MaTK, PhuongThuc)
SELECT t.MaTK, 'bank_transfer'
FROM dbo.TAIKHOAN t
WHERE t.Email = N'customer1@dongnamtrip.test'
  AND NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN_THANHTOAN c WHERE c.MaTK = t.MaTK);
GO
