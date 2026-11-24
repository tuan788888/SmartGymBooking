
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
(N'Nguyễn Đức Anh',N'0912000001',N'ducanh.pt@smartgym.vn',N'Gym – Sức mạnh và tăng cơ',5,N'Hướng dẫn tăng cơ và tập luyện sức mạnh.',N'ACTIVE'),
(N'Lê Thu Hà',N'0912000003',N'thuha.pt@smartgym.vn',N'Yoga – Linh hoạt',6,N'Hướng dẫn Yoga và cải thiện độ linh hoạt.',N'ACTIVE'),
(N'Phạm Quốc Huy',N'0912000004',N'quochuy.pt@smartgym.vn',N'Kickbox – Thể lực',7,N'Hướng dẫn Kickbox và rèn luyện thể lực.',N'ACTIVE'),
(N'Trần Bảo Ngọc',NULL,NULL,N'Pilates – Kiểm soát cơ thể',NULL,N'Hướng dẫn Pilates và kiểm soát chuyển động.',N'ACTIVE'),
(N'Phạm Thanh Vân',NULL,NULL,N'Yoga – Linh hoạt',NULL,N'Hướng dẫn Yoga và cải thiện độ linh hoạt.',N'ACTIVE'),
(N'Đỗ Ngọc Hân',NULL,NULL,N'Gym – Thể lực và giảm mỡ',NULL,N'Hướng dẫn tập gym và cải thiện thể lực.',N'ACTIVE'),
(N'Đinh Phương Thảo',NULL,NULL,N'Pilates – Pilates thảm',NULL,N'Hướng dẫn Pilates trên thảm.',N'ACTIVE');
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
(N'Kickbox',N'Tập Kickbox và cải thiện thể lực.',150000,60,1),
(N'Pilates',N'Pilates cải thiện sức mạnh cơ trung tâm và kiểm soát chuyển động.',150000,60,1);
GO

INSERT INTO dbo.Exercises
(Name,MuscleGroup,Difficulty,Description,Instructions,VideoUrl,IsActive)
VALUES
(N'Bench Press',N'CHEST',N'BEGINNER',N'Bài tập ngực với thanh đòn.',N'Hạ thanh đòn có kiểm soát và đẩy lên.',NULL,1),
(N'Incline Dumbbell Press',N'CHEST',N'BEGINNER',N'Bài tập ngực trên.',N'Đẩy hai quả tạ lên trên.',NULL,1),
(N'Cable Fly',N'CHEST',N'INTERMEDIATE',N'Bài tập cô lập cơ ngực.',N'Khép hai tay về phía trước.',NULL,1),
