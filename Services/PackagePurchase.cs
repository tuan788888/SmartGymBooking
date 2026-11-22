using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;

namespace SmartGymBooking.Services;

public static class PackagePurchase
{
    public static readonly TimeSpan ConfirmationWindow = TimeSpan.FromHours(1);

    public static IQueryable<Payment> AwaitingConfirmation(IQueryable<Payment> payments, DateTime now)
    {
        var cutoff = now - ConfirmationWindow;
        return payments.Where(p => p.Status == "PENDING" && (p.PaidAt ?? p.CreatedAt) > cutoff);
    }

    public static Task<int> ExpirePendingAsync(ApplicationDbContext db, DateTime now, long? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var cutoff = now - ConfirmationWindow;
        var payments = db.Payments.Where(p => p.Status == "PENDING" && (p.PaidAt ?? p.CreatedAt) <= cutoff);
        if (customerId.HasValue)
            payments = payments.Where(p => p.CustomerId == customerId.Value);

        return payments.ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "CANCELLED"), cancellationToken);
    }

    public const int UpgradeDiscountPercent = 20;
    private const string UpgradePrefix = "SGUP";

    public sealed record Quote(decimal Amount, DateTime EndDate, Membership? UpgradeFrom, string? Error = null);

    public static Task<List<Membership>> LoadCurrentAsync(ApplicationDbContext db, long customerId, DateTime now)
    {
        return db.Memberships.Include(m => m.Package)
            .Where(m => m.CustomerId == customerId && m.Status == "ACTIVE" && m.EndDate > now)
            .OrderBy(m => m.StartDate).ToListAsync();
    }

    public static Quote GetQuote(Package target, IReadOnlyList<Membership> memberships, DateTime now)
    {
        if (!target.IsActive)
            return new(0, now, null, "Gói tập này hiện đã ngừng bán.");

        var active = memberships.FirstOrDefault(m => m.StartDate <= now);
        if (active != null && (active.Package.IncludesPt || !target.IncludesPt ||
            target.DurationDays != active.Package.DurationDays || target.Price <= active.Package.Price))
        {
            return new(0, active.EndDate, null,
                $"Bạn đang dùng gói {active.Package.Name} đến {active.EndDate:dd/MM/yyyy HH:mm}. Trong thời gian này, bạn chỉ có thể nâng cấp lên gói PT giá cao hơn có cùng thời hạn.");
        }

        var end = active?.EndDate ?? now.AddDays(target.DurationDays);
        var overlap = memberships.FirstOrDefault(m => m.MembershipId != active?.MembershipId && m.StartDate < end);
        if (overlap != null)
            return new(0, overlap.EndDate, null,
                $"Gói này sẽ trùng thời gian với gói {overlap.Package.Name}. Bạn có thể mua sau {overlap.EndDate:dd/MM/yyyy HH:mm}.");

        var amount = active == null ? target.Price : decimal.Round(
            target.Price * (1 - UpgradeDiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
        return new(amount, end, active);
    }

    public static string CreatePaymentCode(Membership? upgradeFrom)
    {
        if (upgradeFrom == null) return $"SG{Guid.NewGuid():N}";
        var token = Guid.NewGuid().ToString("N")[..26];
        return $"{UpgradePrefix}{upgradeFrom.MembershipId.ToString(CultureInfo.InvariantCulture)}-{token}";
    }

    public static long? GetUpgradeSource(string code)
    {
        var separator = code.IndexOf('-');
        return code.StartsWith(UpgradePrefix, StringComparison.Ordinal) && separator > UpgradePrefix.Length &&
            long.TryParse(code.AsSpan(UpgradePrefix.Length, separator - UpgradePrefix.Length),
                NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : null;
    }

    public static string? ValidatePayment(Payment payment, Quote quote)
    {
        return quote.Error ?? (GetUpgradeSource(payment.PaymentCode) != quote.UpgradeFrom?.MembershipId
            ? "Điều kiện mua gói đã thay đổi. Vui lòng liên hệ SmartGym nếu bạn đã chuyển khoản."
            : null);
    }

    public static Task LockCustomerAsync(ApplicationDbContext db, long customerId)
    {
        var resource = $"SmartGymBooking.PackagePurchase.{customerId}";
        return db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource={resource}, @LockMode='Exclusive',
                @LockOwner='Transaction', @LockTimeout=10000;
            IF @result < 0 THROW 50002, 'Could not lock the customer package purchase.', 1;
            """);
    }
}
