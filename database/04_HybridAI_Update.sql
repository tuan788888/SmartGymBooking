-- ============================================================
-- SMARTGYM BOOKING
-- 04_HybridAI_Update.sql
-- Hybrid AI fields
-- ============================================================

USE SmartGymBookingDB;
GO

ALTER TABLE dbo.Customers
ADD
    ActivityLevel NVARCHAR(20) NULL,
    PreferredSessionsPerWeek INT NULL,
    PreferredSessionMinutes INT NULL;
GO

ALTER TABLE dbo.Customers
ADD CONSTRAINT CK_Customers_ActivityLevel
CHECK
(
    ActivityLevel IS NULL
    OR ActivityLevel IN (N'LOW', N'MODERATE', N'HIGH')
);
GO

ALTER TABLE dbo.Customers
ADD CONSTRAINT CK_Customers_PreferredSessions
CHECK
(
    PreferredSessionsPerWeek IS NULL
    OR PreferredSessionsPerWeek BETWEEN 1 AND 7
);
GO

ALTER TABLE dbo.Customers
ADD CONSTRAINT CK_Customers_PreferredMinutes
CHECK
(
    PreferredSessionMinutes IS NULL
    OR PreferredSessionMinutes BETWEEN 15 AND 180
);
GO

ALTER TABLE dbo.WorkoutPlans
ADD
    ModelName NVARCHAR(100) NULL,
    ModelVersion NVARCHAR(50) NULL,
    PredictedPlanType NVARCHAR(100) NULL;
GO

CREATE INDEX IX_WorkoutPlans_PredictedPlanType
ON dbo.WorkoutPlans(PredictedPlanType);
GO

PRINT N'04_HybridAI_Update.sql COMPLETED';
GO
