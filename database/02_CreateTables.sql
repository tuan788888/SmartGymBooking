-- ============================================================
-- SMARTGYM BOOKING
-- 02_CreateTables.sql
-- 16 bảng nghiệp vụ
-- ============================================================

USE SmartGymBookingDB;
GO

CREATE TABLE dbo.Users
(
    UserId BIGINT IDENTITY(1,1) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT PK_Users PRIMARY KEY (UserId)
);
GO

CREATE TABLE dbo.Customers
(
    CustomerId BIGINT IDENTITY(1,1) NOT NULL,
    UserId BIGINT NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NULL,
    DateOfBirth DATE NULL,
    Gender NVARCHAR(20) NULL,
    Height DECIMAL(5,2) NULL,
    Weight DECIMAL(5,2) NULL,
    FitnessGoal NVARCHAR(100) NULL,
    ExperienceLevel NVARCHAR(30) NULL,
    Avatar NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT PK_Customers PRIMARY KEY (CustomerId),
    CONSTRAINT FK_Customers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO

CREATE TABLE dbo.Employees
(
    EmployeeId BIGINT IDENTITY(1,1) NOT NULL,
    UserId BIGINT NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NULL,
    DateOfBirth DATE NULL,
    Gender NVARCHAR(20) NULL,
    Position NVARCHAR(100) NULL,
    Avatar NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT PK_Employees PRIMARY KEY (EmployeeId),
    CONSTRAINT FK_Employees_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO

CREATE TABLE dbo.PTs
(
    PTId BIGINT IDENTITY(1,1) NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(255) NULL,
    Specialization NVARCHAR(200) NULL,
    Experience INT NULL,
    Description NVARCHAR(1000) NULL,
    Avatar NVARCHAR(500) NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_PTs_Status DEFAULT N'ACTIVE',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PTs_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT PK_PTs PRIMARY KEY (PTId)
);
GO

CREATE TABLE dbo.Packages
(
    PackageId BIGINT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NULL,
    Price DECIMAL(18,2) NOT NULL,
    DurationDays INT NOT NULL,
    IncludesPT BIT NOT NULL CONSTRAINT DF_Packages_IncludesPT DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_Packages_IsActive DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Packages_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT PK_Packages PRIMARY KEY (PackageId)
);
GO

CREATE TABLE dbo.Services
(
    ServiceId BIGINT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(150) NOT NULL,