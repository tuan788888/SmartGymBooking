-- ============================================================
-- SMARTGYM BOOKING
-- 05_SeedData.sql
-- Seed data including EMPLOYEE role
-- ============================================================

USE SmartGymBookingDB;
GO

INSERT INTO dbo.Users (Email, PasswordHash, Role, IsActive)
VALUES
(N'admin@smartgym.vn', N'TEMP_ADMIN_PASSWORD', N'ADMIN', 1),
(N'employee01@smartgym.vn', N'TEMP_PASSWORD', N'EMPLOYEE', 1),
(N'employee02@smartgym.vn', N'TEMP_PASSWORD', N'EMPLOYEE', 1),
(N'nguyenvana@gmail.com', N'TEMP_PASSWORD', N'CUSTOMER', 1),
(N'tranthib@gmail.com', N'TEMP_PASSWORD', N'CUSTOMER', 1),
(N'leminhc@gmail.com', N'TEMP_PASSWORD', N'CUSTOMER', 1),
(N'phamthud@gmail.com', N'TEMP_PASSWORD', N'CUSTOMER', 1),
(N'hoangnam@gmail.com', N'TEMP_PASSWORD', N'CUSTOMER', 1);
GO

INSERT INTO dbo.Customers
(
    UserId, FullName, Phone, DateOfBirth, Gender, Height, Weight,
    FitnessGoal, ExperienceLevel, ActivityLevel,
    PreferredSessionsPerWeek, PreferredSessionMinutes
)
VALUES
((SELECT UserId FROM dbo.Users WHERE Email=N'nguyenvana@gmail.com'),
 N'Nguyễn Văn An',N'0901000001','2003-05-15',N'MALE',175,70,
 N'MUSCLE_GAIN',N'BEGINNER',N'MODERATE',4,60),

((SELECT UserId FROM dbo.Users WHERE Email=N'tranthib@gmail.com'),
 N'Trần Thị Bình',N'0901000002','2002-08-20',N'FEMALE',160,52,
 N'WEIGHT_LOSS',N'BEGINNER',N'LOW',3,60),

((SELECT UserId FROM dbo.Users WHERE Email=N'leminhc@gmail.com'),
 N'Lê Minh Cường',N'0901000003','2000-03-10',N'MALE',178,78,
 N'MUSCLE_GAIN',N'INTERMEDIATE',N'HIGH',5,75),

((SELECT UserId FROM dbo.Users WHERE Email=N'phamthud@gmail.com'),
 N'Phạm Thu Dung',N'0901000004','2001-11-25',N'FEMALE',165,58,
 N'MAINTAIN',N'INTERMEDIATE',N'MODERATE',4,60),

((SELECT UserId FROM dbo.Users WHERE Email=N'hoangnam@gmail.com'),
 N'Hoàng Nam',N'0901000005','1999-06-18',N'MALE',172,85,
 N'WEIGHT_LOSS',N'BEGINNER',N'LOW',3,45);
GO

INSERT INTO dbo.Employees
(
    UserId, FullName, Phone, DateOfBirth, Gender, Position, IsActive
)
VALUES
((SELECT UserId FROM dbo.Users WHERE Email=N'employee01@smartgym.vn'),
 N'Nguyễn Minh Tuấn',N'0922000001','1998-04-10',N'MALE',N'Lễ tân',1),

((SELECT UserId FROM dbo.Users WHERE Email=N'employee02@smartgym.vn'),
 N'Trần Ngọc Anh',N'0922000002','1999-09-20',N'FEMALE',N'Nhân viên vận hành',1);
GO

INSERT INTO dbo.PTs
(FullName,Phone,Email,Specialization,Experience,Description,Status)
VALUES
(N'Nguyễn Đức Anh',N'0912000001',N'ducanh.pt@smartgym.vn',N'Tăng cơ - Strength',5,N'Chuyên hướng dẫn tăng cơ và tập luyện sức mạnh.',N'ACTIVE'),
(N'Trần Minh Hoàng',N'0912000002',N'minhhoang.pt@smartgym.vn',N'Giảm cân - Cardio',4,N'Chuyên giảm cân, cardio và cải thiện thể lực.',N'ACTIVE'),
(N'Lê Thu Hà',N'0912000003',N'thuha.pt@smartgym.vn',N'Yoga - Mobility',6,N'Chuyên Yoga, mobility và cải thiện độ linh hoạt.',N'ACTIVE'),
(N'Phạm Quốc Huy',N'0912000004',N'quochuy.pt@smartgym.vn',N'Boxing - Conditioning',7,N'Chuyên Boxing và conditioning.',N'ACTIVE'),
(N'Vũ Ngọc Mai',N'0912000005',N'ngocmai.pt@smartgym.vn',N'Fitness Beginner',3,N'Hướng dẫn người mới bắt đầu tập luyện.',N'ACTIVE');
GO

INSERT INTO dbo.Packages
(Name,Description,Price,DurationDays,IncludesPT,IsActive)
VALUES
(N'Basic 1 tháng',N'Gói tập cơ bản 30 ngày, không bao gồm PT.',500000,30,0,1),
(N'Standard 3 tháng',N'Gói tập 90 ngày, không bao gồm PT.',1200000,90,0,1),
(N'Premium PT 1 tháng',N'Gói 30 ngày có Personal Trainer.',2000000,30,1,1),
(N'Premium PT 3 tháng',N'Gói 90 ngày có Personal Trainer.',5000000,90,1,1);
GO

INSERT INTO dbo.Services
(Name,Description,Price,DurationMinutes,IsActive)
VALUES
(N'Gym',N'Tập luyện gym thông thường.',0,90,1),
(N'Yoga',N'Yoga cải thiện linh hoạt và thăng bằng.',100000,60,1),
(N'Boxing',N'Boxing và cải thiện thể lực.',150000,60,1),
(N'Cardio',N'Cardio cải thiện sức bền.',0,60,1),
(N'Stretching',N'Giãn cơ và mobility.',50000,45,1);
GO

INSERT INTO dbo.Exercises
(Name,MuscleGroup,Difficulty,Description,Instructions,VideoUrl,IsActive)
VALUES
(N'Bench Press',N'CHEST',N'BEGINNER',N'Bài tập ngực với thanh đòn.',N'Hạ thanh đòn có kiểm soát và đẩy lên.',NULL,1),
(N'Incline Dumbbell Press',N'CHEST',N'BEGINNER',N'Bài tập ngực trên.',N'Đẩy hai quả tạ lên trên.',NULL,1),
(N'Cable Fly',N'CHEST',N'INTERMEDIATE',N'Bài tập cô lập cơ ngực.',N'Khép hai tay về phía trước.',NULL,1),