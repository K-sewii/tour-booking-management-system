/*
 V002 - LỰA CHỌN PHƯƠNG THỨC THANH TOÁN của tài khoản.
 Chạy sau V001. An toàn khi chạy lại. MethodCode khớp BookingEndpoints: bank_transfer | cash.
*/
USE [DongNamTrip];
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ACCOUNT_PAYMENT_CHOICE', N'U') IS NULL
CREATE TABLE dbo.ACCOUNT_PAYMENT_CHOICE (
    AccountId  int         NOT NULL CONSTRAINT PK_ACCOUNT_PAYMENT_CHOICE PRIMARY KEY,
    MethodCode varchar(20) NOT NULL,
    UpdatedAt  datetime2   NOT NULL CONSTRAINT DF_ACCOUNT_PAYMENT_CHOICE_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_ACCOUNT_PAYMENT_CHOICE_TAIKHOAN FOREIGN KEY (AccountId) REFERENCES dbo.TAIKHOAN (MaTK),
    CONSTRAINT CK_ACCOUNT_PAYMENT_CHOICE_Method CHECK (MethodCode IN ('bank_transfer', 'cash'))
);
GO

INSERT dbo.ACCOUNT_PAYMENT_CHOICE (AccountId, MethodCode)
SELECT t.MaTK, 'bank_transfer'
FROM dbo.TAIKHOAN t
WHERE t.Email = N'customer1@dongnamtrip.test'
  AND NOT EXISTS (SELECT 1 FROM dbo.ACCOUNT_PAYMENT_CHOICE c WHERE c.AccountId = t.MaTK);
GO
