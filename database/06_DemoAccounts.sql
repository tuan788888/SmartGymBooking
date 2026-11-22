USE SmartGymBookingDB;
SET XACT_ABORT ON;
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now datetime2 = SYSDATETIME();
    DECLARE @PasswordHash nvarchar(500) = N'AQAAAAIAAYagAAAAEBjJ/voEHoqHb128CnyMKWKvd9+0YE2U57M0rctpiyzdcqg3WGWvX3mc39N5BZw5fw==';
    DECLARE @AdminEmail nvarchar(255) = N'tle263036@gmail.com';
    DECLARE @EmployeeEmail nvarchar(255) = N'tle263037@gmail.com';
    DECLARE @CustomerEmail nvarchar(255) = N'tle263038@gmail.com';

    IF EXISTS (
        SELECT 1 FROM dbo.Users
        WHERE (Email = @AdminEmail AND Role <> N'ADMIN')
           OR (Email = @EmployeeEmail AND Role <> N'EMPLOYEE')
           OR (Email = @CustomerEmail AND Role <> N'CUSTOMER')
    )
        THROW 50001, N'An account already exists with a different role.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @AdminEmail)
        UPDATE dbo.Users SET Email = @AdminEmail
        WHERE Email = N'admin@smartgym.vn' AND Role = N'ADMIN';

    UPDATE dbo.Users
    SET PasswordHash = @PasswordHash, IsActive = 1, UpdatedAt = @Now
    WHERE Email IN (@AdminEmail, @EmployeeEmail, @CustomerEmail)
      AND (PasswordHash <> @PasswordHash OR IsActive = 0);

    INSERT INTO dbo.Users (Email, PasswordHash, Role, IsActive)
    SELECT Account.Email, @PasswordHash, Account.Role, 1
    FROM (VALUES (@AdminEmail, N'ADMIN'), (@EmployeeEmail, N'EMPLOYEE'),
                 (@CustomerEmail, N'CUSTOMER')) AS Account(Email, Role)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = Account.Email);

    DECLARE @EmployeeUserId bigint = (SELECT UserId FROM dbo.Users WHERE Email = @EmployeeEmail);
    DECLARE @CustomerUserId bigint = (SELECT UserId FROM dbo.Users WHERE Email = @CustomerEmail);

    IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE UserId = @EmployeeUserId)
        INSERT INTO dbo.Employees (UserId, FullName, DateOfBirth, Gender, Position, IsActive)
        VALUES (@EmployeeUserId, N'Nhân viên SmartGym', '2000-04-10', N'MALE', N'Lễ tân', 1);

    UPDATE dbo.Employees SET IsActive = 1, UpdatedAt = @Now
    WHERE UserId = @EmployeeUserId AND IsActive = 0;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE UserId = @CustomerUserId)
        INSERT INTO dbo.Customers (UserId, FullName, DateOfBirth, Gender, Height, Weight,
            FitnessGoal, ExperienceLevel, ActivityLevel, PreferredSessionsPerWeek, PreferredSessionMinutes)
        VALUES (@CustomerUserId, N'Khách hàng SmartGym', '2002-05-15', N'MALE', 175, 70,
            N'HYPERTROPHY', N'BEGINNER', N'MODERATE', 4, 60);

    DECLARE @CustomerId bigint = (SELECT CustomerId FROM dbo.Customers WHERE UserId = @CustomerUserId);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Memberships AS m
        JOIN dbo.Packages AS p ON p.PackageId = m.PackageId
        WHERE m.CustomerId = @CustomerId AND m.Status = N'ACTIVE' AND p.IncludesPT = 1
          AND m.StartDate <= @Now AND m.EndDate > @Now
    )
    BEGIN
        DECLARE @PackageId bigint, @Price decimal(18,2), @DurationDays int;
        SELECT TOP (1) @PackageId = PackageId, @Price = Price, @DurationDays = DurationDays
        FROM dbo.Packages
        WHERE IncludesPT = 1 AND IsActive = 1
        ORDER BY CASE WHEN DurationDays = 30 THEN 0 ELSE 1 END, DurationDays, PackageId;

        IF @PackageId IS NULL
            THROW 50002, N'No active package with a personal trainer exists.', 1;

        DECLARE @EndDate datetime2 = DATEADD(day, @DurationDays, @Now);
        IF EXISTS (
            SELECT 1 FROM dbo.Memberships
            WHERE CustomerId = @CustomerId AND Status = N'ACTIVE'
              AND StartDate < @EndDate AND EndDate > @Now
        )
            THROW 50003, N'The demo customer already has an overlapping membership.', 1;

        INSERT INTO dbo.Payments (CustomerId, PackageId, Amount, PaymentCode, PaymentMethod,
            Status, CreatedAt, PaidAt, ConfirmedAt)
        VALUES (@CustomerId, @PackageId, @Price, N'SGDEMO-' + CONVERT(nvarchar(36), NEWID()),
            N'QR_BANK_TRANSFER', N'PAID', @Now, @Now, @Now);

        INSERT INTO dbo.Memberships (CustomerId, PackageId, PaymentId, StartDate, EndDate, Status)
        VALUES (@CustomerId, @PackageId, CONVERT(bigint, SCOPE_IDENTITY()), @Now, @EndDate, N'ACTIVE');
    END;

    COMMIT TRANSACTION;

    SELECT u.Email, u.Role, u.IsActive, e.FullName AS Employee, c.FullName AS Customer,
           p.Name AS PTPackage, m.EndDate AS MembershipEndDate
    FROM dbo.Users AS u
    LEFT JOIN dbo.Employees AS e ON e.UserId = u.UserId
    LEFT JOIN dbo.Customers AS c ON c.UserId = u.UserId
    OUTER APPLY (
        SELECT TOP (1) m.PackageId, m.EndDate
        FROM dbo.Memberships AS m
        JOIN dbo.Packages AS p ON p.PackageId = m.PackageId
        WHERE m.CustomerId = c.CustomerId AND m.Status = N'ACTIVE' AND p.IncludesPT = 1
          AND m.StartDate <= @Now AND m.EndDate > @Now
        ORDER BY m.EndDate DESC
    ) AS m
    LEFT JOIN dbo.Packages AS p ON p.PackageId = m.PackageId
    WHERE u.Email IN (@AdminEmail, @EmployeeEmail, @CustomerEmail)
    ORDER BY u.Email;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
