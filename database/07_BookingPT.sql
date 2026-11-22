SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.PTSchedules', N'BookingId') IS NULL
    ALTER TABLE dbo.PTSchedules ADD BookingId bigint NULL;

IF OBJECT_ID(N'dbo.FK_PTSchedules_Bookings', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.PTSchedules ADD CONSTRAINT FK_PTSchedules_Bookings
           FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(BookingId)');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PTSchedules')
               AND name = N'UX_PTSchedules_ScheduledBooking')
    EXEC(N'CREATE UNIQUE INDEX UX_PTSchedules_ScheduledBooking ON dbo.PTSchedules(BookingId)
           WHERE BookingId IS NOT NULL AND Status = N''SCHEDULED''');

COMMIT TRANSACTION;
