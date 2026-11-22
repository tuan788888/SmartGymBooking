SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.Payments', N'BankReference') IS NULL
    ALTER TABLE dbo.Payments ADD BankReference nvarchar(100) NULL;

IF COL_LENGTH(N'dbo.Payments', N'ReconciledByUserId') IS NULL
    ALTER TABLE dbo.Payments ADD ReconciledByUserId bigint NULL;

IF OBJECT_ID(N'dbo.FK_Payments_ReconciledBy', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.Payments ADD CONSTRAINT FK_Payments_ReconciledBy
           FOREIGN KEY (ReconciledByUserId) REFERENCES dbo.Users(UserId)');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Payments')
               AND name = N'UX_Payments_BankReference')
    EXEC(N'CREATE UNIQUE INDEX UX_Payments_BankReference ON dbo.Payments(BankReference)
           WHERE BankReference IS NOT NULL');

COMMIT TRANSACTION;
