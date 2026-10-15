using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartGymBooking.Controllers;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.ViewModels;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = Directory.GetCurrentDirectory() });
var connection = builder.Configuration.GetConnectionString("DefaultConnection");
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
using var db = new ApplicationDbContext(options);
var today = DateTime.Today;
var month = new DateTime(today.Year, today.Month, 1);
async Task<AdminDashboardViewModel> Admin() => (AdminDashboardViewModel)((ViewResult)await new AdminDashboardController(db).Index()).Model!;
var model = await Admin();
var employee = (EmployeeDashboardViewModel)((ViewResult)await new EmployeeDashboardController(db).Index()).Model!;
var payments = await db.Payments.AsNoTracking().ToListAsync();
var bookings = await db.Bookings.AsNoTracking().ToListAsync();
var customers = await db.Customers.AsNoTracking().Include(c => c.User).ToListAsync();
Check(model.TotalCustomers == customers.Count && model.ActiveCustomers == customers.Count(c => c.User.IsActive), "Customer counts");
Check(model.ActivePTs == await db.Pts.CountAsync(p => p.Status == "ACTIVE"), "Active PTs");
Check(model.ActiveMemberships == await db.Memberships.CountAsync(m => m.Status == "ACTIVE"), "Memberships");
Check(model.RevenueThisMonth == payments.Where(p => p.Status == "PAID" && p.PaidAt >= month && p.PaidAt < month.AddMonths(1)).Sum(p => p.Amount), "Monthly revenue");
Check(model.MonthlyRevenueLabels.Count == 6 && model.MonthlyRevenueValues.Count == 6, "Six months");
for (var i=0; i<6; i++)
{
    var start = month.AddMonths(i-5);
    Check(model.MonthlyRevenueLabels[i] == start.ToString("MM/yyyy"), "Chart labels");
    Check(model.MonthlyRevenueValues[i] == payments.Where(p => p.Status == "PAID" && p.PaidAt >= start && p.PaidAt < start.AddMonths(1)).Sum(p => p.Amount), "Chart sum");
}
Check(new[]{model.PendingBookings,model.ConfirmedBookings,model.CompletedBookings,model.CancelledBookings}.SequenceEqual(new[]{"PENDING","CONFIRMED","COMPLETED","CANCELLED"}.Select(s=>bookings.Count(b=>b.Status==s))), "Booking statuses");
Check(model.TodayBookings == bookings.Count(b=> b.StartTime >= today && b.StartTime < today.AddDays(1) && b.Status != "CANCELLED"), "Today's bookings");
Check(employee.TodayBookings == model.TodayBookings && employee.PendingPayments == payments.Count(p=>p.Status=="PENDING") && employee.PendingBookings == model.PendingBookings && employee.TodayCheckIns == model.TodayCheckIns, "Employee counters");
Check(model.TodayCheckIns == await db.CheckIns.CountAsync(c=> c.CheckInTime >= today && c.CheckInTime < today.AddDays(1)), "Check-ins");
Check(employee.TodayPTSchedules == await db.Ptschedules.CountAsync(s=> s.StartTime >= today && s.StartTime < today.AddDays(1) && s.Status == "SCHEDULED"), "PT schedules today");
Check(model.RecentPayments.Select(p=>p.PaymentId).SequenceEqual(payments.OrderByDescending(p=>p.CreatedAt).ThenByDescending(p=>p.PaymentId).Take(5).Select(p=>p.PaymentId)), "Recent payments");
Check(model.RecentBookings.Select(b=>b.BookingId).SequenceEqual(bookings.OrderByDescending(b=>b.CreatedAt).ThenByDescending(b=>b.BookingId).Take(5).Select(b=>b.BookingId)), "Recent bookings");
Check(employee.PendingPaymentList.All(p=>p.Status=="PENDING") && employee.PendingPaymentList.Count<=5 && employee.UpcomingBookings.Count<=8, "Employee lists");

// SQL fixtures exist only inside this transaction and are always rolled back.
await using (var transaction = await db.Database.BeginTransactionAsync())
{
    var customerId = customers.First().CustomerId;
    var packageId = await db.Packages.Select(p=>p.PackageId).FirstAsync();
    foreach (var item in new[] { ("PAID",month,101m), ("PAID",month.AddMonths(-1),202m), ("PAID",month.AddMonths(1),303m), ("PENDING",month,404m), ("FAILED",month,505m), ("CANCELLED",month,606m) })
        db.Payments.Add(new Payment { CustomerId=customerId, PackageId=packageId, PaymentCode="DASH-"+Guid.NewGuid().ToString("N")[..20], PaymentMethod="QR_BANK_TRANSFER", Status=item.Item1, PaidAt=item.Item1 == "PAID" ? item.Item2 : null, Amount=item.Item3, CreatedAt=item.Item2.AddDays(-1) });
    var serviceId = await db.Services.Select(s=>s.ServiceId).FirstAsync();
    foreach (var item in new[] { ("PENDING",today), ("CANCELLED",today), ("PENDING",today.AddDays(1)) })
        db.Bookings.Add(new Booking { CustomerId=customerId, ServiceId=serviceId, Status=item.Item1, StartTime=item.Item2, EndTime=item.Item2.AddHours(1), CreatedAt=DateTime.Now });
    await db.SaveChangesAsync();
    var changed = await Admin();
    Check(changed.RevenueThisMonth == model.RevenueThisMonth+101, "Revenue includes PAID only and excludes next month");
    Check(changed.MonthlyRevenueValues[4] == model.MonthlyRevenueValues[4]+202 && changed.MonthlyRevenueValues[5] == model.MonthlyRevenueValues[5]+101, "Revenue month boundaries");
    Check(changed.TodayBookings == model.TodayBookings+1, "Today excludes tomorrow and cancelled bookings");
    Check(changed.PendingPayments == model.PendingPayments+1, "Pending counter refresh");
    await transaction.RollbackAsync();
}
using var empty = new EmptyContext(options);
var zero = (AdminDashboardViewModel)((ViewResult)await new AdminDashboardController(empty).Index()).Model!;
var zeroEmployee = (EmployeeDashboardViewModel)((ViewResult)await new EmployeeDashboardController(empty).Index()).Model!;
Check(zero.RevenueThisMonth==0 && zero.TotalCustomers==0 && zero.MonthlyRevenueValues.Count==6 && zero.MonthlyRevenueValues.All(v=>v==0), "Empty dashboard / six zero months");
Check(zero.RecentPayments.Count==0 && zero.RecentBookings.Count==0 && zero.UpcomingSchedules.Count==0 && zeroEmployee.UpcomingBookings.Count==0 && zeroEmployee.PendingPaymentList.Count==0, "Empty lists");
Console.WriteLine("PASS: SQL aggregates, six months, status counts, recent lists, employee counters, rolled-back boundary/status fixtures, empty datasets.");
static void Check(bool value,string name) { if(!value) throw new Exception(name); }
class EmptyContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Customer>().HasQueryFilter(_=>false);
        b.Entity<Pt>().HasQueryFilter(_=>false);
        b.Entity<Membership>().HasQueryFilter(_=>false);
        b.Entity<Booking>().HasQueryFilter(_=>false);
        b.Entity<CheckIn>().HasQueryFilter(_=>false);
        b.Entity<Payment>().HasQueryFilter(_=>false);
        b.Entity<Ptschedule>().HasQueryFilter(_=>false);
    }
}

