using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;

namespace SmartGymBooking.Services;

public static class GymTime
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static readonly TimeSpan OpeningTime = TimeSpan.FromHours(6);
    public static readonly TimeSpan ClosingTime = TimeSpan.FromHours(22);
    public const string HoursMessage = "Phòng tập mở cửa từ 06:00 đến 22:00 (giờ Việt Nam).";

    public static bool IsOpen(DateTime now) => now.TimeOfDay >= OpeningTime && now.TimeOfDay < ClosingTime;

    public static bool IsWithinHours(DateTime start, DateTime end) =>
        start.Date == end.Date && IsOpen(start) && end.TimeOfDay <= ClosingTime;

    public static DateTime Now => FromUtc(TimeProvider.System.GetUtcNow());

    public static DateTime FromUtc(DateTimeOffset utc) => utc.ToOffset(VietnamOffset).DateTime;
}

public sealed class BookingExpirationService(ApplicationDbContext context, TimeProvider timeProvider)
{
    public async Task<string?> CheckInAsync(long bookingId, long? customerId = null,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var booking = await context.Bookings
            .FromSqlInterpolated($"SELECT * FROM dbo.Bookings WITH (UPDLOCK, HOLDLOCK) WHERE BookingId = {bookingId}")
            .SingleOrDefaultAsync(b => b.BookingId == bookingId && (!customerId.HasValue || b.CustomerId == customerId), cancellationToken);
        if (booking == null) return "Không tìm thấy lịch tập.";

        var now = GymTime.FromUtc(timeProvider.GetUtcNow());
        if (!GymTime.IsOpen(now)) return GymTime.HoursMessage;
        if (await context.CheckIns.AnyAsync(c => c.BookingId == bookingId, cancellationToken))
            return "Lịch tập này đã được check-in.";
        if (booking.EndTime <= now)
        {
            await CancelExpiredAsync(bookingId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return "Lịch tập đã quá giờ kết thúc.";
        }
        if (booking.Status != "CONFIRMED") return "Lịch tập chưa được xác nhận hoặc đã hủy.";
        if (booking.StartTime > now) return "Chỉ có thể check-in khi lịch tập đã bắt đầu.";

        context.CheckIns.Add(new CheckIn
        {
            CustomerId = booking.CustomerId,
            BookingId = bookingId,
            CheckInTime = now,
            CreatedAt = now
        });
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException)
        {
            return "Không thể check-in. Vui lòng kiểm tra lại trạng thái lịch tập.";
        }
    }

    public Task<int> CancelExpiredAsync(long? bookingId = null, CancellationToken cancellationToken = default)
    {
        var now = GymTime.FromUtc(timeProvider.GetUtcNow());
        var bookings = context.Bookings.Where(b =>
            (b.Status == "PENDING" || b.Status == "CONFIRMED") &&
            b.EndTime <= now &&
            !context.CheckIns.Any(c => c.BookingId == b.BookingId));

        if (bookingId.HasValue)
            bookings = bookings.Where(b => b.BookingId == bookingId.Value);

        return bookings.ExecuteUpdateAsync(setters => setters
            .SetProperty(b => b.Status, "CANCELLED")
            .SetProperty(b => b.UpdatedAt, now), cancellationToken);
    }
}

public sealed class BookingExpirationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var expiration = scope.ServiceProvider.GetRequiredService<BookingExpirationService>();
                var count = await expiration.CancelExpiredAsync(cancellationToken: stoppingToken);
                if (count > 0)
                    logger.LogInformation("Automatically cancelled {Count} expired bookings without check-in.", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not expire bookings; the next scheduled check will retry.");
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await PackagePurchase.ExpirePendingAsync(db, GymTime.Now, cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not expire payments; the next scheduled check will retry.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
