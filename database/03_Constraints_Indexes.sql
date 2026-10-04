-- ============================================================
-- SMARTGYM BOOKING
-- 03_Constraints_Indexes.sql
-- Constraints + indexes
-- ============================================================

USE SmartGymBookingDB;
GO

ALTER TABLE dbo.Users
ADD CONSTRAINT UQ_Users_Email UNIQUE (Email);
GO

ALTER TABLE dbo.Users
ADD CONSTRAINT CK_Users_Role
CHECK (Role IN (N'ADMIN', N'EMPLOYEE', N'CUSTOMER'));
GO

ALTER TABLE dbo.Customers
ADD CONSTRAINT UQ_Customers_UserId UNIQUE (UserId);
GO

ALTER TABLE dbo.Customers
ADD CONSTRAINT CK_Customers_Gender
CHECK (Gender IS NULL OR Gender IN (N'MALE', N'FEMALE', N'OTHER'));
GO

ALTER TABLE dbo.Customers
ADD CONSTRAINT CK_Customers_Height
CHECK (Height IS NULL OR Height > 0);
GO

ALTER TABLE dbo.Customers
ADD CONSTRAINT CK_Customers_Weight
CHECK (Weight IS NULL OR Weight > 0);
GO

ALTER TABLE dbo.Customers
ADD CONSTRAINT CK_Customers_ExperienceLevel
CHECK (ExperienceLevel IS NULL OR ExperienceLevel IN (N'BEGINNER', N'INTERMEDIATE', N'ADVANCED'));
GO

CREATE UNIQUE INDEX UX_Customers_Phone
ON dbo.Customers(Phone)
WHERE Phone IS NOT NULL;
GO

ALTER TABLE dbo.Employees
ADD CONSTRAINT UQ_Employees_UserId UNIQUE (UserId);
GO

ALTER TABLE dbo.Employees
ADD CONSTRAINT CK_Employees_Gender
CHECK (Gender IS NULL OR Gender IN (N'MALE', N'FEMALE', N'OTHER'));
GO

CREATE UNIQUE INDEX UX_Employees_Phone
ON dbo.Employees(Phone)
WHERE Phone IS NOT NULL;
GO

ALTER TABLE dbo.PTs
ADD CONSTRAINT CK_PTs_Experience
CHECK (Experience IS NULL OR Experience >= 0);
GO

ALTER TABLE dbo.PTs
ADD CONSTRAINT CK_PTs_Status
CHECK (Status IN (N'ACTIVE', N'INACTIVE'));
GO

CREATE UNIQUE INDEX UX_PTs_Email
ON dbo.PTs(Email)
WHERE Email IS NOT NULL;
GO

CREATE UNIQUE INDEX UX_PTs_Phone
ON dbo.PTs(Phone)
WHERE Phone IS NOT NULL;
GO

ALTER TABLE dbo.Packages
ADD CONSTRAINT CK_Packages_Price CHECK (Price >= 0);
GO

ALTER TABLE dbo.Packages
ADD CONSTRAINT CK_Packages_DurationDays CHECK (DurationDays > 0);
GO

CREATE UNIQUE INDEX UX_Packages_Name ON dbo.Packages(Name);
GO

ALTER TABLE dbo.Services
ADD CONSTRAINT CK_Services_Price CHECK (Price >= 0);
GO
