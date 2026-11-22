using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.Services;

namespace SmartGymBooking.SampleData;

internal sealed partial class SampleDataSeeder(ApplicationDbContext db)
{
    private static readonly Dictionary<string, string> Tables = new()
    {
        ["Users"] = "UserId", ["Customers"] = "CustomerId", ["Employees"] = "EmployeeId",
        ["PTs"] = "PTId", ["Packages"] = "PackageId", ["Services"] = "ServiceId", ["Exercises"] = "ExerciseId",
        ["Payments"] = "PaymentId", ["Memberships"] = "MembershipId", ["Bookings"] = "BookingId",
        ["CheckIns"] = "CheckInId", ["PTSchedules"] = "PTScheduleId",
#if AI_FEATURES
        ["WorkoutPlans"] = "WorkoutPlanId",
        ["WorkoutExercises"] = "WorkoutExerciseId", ["AIConversations"] = "AIConversationId", ["AIMessages"] = "AIMessageId"
#endif
    };
    private static string[] MemberEmails => SampleCatalog.Members.Select(SampleCatalog.MemberEmail).ToArray();
    private static string[] StaffEmails => SampleCatalog.Employees.Select(SampleCatalog.EmployeeEmail).ToArray();

    public async Task<object> PreviewAsync()
    {
        var exists = await BatchExistsAsync();
        if (!exists) await CheckCollisionsAsync();
        return new
        {
            Mode = "Preview", AlreadyPresent = exists,
            PlannedRows = new Dictionary<string, int>
            {
                ["Users"] = 20, ["Customers"] = 10, ["Employees"] = 10, ["PTs"] = 10,
                ["Packages"] = 0, ["Services"] = 10, ["Exercises"] = 10,
                ["Payments"] = 15, ["Memberships"] = 10, ["Bookings"] = 15,
                ["CheckIns"] = 10, ["PTSchedules"] = 10,
#if AI_FEATURES
                ["WorkoutPlans"] = 10,
                ["WorkoutExercises"] = SampleCatalog.Members.Sum(p => p.Sessions * 4),
                ["AIConversations"] = 10, ["AIMessages"] = 20
#endif
            },
            Rules = new[]
            {
                "10 paid payments create 10 matching memberships; 5 additional payments cover pending/failed/cancelled states.",
                "Reuse the four original Basic/Standard/Premium packages; do not create packages.",
                "10 completed bookings have attendance; 5 upcoming bookings cover pending/confirmed/cancelled states.",
                "PT sessions use only PT-inclusive memberships, within their validity and without overlapping sessions.",
                "Existing records are preserved. A repeated apply validates the existing batch without inserting duplicates."
            }
        };
    }

    public async Task<object> ApplyAsync()
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource=N'SmartGymBooking.SampleData.V1',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @result < 0 THROW 50001, 'Could not lock sample data batch.', 1;
            """);
        if (await BatchExistsAsync())
        {
            var verification = await VerifyAsync();
            await transaction.CommitAsync();
            return new { Mode = "AlreadyPresent", AddedRows = 0, Verification = verification };
        }
        await CheckCollisionsAsync();
        var before = await SnapshotAsync();
        var now = DateTime.Now;
        var anchor = now.Date;
        var created = anchor.AddDays(-10).AddHours(8);
        var joinedAt = anchor.AddDays(-7).AddHours(8);
        var hasher = new PasswordHasher<User>();
        User NewUser(string email, string role)
        {
            var user = new User { Email = email, Role = role, IsActive = true, CreatedAt = created };
            user.PasswordHash = hasher.HashPassword(user, role == "CUSTOMER" ? "Customer@123" : "Employee@123");
            return user;
        }
        var customers = SampleCatalog.Members.Select(p => new Customer
        {
            User = NewUser(SampleCatalog.MemberEmail(p), "CUSTOMER"), FullName = p.Name, Phone = p.Phone,
            DateOfBirth = DateOnly.ParseExact(p.Birth, "yyyy-MM-dd"), Gender = p.Gender,
            Height = p.Height, Weight = p.Weight, FitnessGoal = p.Goal, ExperienceLevel = p.Level,
            ActivityLevel = p.Activity, PreferredSessionsPerWeek = p.Sessions, PreferredSessionMinutes = 60, CreatedAt = created
        }).ToArray();
        var employees = SampleCatalog.Employees.Select(p => new Employee
        {
            User = NewUser(SampleCatalog.EmployeeEmail(p), "EMPLOYEE"), FullName = p.Name, Phone = p.Phone,
            DateOfBirth = DateOnly.ParseExact(p.Birth, "yyyy-MM-dd"), Gender = p.Gender, Position = p.Position,
            IsActive = true, CreatedAt = created
        }).ToArray();
        var trainers = SampleCatalog.Trainers.Select(p => new Pt
        {
            FullName = p.Name, Email = SampleCatalog.TrainerEmail(p), Phone = p.Phone,
            Specialization = p.Specialization, Experience = p.Experience,
            Description = $"Huấn luyện viên chuyên về {p.Specialization.ToLowerInvariant()}.", Status = "ACTIVE", CreatedAt = created
        }).ToArray();
        var packages = await LoadMemberPackagesAsync();
        var services = SampleCatalog.Services(created);
        var exercises = SampleCatalog.Exercises(created);
        db.Customers.AddRange(customers);
        db.Employees.AddRange(employees);
        db.Pts.AddRange(trainers);
        db.Services.AddRange(services);
        db.Exercises.AddRange(exercises);
        await db.SaveChangesAsync();

        for (var i = 0; i < customers.Length; i++)
        {
            var paidAt = joinedAt.AddMinutes(i);
            var payment = new Payment
            {
                CustomerId = customers[i].CustomerId, PackageId = packages[i].PackageId,
                Amount = packages[i].Price, PaymentCode = $"{SampleCatalog.PaymentPrefix}{i + 1:D3}P",
                PaymentMethod = "QR_BANK_TRANSFER", Status = "PAID", CreatedAt = paidAt.AddMinutes(-30),
                PaidAt = paidAt, ConfirmedAt = paidAt
            };
            db.Memberships.Add(new Membership
            {
                CustomerId = customers[i].CustomerId, PackageId = packages[i].PackageId, Payment = payment,
                StartDate = paidAt, EndDate = paidAt.AddDays(packages[i].DurationDays), Status = "ACTIVE", CreatedAt = paidAt
            });
            if (i < 5)
            {
                db.Payments.Add(new Payment
                {
                    CustomerId = customers[i].CustomerId, PackageId = packages[i + 5].PackageId,
                    Amount = packages[i + 5].Price, PaymentCode = $"{SampleCatalog.PaymentPrefix}{i + 1:D3}U",
                    PaymentMethod = "QR_BANK_TRANSFER", Status = i switch { 3 => "FAILED", 4 => "CANCELLED", _ => "PENDING" },
                    CreatedAt = now.AddHours(-1).AddMinutes(i)
                });
            }
            var start = anchor.AddDays(-(i % 5 + 1)).AddHours(8 + (i / 5) * 2);
            var completed = new Booking
            {
                CustomerId = customers[i].CustomerId, ServiceId = services[i].ServiceId,
                StartTime = start, EndTime = start.AddMinutes(services[i].DurationMinutes!.Value),
                Status = "COMPLETED", Note = SampleCatalog.BookingNote, CreatedAt = joinedAt.AddHours(1),
                UpdatedAt = start.AddMinutes(services[i].DurationMinutes!.Value + 5)
            };
            db.CheckIns.Add(new CheckIn
            {
                CustomerId = customers[i].CustomerId, Booking = completed,
                CheckInTime = start.AddMinutes(5), CreatedAt = start.AddMinutes(5)
            });
            if (i < 5)
            {
                var futureStart = anchor.AddDays(i + 2).AddHours(9);
                db.Bookings.Add(new Booking
                {
                    CustomerId = customers[i].CustomerId, ServiceId = services[i].ServiceId,
                    StartTime = futureStart, EndTime = futureStart.AddMinutes(services[i].DurationMinutes!.Value),
                    Status = i switch { 1 or 3 => "CONFIRMED", 4 => "CANCELLED", _ => "PENDING" },
                    Note = SampleCatalog.BookingNote, CreatedAt = now, UpdatedAt = i is 1 or 3 or 4 ? now : null
                });
            }
        }
        for (var i = 0; i < 10; i++)
        {
            var past = i < 5;
            var start = anchor.AddDays(past ? -(i + 1) : i - 4).AddHours(15);
            db.Ptschedules.Add(new Ptschedule
            {
                Ptid = trainers[i].Ptid, CustomerId = customers[5 + i % 5].CustomerId,
                StartTime = start, EndTime = start.AddHours(1),
                Status = past ? "COMPLETED" : i == 9 ? "CANCELLED" : "SCHEDULED",
                Note = SampleCatalog.ScheduleNote, CreatedAt = past ? joinedAt.AddHours(1) : now,
                UpdatedAt = past ? start.AddHours(1).AddMinutes(5) : i == 9 ? now : null
            });
        }
#if AI_FEATURES
        var demoChat = new DemoAIChatService();
        for (var i = 0; i < customers.Length; i++)
        {
            var customer = customers[i];
            var plan = BuildPlan(customer, exercises, now.AddMinutes(-30));
            db.WorkoutPlans.Add(plan);
            var question = "Xin chào, tôi muốn bắt đầu tập luyện tại SmartGym.";
            var sentAt = now.AddMinutes(-10).AddSeconds(i * 2);
            var conversation = new Aiconversation
            {
                CustomerId = customer.CustomerId, Title = "Bắt đầu cùng SmartGym", CreatedAt = sentAt, UpdatedAt = sentAt.AddSeconds(1)
            };
            conversation.Aimessages.Add(new Aimessage { Role = "USER", Content = question, CreatedAt = sentAt });
            conversation.Aimessages.Add(new Aimessage
            {
                Role = "ASSISTANT", Content = await demoChat.GetResponseAsync(customer, question, [], plan), CreatedAt = sentAt.AddSeconds(1)
            });
            db.Aiconversations.Add(conversation);
        }
#endif
        await db.SaveChangesAsync();
        var verificationResult = await VerifyAsync();
        var after = await SnapshotAsync();
        var addedIds = new Dictionary<string, long[]>();
        foreach (var (name, original) in before)
        {
            var preserved = await ReadTableAsync(name, original.Keys.ToArray());
            if (!original.OrderBy(p => p.Key).SequenceEqual(preserved.OrderBy(p => p.Key)))
                throw new InvalidOperationException($"An existing record changed in {name}; all sample inserts will roll back.");
            addedIds[name] = after[name].Keys.Except(original.Keys).Order().ToArray();
        }
        await transaction.CommitAsync();
        return new
        {
            Mode = "Applied", AnchorDate = anchor.ToString("yyyy-MM-dd"),
            AddedRows = addedIds.ToDictionary(p => p.Key, p => p.Value.Length),
            TotalAdded = addedIds.Sum(p => p.Value.Length), ExistingRows = "Verified unchanged",
            RecordIds = addedIds, Verification = verificationResult
        };
    }

#if AI_FEATURES
    private static WorkoutPlan BuildPlan(Customer customer, Exercise[] exercises, DateTime created)
    {
        var plan = new WorkoutPlan
        {
            CustomerId = customer.CustomerId, Title = $"Lộ trình tập luyện của {customer.FullName}", Goal = customer.FitnessGoal,
            ExperienceLevel = customer.ExperienceLevel, SessionsPerWeek = customer.PreferredSessionsPerWeek,
            DurationWeeks = 4, CreatedByAi = false, CreatedAt = created
        };
        var levels = new Dictionary<string, int> { ["BEGINNER"] = 0, ["INTERMEDIATE"] = 1, ["ADVANCED"] = 2 };
        var suitable = exercises.Where(e => levels[e.Difficulty!] <= levels[customer.ExperienceLevel!]).ToArray();
        for (var day = 1; day <= plan.SessionsPerWeek; day++)
            for (var order = 1; order <= 4; order++)
                plan.WorkoutExercises.Add(new WorkoutExercise
                {
                    Exercise = suitable[((day - 1) * 2 + order - 1) % suitable.Length], DayNumber = day, OrderNumber = order,
                    Sets = customer.ExperienceLevel == "BEGINNER" ? 3 : 4,
                    Reps = customer.FitnessGoal == "MUSCLE_GAIN" ? 10 : 12, RestSeconds = 60
                });
        return plan;
    }
#endif

    private Task<bool> BatchExistsAsync() => db.Payments.AnyAsync(p => p.PaymentCode.StartsWith(SampleCatalog.PaymentPrefix));

    private async Task<Package[]> LoadMemberPackagesAsync()
    {
        var names = SampleCatalog.OriginalPackageNames;
        var originals = await db.Packages.Where(p => names.Contains(p.Name)).ToListAsync();
        if (originals.Count != 4 || originals.Any(p => !p.IsActive || p.Price <= 0 || p.DurationDays < 30))
            throw new InvalidOperationException("The four active original packages are required; sample data will not create or modify packages.");
        var byName = originals.ToDictionary(p => p.Name, StringComparer.Ordinal);
        var packages = SampleCatalog.MemberPackageNames.Select(name => byName[name]).ToArray();
        if (packages.Where((p, i) => p.IncludesPt != (i >= 5)).Any())
            throw new InvalidOperationException("Original packages do not match the required PT eligibility.");
        return packages;
    }

    public async Task<object> RestoreOriginalPackagesAsync(string backupDirectory)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource=N'SmartGymBooking.SampleData.V1',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @result < 0 THROW 50001, 'Could not lock sample data batch.', 1;
            """);
        var packages = await LoadMemberPackagesAsync();
        var legacyNames = SampleCatalog.RetiredSamplePackageNames;
        var retired = await db.Packages.Where(p => legacyNames.Contains(p.Name)).ToListAsync();
        if (retired.Count == 0)
        {
            var existingVerification = await VerifyAsync();
            await transaction.CommitAsync();
            return new { Mode = "OriginalPackagesAlreadyRestored", ChangedRows = 0, Verification = existingVerification };
        }
        var retiredIds = retired.Select(p => p.PackageId).ToArray();
        var payments = await db.Payments.Where(p => p.PaymentCode.StartsWith(SampleCatalog.PaymentPrefix)).ToListAsync();
        var paymentIds = payments.Select(p => p.PaymentId).ToArray();
        var memberships = await db.Memberships.Where(m => paymentIds.Contains(m.PaymentId)).ToListAsync();
        if (await db.Payments.AnyAsync(p => retiredIds.Contains(p.PackageId) && !paymentIds.Contains(p.PaymentId)) ||
            await db.Memberships.AnyAsync(m => retiredIds.Contains(m.PackageId) && !paymentIds.Contains(m.PaymentId)))
            throw new InvalidOperationException("An added package is used by a record outside the sample batch. No records were changed.");
        var customers = await db.Customers.Where(c => MemberEmails.Contains(c.User.Email))
            .Select(c => new { c.CustomerId, c.User.Email }).ToListAsync();
        var customerByEmail = customers.ToDictionary(c => c.Email, c => c.CustomerId);
        var expected = new Dictionary<string, (long CustomerId, Package Package)>();
        for (var i = 0; i < 10; i++)
        {
            var customerId = customerByEmail[SampleCatalog.MemberEmail(SampleCatalog.Members[i])];
            expected[$"{SampleCatalog.PaymentPrefix}{i + 1:D3}P"] = (customerId, packages[i]);
            if (i < 5) expected[$"{SampleCatalog.PaymentPrefix}{i + 1:D3}U"] = (customerId, packages[i + 5]);
        }
        if (payments.Count != 15 || payments.Any(p => !expected.TryGetValue(p.PaymentCode, out var target) || target.CustomerId != p.CustomerId))
            throw new InvalidOperationException("Sample payment identities differ from the expected batch. No records were changed.");

        var before = await SnapshotAsync();
        var backup = new
        {
            Database = db.Database.GetDbConnection().Database, CreatedAt = DateTime.Now,
            Packages = retired.Select(p => new { p.PackageId, p.Name, p.Description, p.Price, p.DurationDays, p.IncludesPt, p.IsActive, p.CreatedAt, p.UpdatedAt }),
            Payments = payments.Select(p => new { p.PaymentId, p.CustomerId, p.PackageId, p.Amount, p.PaymentCode, p.PaymentMethod, p.Status, p.CreatedAt, p.PaidAt, p.ConfirmedAt }),
            Memberships = memberships.Select(m => new { m.MembershipId, m.CustomerId, m.PackageId, m.PaymentId, m.StartDate, m.EndDate, m.Status, m.CreatedAt, m.UpdatedAt })
        };
        Directory.CreateDirectory(backupDirectory);
        var backupPath = Path.Combine(backupDirectory, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".json");
        await File.WriteAllTextAsync(backupPath, JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true }));
        var targetsByPaymentId = payments.ToDictionary(p => p.PaymentId, p => expected[p.PaymentCode].Package);
        foreach (var payment in payments)
        {
            var target = targetsByPaymentId[payment.PaymentId];
            payment.Package = target;
            payment.PackageId = target.PackageId;
            payment.Amount = target.Price;
        }
        foreach (var membership in memberships)
        {
            var target = targetsByPaymentId[membership.PaymentId];
            membership.Package = target;
            membership.PackageId = target.PackageId;
            membership.EndDate = membership.StartDate.AddDays(target.DurationDays);
            membership.UpdatedAt = DateTime.Now;
        }
        await db.SaveChangesAsync();
        db.Packages.RemoveRange(retired);
        await db.SaveChangesAsync();
        var verification = await VerifyAsync();
        var authorizedChanges = new Dictionary<string, HashSet<long>>
        {
            ["Packages"] = retiredIds.ToHashSet(), ["Payments"] = paymentIds.ToHashSet(),
            ["Memberships"] = memberships.Select(m => m.MembershipId).ToHashSet()
        };
        foreach (var (name, rows) in before)
        {
            var preserved = rows.Where(p => !authorizedChanges.TryGetValue(name, out var ids) || !ids.Contains(p.Key)).ToDictionary();
            var actual = await ReadTableAsync(name, preserved.Keys.ToArray());
            if (!preserved.OrderBy(p => p.Key).SequenceEqual(actual.OrderBy(p => p.Key)))
                throw new InvalidOperationException($"A record outside the package restoration changed in {name}; all changes will roll back.");
        }
        await transaction.CommitAsync();
        return new
        {
            Mode = "OriginalPackagesRestored", RemovedPackages = retired.Count, RemappedPayments = payments.Count,
            UpdatedMemberships = memberships.Count, OriginalPackages = SampleCatalog.OriginalPackageNames,
            OriginalPackageValues = "Verified unchanged", OtherRows = "Verified unchanged", Backup = backupPath, Verification = verification
        };
    }

    private async Task CheckCollisionsAsync()
    {
        var emails = MemberEmails.Concat(StaffEmails).ToArray();
        var phones = SampleCatalog.Members.Select(p => p.Phone).Concat(SampleCatalog.Employees.Select(p => p.Phone)).ToArray();
        var trainerEmails = SampleCatalog.Trainers.Select(SampleCatalog.TrainerEmail).ToArray();
        var trainerPhones = SampleCatalog.Trainers.Select(p => p.Phone).ToArray();
        var serviceNames = SampleCatalog.Services(DateTime.Today).Select(p => p.Name).ToArray();
        var exerciseNames = SampleCatalog.Exercises(DateTime.Today).Select(p => p.Name).ToArray();
        if (await db.Users.AnyAsync(u => emails.Contains(u.Email)) ||
            await db.Customers.AnyAsync(c => phones.Contains(c.Phone!)) || await db.Employees.AnyAsync(e => phones.Contains(e.Phone!)) ||
            await db.Pts.AnyAsync(p => trainerEmails.Contains(p.Email!) || trainerPhones.Contains(p.Phone!)) ||
            await db.Services.AnyAsync(s => serviceNames.Contains(s.Name)) || await db.Exercises.AnyAsync(e => exerciseNames.Contains(e.Name)))
            throw new InvalidOperationException("Some sample identities already exist without a complete batch. No records were changed.");
        await LoadMemberPackagesAsync();
    }

    public async Task<object> VerifyAsync()
    {
        var checks = new List<string>();
        void Require(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("Sample data validation failed: " + description);
            checks.Add(description);
        }
        var memberEmails = MemberEmails;
        var staffEmails = StaffEmails;
        var customers = await db.Customers.AsNoTracking().Include(c => c.User).Where(c => memberEmails.Contains(c.User.Email)).ToListAsync();
        var customerIds = customers.Select(c => c.CustomerId).ToArray();
        var employees = await db.Employees.AsNoTracking().Include(e => e.User).Where(e => staffEmails.Contains(e.User.Email)).ToListAsync();
        var payments = await db.Payments.AsNoTracking().Include(p => p.Package).Where(p => p.PaymentCode.StartsWith(SampleCatalog.PaymentPrefix)).ToListAsync();
        var paymentIds = payments.Select(p => p.PaymentId).ToArray();
        var memberships = await db.Memberships.AsNoTracking().Include(m => m.Package).Where(m => paymentIds.Contains(m.PaymentId)).ToListAsync();
        var bookings = await db.Bookings.AsNoTracking().Where(b => customerIds.Contains(b.CustomerId) && b.Note == SampleCatalog.BookingNote).ToListAsync();
        var bookingIds = bookings.Select(b => b.BookingId).ToArray();
        var checkIns = await db.CheckIns.AsNoTracking().Where(c => c.BookingId.HasValue && bookingIds.Contains(c.BookingId.Value)).ToListAsync();
        var schedules = await db.Ptschedules.AsNoTracking().Include(s => s.Pt).Where(s => customerIds.Contains(s.CustomerId) && s.Note == SampleCatalog.ScheduleNote).ToListAsync();
#if AI_FEATURES
        var plans = await db.WorkoutPlans.AsNoTracking().Include(p => p.WorkoutExercises).ThenInclude(e => e.Exercise)
            .Where(p => customerIds.Contains(p.CustomerId) && !p.CreatedByAi).ToListAsync();
        var conversations = await db.Aiconversations.AsNoTracking().Include(c => c.Aimessages)
            .Where(c => customerIds.Contains(c.CustomerId) && c.Title == "Bắt đầu cùng SmartGym").ToListAsync();
#endif
        Require(customers.Count == 10 && customers.All(c => c.User.Role == "CUSTOMER" && c.User.IsActive), "10 customer profiles match active CUSTOMER accounts");
        Require(employees.Count == 10 && employees.All(e => e.User.Role == "EMPLOYEE" && e.IsActive == e.User.IsActive), "10 employee profiles match EMPLOYEE accounts");
        var hasher = new PasswordHasher<User>();
        Require(customers.All(c => hasher.VerifyHashedPassword(c.User, c.User.PasswordHash, "Customer@123") != PasswordVerificationResult.Failed)
            && employees.All(e => hasher.VerifyHashedPassword(e.User, e.User.PasswordHash, "Employee@123") != PasswordVerificationResult.Failed), "sample accounts have working hashed passwords");
        Require(customers.All(c => c.DateOfBirth < DateOnly.FromDateTime(DateTime.Today) && c.Height > 0 && c.Weight > 0), "customer dates of birth and body measurements are valid");
        var packageNames = SampleCatalog.OriginalPackageNames;
        var serviceNames = SampleCatalog.Services(DateTime.Today).Select(p => p.Name).ToArray();
        var exerciseNames = SampleCatalog.Exercises(DateTime.Today).Select(p => p.Name).ToArray();
        var trainerEmails = SampleCatalog.Trainers.Select(SampleCatalog.TrainerEmail).ToArray();
        Require(await db.Packages.CountAsync(p => packageNames.Contains(p.Name)) == 4
            && payments.All(p => packageNames.Contains(p.Package.Name)), "sample payments reuse the four original packages");
        Require(await db.Services.CountAsync(s => serviceNames.Contains(s.Name)) == 10, "10 sample services");
        Require(await db.Exercises.CountAsync(e => exerciseNames.Contains(e.Name)) == 10, "10 sample exercises");
        Require(await db.Pts.CountAsync(p => trainerEmails.Contains(p.Email!)) == 10, "10 sample trainers");
        Require(payments.Count == 15 && payments.Select(p => p.PaymentCode).Distinct().Count() == 15, "15 payments have unique payment codes");
        Require(payments.All(p => p.Amount > 0 && p.Amount == p.Package.Price && p.PaymentMethod == "QR_BANK_TRANSFER"), "payment amounts match their packages and supported payment method");
        Require(payments.Where(p => p.Status == "PAID").Count() == 10 && memberships.Count == 10, "10 paid payments create exactly 10 memberships");
        Require(memberships.All(m => payments.Count(p => p.PaymentId == m.PaymentId && p.Status == "PAID" && p.CustomerId == m.CustomerId && p.PackageId == m.PackageId) == 1), "membership payment/customer/package links agree");
        Require(memberships.All(m => m.Status == "ACTIVE" && m.EndDate == m.StartDate.AddDays(m.Package.DurationDays)
            && payments.Any(p => p.PaymentId == m.PaymentId && p.PaidAt == m.StartDate && p.ConfirmedAt == m.StartDate && p.CreatedAt <= m.StartDate)), "membership dates match package duration and payment confirmation");
        Require(payments.Where(p => p.Status != "PAID").All(p => p.PaidAt == null && p.ConfirmedAt == null && memberships.All(m => m.PaymentId != p.PaymentId)), "pending/failed/cancelled payments have no membership or paid timestamp");
        Require(bookings.Count == 15 && bookings.All(b => b.EndTime > b.StartTime && b.CreatedAt <= b.StartTime
            && memberships.Any(m => m.CustomerId == b.CustomerId && m.StartDate <= b.StartTime && m.EndDate >= b.EndTime)), "15 bookings fit the correct customers' memberships");
        Require(checkIns.Count == 10 && checkIns.Select(c => c.BookingId).Distinct().Count() == 10
            && checkIns.All(c => bookings.Any(b => b.BookingId == c.BookingId && b.CustomerId == c.CustomerId
                && b.Status == "COMPLETED" && c.CheckInTime >= b.StartTime && c.CheckInTime < b.EndTime)), "10 unique check-ins match completed bookings and attendance times");
        Require(bookings.Where(b => b.Status == "COMPLETED").All(b => b.EndTime <= DateTime.Now && checkIns.Any(c => c.BookingId == b.BookingId)), "completed bookings have attendance and have ended");
        Require(!bookings.Any(a => bookings.Any(b => a.BookingId < b.BookingId && a.CustomerId == b.CustomerId
            && a.Status != "CANCELLED" && b.Status != "CANCELLED" && a.StartTime < b.EndTime && a.EndTime > b.StartTime)), "customer bookings do not overlap");
        Require(schedules.Count == 10 && schedules.All(s => s.EndTime > s.StartTime && memberships.Any(m => m.CustomerId == s.CustomerId
            && m.Package.IncludesPt && m.StartDate <= s.StartTime && m.EndDate >= s.EndTime)), "10 PT sessions fit PT-inclusive memberships");
        Require(schedules.Where(s => s.Status == "SCHEDULED").All(s => s.Pt.Status == "ACTIVE" && s.StartTime > s.CreatedAt
            && s.EndTime <= s.CreatedAt.AddDays(14)), "scheduled PT sessions use active trainers within the 14-day booking window");
        Require(!schedules.Any(a => schedules.Any(b => a.PtscheduleId < b.PtscheduleId && (a.CustomerId == b.CustomerId || a.Ptid == b.Ptid)
            && a.Status != "CANCELLED" && b.Status != "CANCELLED" && a.StartTime < b.EndTime && a.EndTime > b.StartTime)), "PT and customer schedules do not overlap");
        Require(!schedules.Any(s => bookings.Any(b => b.CustomerId == s.CustomerId && b.Status != "CANCELLED" && s.Status != "CANCELLED"
            && b.StartTime < s.EndTime && b.EndTime > s.StartTime)), "PT sessions and service bookings do not overlap for a customer");
#if AI_FEATURES
        Require(plans.Count == 10 && plans.All(p => customers.Any(c => c.CustomerId == p.CustomerId && c.FitnessGoal == p.Goal
            && c.ExperienceLevel == p.ExperienceLevel && c.PreferredSessionsPerWeek == p.SessionsPerWeek)
            && p.ModelName == null && p.ModelVersion == null && p.WorkoutExercises.Count == p.SessionsPerWeek * 4), "10 manual workout plans match member profiles and have no fabricated ML provenance");
        var levels = new Dictionary<string, int> { ["BEGINNER"] = 0, ["INTERMEDIATE"] = 1, ["ADVANCED"] = 2 };
        Require(plans.All(p => p.WorkoutExercises.All(e => e.DayNumber >= 1 && e.DayNumber <= p.SessionsPerWeek
            && e.OrderNumber >= 1 && e.OrderNumber <= 4 && e.Sets > 0 && e.Reps > 0 && e.RestSeconds >= 0
            && levels[e.Exercise.Difficulty!] <= levels[p.ExperienceLevel!])
            && p.WorkoutExercises.GroupBy(e => e.DayNumber).All(g => g.Select(e => e.ExerciseId).Distinct().Count() == 4)), "workout exercise difficulty, day/order and repetitions are consistent");
        Require(conversations.Count == 10 && conversations.All(c => c.Aimessages.Count == 2
            && c.Aimessages.Count(m => m.Role == "USER") == 1 && c.Aimessages.Count(m => m.Role == "ASSISTANT") == 1
            && c.Aimessages.Single(m => m.Role == "USER").CreatedAt < c.Aimessages.Single(m => m.Role == "ASSISTANT").CreatedAt), "10 sample conversations have correctly ordered USER/ASSISTANT messages");
#endif
        return new { Status = "Valid", PassedChecks = checks.Count, Checks = checks };
    }

    private async Task<Dictionary<string, Dictionary<long, string>>> SnapshotAsync()
    {
        var result = new Dictionary<string, Dictionary<long, string>>();
        foreach (var name in Tables.Keys) result[name] = await ReadTableAsync(name);
        return result;
    }

    private async Task<Dictionary<long, string>> ReadTableAsync(string name, long[]? ids = null)
    {
        if (ids is { Length: 0 }) return [];
        if (db.Database.GetDbConnection().State != ConnectionState.Open)
            await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = $"SELECT * FROM dbo.[{name}]" +
            (ids == null ? "" : $" WHERE [{Tables[name]}] IN ({string.Join(',', ids)})") + $" ORDER BY [{Tables[name]}]";
        var result = new Dictionary<long, string>();
        await using var reader = await command.ExecuteReaderAsync();
        var keyOrdinal = reader.GetOrdinal(Tables[name]);
        while (await reader.ReadAsync())
        {
            var values = new object?[reader.FieldCount];
            for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            result[reader.GetInt64(keyOrdinal)] = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(values)));
        }
        return result;
    }
}
