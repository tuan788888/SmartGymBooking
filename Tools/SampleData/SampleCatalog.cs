using SmartGymBooking.Models;

namespace SmartGymBooking.SampleData;

internal static class SampleCatalog
{
    public const string PaymentPrefix = "SGM26A";
    public const string BookingNote = "Buổi tập theo lộ trình cá nhân";
    public const string ScheduleNote = "Huấn luyện cá nhân theo gói thành viên";
    public static readonly Person[] Members =
    [
        new("Nguyễn Quang Bảo", "quangbao", "0935101001", "1998-02-14", "MALE", 175, 72, "MUSCLE_GAIN", "BEGINNER", "MODERATE", 4),
        new("Trần Ngọc Lan", "ngoclan", "0935101002", "2000-05-22", "FEMALE", 162, 60, "WEIGHT_LOSS", "BEGINNER", "LOW", 3),
        new("Lê Hoàng Phúc", "hoangphuc", "0935101003", "1995-08-09", "MALE", 178, 76, "MUSCLE_GAIN", "INTERMEDIATE", "HIGH", 5),
        new("Phạm Khánh Linh", "khanhlinh", "0935101004", "2001-11-03", "FEMALE", 165, 55, "MAINTAIN", "BEGINNER", "MODERATE", 3),
        new("Đỗ Minh Khang", "minhkhang", "0935101005", "1997-03-18", "MALE", 172, 80, "WEIGHT_LOSS", "INTERMEDIATE", "MODERATE", 4),
        new("Vũ Thanh Thảo", "thanhthao", "0935101006", "1999-07-27", "FEMALE", 160, 53, "MAINTAIN", "BEGINNER", "MODERATE", 3),
        new("Bùi Đức Huy", "duchuy", "0935101007", "1993-12-12", "MALE", 180, 82, "MUSCLE_GAIN", "ADVANCED", "HIGH", 5),
        new("Hồ Ngọc Ánh", "ngocanh", "0935101008", "2002-04-06", "FEMALE", 164, 62, "WEIGHT_LOSS", "BEGINNER", "LOW", 3),
        new("Đặng Quốc Việt", "quocviet", "0935101009", "1996-09-15", "MALE", 174, 70, "MAINTAIN", "INTERMEDIATE", "HIGH", 4),
        new("Dương Hải Yến", "haiyen", "0935101010", "2000-01-30", "FEMALE", 168, 57, "MUSCLE_GAIN", "INTERMEDIATE", "MODERATE", 4)
    ];
    public static readonly Staff[] Employees =
    [
        new("Nguyễn Hồng Nhung", "hongnhung", "0935201001", "1997-01-12", "FEMALE", "Lễ tân"),
        new("Trần Quốc Đạt", "quocdat", "0935201002", "1995-06-08", "MALE", "Nhân viên vận hành"),
        new("Lê Mỹ Duyên", "myduyen", "0935201003", "1999-02-24", "FEMALE", "Tư vấn hội viên"),
        new("Phạm Anh Kiệt", "anhkiet", "0935201004", "1994-10-16", "MALE", "Quản lý ca"),
        new("Võ Thanh Trúc", "thanhtruc", "0935201005", "2001-04-19", "FEMALE", "Chăm sóc khách hàng"),
        new("Đỗ Gia Hưng", "giahung", "0935201006", "1996-08-03", "MALE", "Nhân viên thiết bị"),
        new("Bùi Phương Nhi", "phuongnhi", "0935201007", "2000-12-07", "FEMALE", "Lễ tân"),
        new("Hồ Nhật Minh", "nhatminh", "0935201008", "1998-05-28", "MALE", "Nhân viên vận hành"),
        new("Đặng Thùy Chi", "thuychi", "0935201009", "1997-09-11", "FEMALE", "Tư vấn hội viên"),
        new("Dương Thành Long", "thanhlong", "0935201010", "1992-11-20", "MALE", "Điều phối lịch tập")
    ];
    public static readonly Trainer[] Trainers =
    [
        new("Nguyễn Thành Vinh", "thanhvinh", "0935301001", "Sức mạnh và tăng cơ", 6),
        new("Trần Bảo Ngọc", "baongoc", "0935301002", "Pilates và kiểm soát cơ thể", 5),
        new("Lê Quốc Bảo", "quocbao", "0935301003", "Functional Training", 7),
        new("Phạm Thanh Vân", "thanhvan", "0935301004", "Yoga và Mobility", 4),
        new("Võ Minh Triết", "minhtriet", "0935301005", "Boxing và thể lực", 8),
        new("Đỗ Ngọc Hân", "ngochan", "0935301006", "Giảm mỡ và Cardio", 5),
        new("Bùi Hoàng Sơn", "hoangson", "0935301007", "Tập sức mạnh cho người mới", 4),
        new("Hồ Mai Phương", "maiphuong", "0935301008", "Zumba và vận động nhóm", 6),
        new("Đặng Anh Dũng", "anhdung", "0935301009", "TRX và Core Training", 7),
        new("Dương Hạnh Nguyên", "hanhnguyen", "0935301010", "Linh hoạt và phục hồi vận động", 5)
    ];
    public static readonly string[] OriginalPackageNames =
    [
        "Basic 1 tháng", "Standard 3 tháng", "Premium PT 1 tháng", "Premium PT 3 tháng"
    ];
    public static readonly string[] MemberPackageNames =
    [
        "Basic 1 tháng", "Basic 1 tháng", "Standard 3 tháng", "Standard 3 tháng", "Standard 3 tháng",
        "Premium PT 1 tháng", "Premium PT 3 tháng", "Premium PT 3 tháng", "Premium PT 3 tháng", "Premium PT 3 tháng"
    ];
    public static readonly string[] RetiredSamplePackageNames =
    [
        "Khởi động 30 ngày", "Năng động 45 ngày", "Cân bằng 60 ngày", "Bền bỉ 120 ngày", "Toàn diện 180 ngày",
        "Đồng hành PT 30 ngày", "Sức mạnh PT 60 ngày", "Thể lực PT 90 ngày", "Chuyên sâu PT 180 ngày", "Đỉnh cao PT 365 ngày"
    ];
    public static Service[] Services(DateTime created) =>
    [
        Service("Functional Training", "Tập luyện vận động toàn thân.", 100000, 60, created),
        Service("Pilates", "Lớp Pilates tập trung kiểm soát chuyển động.", 150000, 60, created),
        Service("HIIT", "Buổi tập ngắt quãng theo hướng dẫn.", 100000, 45, created),
        Service("Zumba", "Vận động nhóm kết hợp âm nhạc.", 80000, 60, created),
        Service("Cycling", "Đạp xe trong nhà theo nhóm.", 90000, 45, created),
        Service("Body Combat", "Lớp vận động kết hợp các động tác võ thuật.", 120000, 60, created),
        Service("Core Training", "Lớp luyện tập nhóm cơ trung tâm.", 70000, 45, created),
        Service("TRX", "Tập luyện với dây kháng lực treo.", 120000, 60, created),
        Service("Mobility", "Buổi luyện tập biên độ và kiểm soát vận động.", 70000, 45, created),
        Service("Circuit Training", "Tập luyện luân phiên theo các trạm.", 100000, 60, created)
    ];
    private static Service Service(string name, string description, decimal price, int minutes, DateTime created) => new()
    { Name = name, Description = description, Price = price, DurationMinutes = minutes, IsActive = true, CreatedAt = created };
    public static Exercise[] Exercises(DateTime created) =>
    [
        Exercise("Goblet Squat", "LEGS", "BEGINNER", "Squat với một tạ đơn phía trước ngực.", created),
        Exercise("Glute Bridge", "LEGS", "BEGINNER", "Nâng hông từ tư thế nằm ngửa.", created),
        Exercise("Bird Dog", "CORE", "BEGINNER", "Duỗi tay và chân đối diện từ tư thế chống bốn điểm.", created),
        Exercise("Dead Bug", "CORE", "BEGINNER", "Luân phiên duỗi tay chân trong tư thế nằm ngửa.", created),
        Exercise("Face Pull", "SHOULDERS", "BEGINNER", "Kéo dây cáp về phía mặt với mức tải phù hợp.", created),
        Exercise("Dumbbell Floor Press", "CHEST", "BEGINNER", "Đẩy tạ đơn khi nằm trên sàn.", created),
        Exercise("Romanian Deadlift", "LEGS", "INTERMEDIATE", "Gập hông và hạ tạ có kiểm soát.", created),
        Exercise("Single Arm Dumbbell Row", "BACK", "INTERMEDIATE", "Kéo tạ đơn từng tay với điểm tựa.", created),
        Exercise("Farmer Carry", "FULL BODY", "INTERMEDIATE", "Đi bộ trong khi giữ tạ ở hai bên người.", created),
        Exercise("Bulgarian Split Squat", "LEGS", "ADVANCED", "Squat một chân với chân sau đặt trên bục.", created)
    ];
    private static Exercise Exercise(string name, string group, string level, string description, DateTime created) => new()
    { Name = name, MuscleGroup = group, Difficulty = level, Description = description, Instructions = description, VideoUrl = SmartGymBooking.Services.ExerciseGuide.Resolve(name, null), IsActive = true, CreatedAt = created };
    public static readonly Staff[] AdditionalEmployees =
    [
        new("Lê Hà My", "hamy", "0935202001", "1998-01-12", "FEMALE", "Lễ tân"),
        new("Ngô Anh Khoa", "anhkhoa", "0935202002", "1997-02-14", "MALE", "Vận hành"),
        new("Phan Bích Trâm", "bichtram", "0935202003", "1999-03-18", "FEMALE", "Tư vấn hội viên"),
        new("Trịnh Đức Toàn", "ductoan", "0935202004", "1995-04-20", "MALE", "Quản lý ca"),
        new("Mai Tú Uyên", "tuuyen", "0935202005", "1998-05-22", "FEMALE", "Chăm sóc khách hàng"),
        new("Lý Quốc Thịnh", "quocthinh", "0935202006", "1996-06-24", "MALE", "Nhân viên thiết bị"),
        new("Vương Khả Hân", "khahan", "0935202007", "2000-07-08", "FEMALE", "Lễ tân"),
        new("Võ Trọng Nhân", "trongnhan", "0935202008", "1997-08-10", "MALE", "Vận hành"),
        new("Lâm Ngọc Diệp", "ngocdiep", "0935202009", "1999-09-12", "FEMALE", "Điều phối lịch tập"),
        new("Đinh Minh Nhật", "minhnhat", "0935202010", "1996-10-16", "MALE", "Tư vấn hội viên")
    ];

    public static readonly Trainer[] AdditionalTrainers =
    [
        new("Ngô Minh Tâm", "minhtam", "0935302001", "Sức mạnh thân trên", 5),
        new("Trịnh Hoài An", "hoaian", "0935302002", "Tập luyện thân dưới", 6),
        new("Lý Thanh Tùng", "thanhtung", "0935302003", "Điền kinh và sức bền", 7),
        new("Đinh Phương Thảo", "phuongthao", "0935302004", "Pilates thảm", 4),
        new("Phan Quang Hào", "quanghao", "0935302005", "Kỹ thuật tạ đòn", 8),
        new("Mai Cẩm Tú", "camtu", "0935302006", "Yoga thăng bằng", 5),
        new("Vương Gia Bảo", "giabao", "0935302007", "Tập với dây kháng lực", 4),
        new("Võ Hải Anh", "haianh", "0935302008", "Thể lực toàn thân", 6),
        new("Lâm Hà Linh", "halinh", "0935302009", "Vận động cho người mới", 4),
        new("Lê Đức Phát", "ducphat", "0935302010", "Kỹ thuật kéo và đẩy", 7)
    ];

    public static Service[] AdditionalServices(DateTime created) =>
    [
        Service("Strength Basics", "Làm quen với các động tác sức mạnh cơ bản.", 100000, 60, created),
        Service("Mat Pilates", "Tập Pilates trên thảm theo nhóm.", 110000, 50, created),
        Service("Hatha Yoga", "Luyện tư thế và nhịp thở trong lớp Hatha Yoga.", 100000, 60, created),
        Service("Low Impact Aerobics", "Vận động nhịp điệu với các bước ít va chạm.", 80000, 45, created),
        Service("Kettlebell Fundamentals", "Tập kỹ thuật cơ bản với tạ ấm.", 120000, 60, created),
        Service("Resistance Band Training", "Tập sức mạnh với dây kháng lực.", 85000, 45, created),
        Service("Indoor Rowing", "Tập kỹ thuật và sức bền với máy chèo.", 90000, 45, created),
        Service("Step Aerobics", "Tập nhịp điệu trên bục thấp.", 85000, 50, created),
        Service("Balance Training", "Luyện thăng bằng và kiểm soát tư thế.", 70000, 45, created),
        Service("Full Body Dumbbell", "Tập sức mạnh toàn thân với tạ đơn.", 110000, 60, created)
    ];

    public static Exercise[] AdditionalExercises(DateTime created) =>
    [
        Exercise("Push-Up", "CHEST", "BEGINNER", "Hạ ngực có kiểm soát, giữ thân người thẳng, rồi đẩy lên.", created),
        Exercise("Close Grip Lat Pulldown", "BACK", "BEGINNER", "Kéo tay cầm hẹp về trước ngực rồi duỗi tay có kiểm soát.", created),
        Exercise("Resistance Band Row", "BACK", "BEGINNER", "Giữ thân ổn định và kéo hai đầu dây về phía bụng.", created),
        Exercise("Dumbbell Lateral Raise", "SHOULDERS", "BEGINNER", "Nâng tạ sang hai bên đến gần ngang vai.", created),
        Exercise("Dumbbell Bicep Curl", "ARMS", "BEGINNER", "Gập khuỷu tay để nâng tạ, giữ cánh tay trên ổn định.", created),
        Exercise("Cable Triceps Pushdown", "ARMS", "BEGINNER", "Giữ khuỷu sát thân và duỗi tay xuống.", created),
        Exercise("Dumbbell Step-Up", "LEGS", "BEGINNER", "Bước lên bục bằng một chân rồi hạ xuống có kiểm soát.", created),
        Exercise("Reverse Lunge", "LEGS", "INTERMEDIATE", "Bước lùi một chân, hạ người rồi đứng lên bằng chân trước.", created),
        Exercise("Pallof Press", "CORE", "INTERMEDIATE", "Đẩy tay trước ngực và giữ thân người không xoay theo dây.", created),
        Exercise("Dumbbell Thruster", "FULL BODY", "ADVANCED", "Đứng lên từ tư thế squat rồi đẩy tạ qua đầu trong một chuyển động.", created)
    ];

    public static string MemberEmail(Person person) => person.Handle + "@members.smartgym.example";
    public static string EmployeeEmail(Staff person) => person.Handle + "@staff.smartgym.example";
    public static string TrainerEmail(Trainer person) => person.Handle + "@trainers.smartgym.example";
}

internal record Person(string Name, string Handle, string Phone, string Birth, string Gender, decimal Height, decimal Weight, string Goal, string Level, string Activity, int Sessions);
internal record Staff(string Name, string Handle, string Phone, string Birth, string Gender, string Position);
internal record Trainer(string Name, string Handle, string Phone, string Specialization, int Experience);
