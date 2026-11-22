using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Models;
using SmartGymBooking.Services;

namespace SmartGymBooking.SampleData;

internal sealed partial class SampleDataSeeder
{
    private const string ExtraPrefix = "SGM26B";
    private const string ExtraNote = "Dữ liệu mẫu bổ sung SmartGym";
#if AI_FEATURES
    private const string ExtraPlan = "Giáo án kỹ thuật SmartGym";
#endif

    public async Task<object> AdditionalAsync(bool apply)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource=N'SmartGymBooking.SampleData.V1',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @result < 0 THROW 50001, 'Could not lock sample data batch.', 1;
            """);
        if (await db.Payments.AnyAsync(p => p.PaymentCode.StartsWith(ExtraPrefix)))
        {
            var existing = await VerifyAdditionalAsync();
            await transaction.CommitAsync();
            return new { Mode = "AlreadyPresent", Verification = existing };
        }

        var now = DateTime.Now;
        var originalPayments = Enumerable.Range(1, SampleCatalog.Members.Length)
            .Select(i => $"{SampleCatalog.PaymentPrefix}{i:D3}P").ToArray();
        var previous = await db.Memberships.AsNoTracking().Include(m => m.Package)
            .Include(m => m.Customer).ThenInclude(c => c.User).Include(m => m.Payment)
            .Where(m => originalPayments.Contains(m.Payment.PaymentCode)).OrderBy(m => m.Payment.PaymentCode)
            .ToListAsync();
        if (previous.Count != originalPayments.Length ||
            !previous.Select(m => m.Customer.User.Email).SequenceEqual(MemberEmails) ||
            previous.Any(m => m.Payment.Status != "PAID" || !m.Package.IsActive ||
                m.CustomerId != m.Payment.CustomerId || m.PackageId != m.Payment.PackageId) ||
            previous.Count(m => m.Package.IncludesPt) != 5)
            throw new InvalidOperationException("The extra batch requires the ten existing sample memberships and their original active packages.");

        var knownPackages = SampleCatalog.OriginalPackageNames;
        if (previous.Any(m => !knownPackages.Contains(m.Package.Name)))
            throw new InvalidOperationException("Restore the original sample packages before adding the extra batch.");

        var historyStart = previous.Min(m => m.StartDate).AddDays(-1);
        var services = SampleCatalog.AdditionalServices(historyStart);
        var exercises = SampleCatalog.AdditionalExercises(historyStart);
        var hasher = new PasswordHasher<User>();
        var employees = SampleCatalog.AdditionalEmployees.Select(person =>
        {
            var user = new User
            {
                Email = SampleCatalog.EmployeeEmail(person), Role = "EMPLOYEE", IsActive = true, CreatedAt = now
            };
            user.PasswordHash = hasher.HashPassword(user, "Employee@123");
            return new Employee
            {
                User = user, FullName = person.Name, Phone = person.Phone, Gender = person.Gender,
                DateOfBirth = DateOnly.ParseExact(person.Birth, "yyyy-MM-dd"), Position = person.Position,
                IsActive = true, CreatedAt = now
            };
        }).ToArray();
        var trainers = SampleCatalog.AdditionalTrainers.Select(person => new Pt
        {
            FullName = person.Name, Email = SampleCatalog.TrainerEmail(person), Phone = person.Phone,
            Specialization = person.Specialization, Experience = person.Experience,
            Description = $"Huấn luyện viên chuyên về {person.Specialization.ToLowerInvariant()}.",
            Status = "ACTIVE", CreatedAt = historyStart
        }).ToArray();

        var staffEmails = employees.Select(e => e.User.Email).ToArray();
        var staffPhones = employees.Select(e => e.Phone).ToArray();
        var ptEmails = trainers.Select(p => p.Email).ToArray();
        var ptPhones = trainers.Select(p => p.Phone).ToArray();
        var serviceNames = services.Select(s => s.Name).ToArray();
        var exerciseNames = exercises.Select(e => e.Name).ToArray();
        var existingServiceNames = await db.Services.Where(s => serviceNames.Contains(s.Name)).Select(s => s.Name).ToListAsync();
        var existingExerciseNames = await db.Exercises.Where(e => exerciseNames.Contains(e.Name)).Select(e => e.Name).ToListAsync();
        if (existingServiceNames.Count > 0 || existingExerciseNames.Count > 0)
            throw new InvalidOperationException("Existing services or exercises: " +
                string.Join(", ", existingServiceNames.Concat(existingExerciseNames)));

        if (await db.Users.AnyAsync(u => staffEmails.Contains(u.Email)) ||
            await db.Employees.AnyAsync(e => staffPhones.Contains(e.Phone)) ||
            await db.Pts.AnyAsync(p => ptEmails.Contains(p.Email) || ptPhones.Contains(p.Phone)))
            throw new InvalidOperationException("An employee or trainer conflicts with the extra sample batch.");

        var customerIds = previous.Select(m => m.CustomerId).ToArray();
        var latestMembershipEnds = await db.Memberships.AsNoTracking()
            .Where(m => customerIds.Contains(m.CustomerId)).GroupBy(m => m.CustomerId)
            .ToDictionaryAsync(g => g.Key, g => g.Max(m => m.EndDate));
        var bookings = await db.Bookings.AsNoTracking()
            .Where(b => customerIds.Contains(b.CustomerId) && b.Status != "CANCELLED").ToListAsync();
        var schedules = await db.Ptschedules.AsNoTracking()
            .Where(s => customerIds.Contains(s.CustomerId) && s.Status != "CANCELLED").ToListAsync();

        DateTime FindSlot(Membership membership, int hour, int minutes)
        {
            var last = membership.EndDate < now ? membership.EndDate : now;
            for (var start = membership.StartDate.Date.AddDays(1).AddHours(hour);
                start.AddMinutes(minutes + 5) <= last; start = start.AddDays(1))
            {
                var end = start.AddMinutes(minutes);
                if (!bookings.Any(b => b.CustomerId == membership.CustomerId && b.StartTime < end && b.EndTime > start) &&
                    !schedules.Any(s => s.CustomerId == membership.CustomerId && s.StartTime < end && s.EndTime > start))
                    return start;
            }
            throw new InvalidOperationException($"No historical slot within membership {membership.MembershipId}.");
        }

        db.Employees.AddRange(employees);
        db.Pts.AddRange(trainers);
        db.Services.AddRange(services);
        db.Exercises.AddRange(exercises);
        var trainerIndex = 0;
        for (var i = 0; i < previous.Count; i++)
        {
            var membership = previous[i];
            var customerId = membership.CustomerId;
            var renewalStart = latestMembershipEnds[customerId] > now ? latestMembershipEnds[customerId] : now;
            var payment = new Payment
            {
                CustomerId = customerId, PackageId = membership.PackageId, Amount = membership.Package.Price,
                PaymentCode = $"{ExtraPrefix}{i + 1:D3}P", PaymentMethod = "QR_BANK_TRANSFER",
                Status = "PAID", CreatedAt = now.AddMinutes(-10), PaidAt = now, ConfirmedAt = now
            };
            db.Memberships.Add(new Membership
            {
                CustomerId = customerId, PackageId = membership.PackageId, Payment = payment,
                StartDate = renewalStart, EndDate = renewalStart.AddDays(membership.Package.DurationDays),
                Status = "ACTIVE", CreatedAt = now
            });

            var minutes = services[i].DurationMinutes!.Value;
            var start = FindSlot(membership, 10, minutes);
            var booking = new Booking
            {
                CustomerId = customerId, Service = services[i], StartTime = start, EndTime = start.AddMinutes(minutes),
                Status = "COMPLETED", Note = ExtraNote, CreatedAt = membership.StartDate,
                UpdatedAt = start.AddMinutes(minutes + 5)
            };
            bookings.Add(booking);
            db.CheckIns.Add(new CheckIn
            {
                CustomerId = customerId, Booking = booking, CheckInTime = start.AddMinutes(5), CreatedAt = start.AddMinutes(5)
            });

            if (membership.Package.IncludesPt)
            {
                for (var hour = 14; hour <= 16; hour += 2)
                {
                    var slot = FindSlot(membership, hour, 60);
                    var schedule = new Ptschedule
                    {
                        Pt = trainers[trainerIndex++], CustomerId = customerId, StartTime = slot, EndTime = slot.AddHours(1),
                        Status = "COMPLETED", Note = ExtraNote, CreatedAt = membership.StartDate, UpdatedAt = slot.AddMinutes(65)
                    };
                    schedules.Add(schedule);
                    db.Ptschedules.Add(schedule);
                }
            }
#if AI_FEATURES
            var plan = BuildPlan(membership.Customer, exercises, now.AddMinutes(-5));
            plan.Title = ExtraPlan;
            db.WorkoutPlans.Add(plan);
            const string question = "Giáo án của tôi là gì?";
            var conversation = new Aiconversation
            {
                CustomerId = customerId, Title = ExtraPlan, CreatedAt = now, UpdatedAt = now.AddSeconds(1)
            };
            conversation.Aimessages.Add(new Aimessage { Role = "USER", Content = question, CreatedAt = now });
            conversation.Aimessages.Add(new Aimessage
            {
                Role = "ASSISTANT", Content = await new DemoAIChatService().GetResponseAsync(membership.Customer, question, [], plan),
                CreatedAt = now.AddSeconds(1)
            });
            db.Aiconversations.Add(conversation);
#endif
        }
        if (!apply)
        {
            var planned = db.ChangeTracker.Entries().GroupBy(e => e.Metadata.ClrType.Name)
                .ToDictionary(g => g.Key, g => g.Count());
            return new { Mode = "PreviewExtra", PlannedRows = planned };
        }

        var before = await SnapshotAsync();
        await db.SaveChangesAsync();
        var verification = await VerifyAdditionalAsync();
        foreach (var (name, original) in before)
        {
            var preserved = await ReadTableAsync(name, original.Keys.ToArray());
            if (!original.OrderBy(p => p.Key).SequenceEqual(preserved.OrderBy(p => p.Key)))
                throw new InvalidOperationException($"Existing records changed in {name}; extra inserts will roll back.");
        }
        await transaction.CommitAsync();
        return new { Mode = "AppliedExtra", Verification = verification };
    }

    public async Task<object> VerifyAdditionalAsync()
    {
        var payments = await db.Payments.AsNoTracking().Include(p => p.Package).Include(p => p.Membership)
            .Where(p => p.PaymentCode.StartsWith(ExtraPrefix)).ToListAsync();
        var ids = payments.Select(p => p.CustomerId).Distinct().ToArray();
        var bookings = await db.Bookings.AsNoTracking().Include(b => b.CheckIn).Include(b => b.Service)
            .Where(b => ids.Contains(b.CustomerId) && b.Note == ExtraNote).ToListAsync();
        var schedules = await db.Ptschedules.AsNoTracking()
            .Where(s => ids.Contains(s.CustomerId) && s.Note == ExtraNote).ToListAsync();
        var memberships = await db.Memberships.AsNoTracking().Include(m => m.Package).Include(m => m.Payment)
            .Where(m => ids.Contains(m.CustomerId)).ToListAsync();
        bool Covered(long customerId, DateTime start, DateTime end, bool needsPt) => memberships.Any(m =>
            m.CustomerId == customerId && m.StartDate <= start && end <= m.EndDate && m.Payment.Status == "PAID" &&
            (!needsPt || m.Package.IncludesPt));

        if (ids.Length != 10 || payments.Count != 10 || payments.Any(p =>
                p.Status != "PAID" || p.Amount != p.Package.Price || p.Membership is not { } m ||
                m.CustomerId != p.CustomerId || m.PackageId != p.PackageId ||
                m.EndDate != m.StartDate.AddDays(p.Package.DurationDays)) ||
            bookings.Count != 10 || bookings.Any(b => b.Status != "COMPLETED" ||
                b.EndTime != b.StartTime.AddMinutes(b.Service.DurationMinutes!.Value) ||
                b.CheckIn is not { } checkIn || checkIn.CustomerId != b.CustomerId ||
                checkIn.CheckInTime < b.StartTime || checkIn.CheckInTime > b.EndTime ||
                !Covered(b.CustomerId, b.StartTime, b.EndTime, needsPt: false)) ||
            schedules.Count != 10 || schedules.Any(s => s.Status != "COMPLETED" ||
                !Covered(s.CustomerId, s.StartTime, s.EndTime, needsPt: true)))
            throw new InvalidOperationException("The extra sample payments, memberships, bookings, check-ins or PT schedules are inconsistent.");

        var staffEmails = SampleCatalog.AdditionalEmployees.Select(SampleCatalog.EmployeeEmail).ToArray();
        var ptEmails = SampleCatalog.AdditionalTrainers.Select(SampleCatalog.TrainerEmail).ToArray();
        var serviceNames = SampleCatalog.AdditionalServices(DateTime.Today).Select(s => s.Name).ToArray();
        var exerciseNames = SampleCatalog.AdditionalExercises(DateTime.Today).Select(e => e.Name).ToArray();
        var counts = new Dictionary<string, int>
        {
            ["Employees"] = await db.Employees.CountAsync(e => staffEmails.Contains(e.User.Email) && e.User.Role == "EMPLOYEE"),
            ["PTs"] = await db.Pts.CountAsync(p => ptEmails.Contains(p.Email)),
            ["Services"] = await db.Services.CountAsync(s => serviceNames.Contains(s.Name)),
            ["Exercises"] = await db.Exercises.CountAsync(e => exerciseNames.Contains(e.Name) && e.VideoUrl != null),
            ["Payments"] = payments.Count, ["Memberships"] = payments.Count, ["Bookings"] = bookings.Count,
            ["CheckIns"] = bookings.Count, ["PTSchedules"] = schedules.Count
        };
#if AI_FEATURES
        counts["WorkoutPlans"] = await db.WorkoutPlans.CountAsync(p => ids.Contains(p.CustomerId) && p.Title == ExtraPlan);
        counts["AIConversations"] = await db.Aiconversations.CountAsync(c =>
            ids.Contains(c.CustomerId) && c.Title == ExtraPlan && c.Aimessages.Count == 2);
#endif
        if (counts.Values.Any(count => count != 10))
            throw new InvalidOperationException("The extra sample batch is incomplete.");
        return new { Rows = counts, CustomersAdded = 0, PackagesAdded = 0 };
    }
}
