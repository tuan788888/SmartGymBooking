using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartGymBooking.Controllers;
using SmartGymBooking.Data;
using SmartGymBooking.ViewModels;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = Directory.GetCurrentDirectory() });
using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")).Options);
var command = args.FirstOrDefault() ?? "integrity";
var prefix = args.Skip(1).FirstOrDefault() ?? "B33QA";
if (command == "snapshot")
{
    var customerIds = await db.Customers.Where(c=>c.FullName.StartsWith(prefix)).Select(c=>c.CustomerId).ToListAsync();
    Console.WriteLine(JsonSerializer.Serialize(new {
        Customers = await db.Customers.Where(c=>customerIds.Contains(c.CustomerId)).Select(c=>new { c.CustomerId, c.UserId, c.FullName, c.User.Role, c.User.IsActive }).ToListAsync(),
        Employees = await db.Employees.Where(c=>c.FullName.StartsWith(prefix)).Select(c=>new { c.EmployeeId, c.FullName, c.User.IsActive }).ToListAsync(),
        PTs = await db.Pts.Where(c=>c.FullName.StartsWith(prefix)).Select(c=>new { c.Ptid, c.FullName, c.Status, c.Avatar }).ToListAsync(),
        Packages = await db.Packages.Where(c=>c.Name.StartsWith(prefix)).Select(c=>new { c.PackageId, c.Name, c.IsActive }).ToListAsync(),
        Services = await db.Services.Where(c=>c.Name.StartsWith(prefix)).Select(c=>new { c.ServiceId, c.Name, c.IsActive }).ToListAsync(),
        Exercises = await db.Exercises.Where(c=>c.Name.StartsWith(prefix)).Select(c=>new { c.ExerciseId, c.Name, c.IsActive }).ToListAsync(),
        Payments = await db.Payments.Where(c=>customerIds.Contains(c.CustomerId)).Select(c=>new { c.PaymentId,c.CustomerId,c.PackageId,c.Status }).ToListAsync(),
        Memberships = await db.Memberships.Where(c=>customerIds.Contains(c.CustomerId)).Select(c=>new { c.MembershipId,c.PaymentId,c.Status }).ToListAsync(),
        Bookings = await db.Bookings.Where(c=>customerIds.Contains(c.CustomerId)).Select(c=>new { c.BookingId,c.CustomerId,c.ServiceId,c.Status }).ToListAsync(),
        CheckIns = await db.CheckIns.Where(c=>customerIds.Contains(c.CustomerId)).Select(c=>new { c.CheckInId,c.BookingId }).ToListAsync(),
        Schedules = await db.Ptschedules.Where(c=>customerIds.Contains(c.CustomerId)).Select(c=>new { c.PtscheduleId,c.Ptid,c.Status }).ToListAsync(),
        Plans = await db.WorkoutPlans.Where(c=>customerIds.Contains(c.CustomerId)).Select(c=>new { c.WorkoutPlanId,c.ModelName,c.ModelVersion,c.PredictedPlanType,Exercises=c.WorkoutExercises.Count,FirstExercise=c.WorkoutExercises.Select(e=>e.ExerciseId).FirstOrDefault() }).ToListAsync(),
        Conversations = await db.Aiconversations.Where(c=>customerIds.Contains(c.CustomerId)).Select(c=>new { c.AiconversationId,c.CustomerId,Roles=c.Aimessages.OrderBy(m=>m.AimessageId).Select(m=>m.Role).ToList() }).ToListAsync()
    }));
    return;
}
if (command == "age")
{
    // Only explicitly named B33 QA records; never touch other customers' history.
    if (!prefix.StartsWith("B33QA") || prefix.Length < 10) throw new Exception("QA prefix required");
    var ids = db.Customers.Where(c=>c.FullName.StartsWith(prefix)).Select(c=>c.CustomerId);
    var now=DateTime.Now;
    await db.Memberships.Where(m=>ids.Contains(m.CustomerId)).ExecuteUpdateAsync(s=>s.SetProperty(m=>m.StartDate,now.AddDays(-1)));
    await db.Bookings.Where(b=>ids.Contains(b.CustomerId) && b.Status=="CONFIRMED").ExecuteUpdateAsync(s=>s.SetProperty(b=>b.StartTime,now.AddHours(-2)).SetProperty(b=>b.EndTime,now.AddHours(-1)));
    await db.Ptschedules.Where(b=>ids.Contains(b.CustomerId) && b.Status=="SCHEDULED").ExecuteUpdateAsync(s=>s.SetProperty(b=>b.StartTime,now.AddHours(-2)).SetProperty(b=>b.EndTime,now.AddHours(-1)));
    Console.WriteLine("QA schedule timestamps advanced for completion checks."); return;
}
if (command == "retire")
{
    if (prefix != "B33QA") throw new Exception("Use the explicit B33QA test prefix");
    await db.Users.Where(u=>u.Email.StartsWith("b33qa") && u.Email.EndsWith("@example.test"))
        .ExecuteUpdateAsync(s=>s.SetProperty(u=>u.IsActive,false));
    await db.Pts.Where(p=>p.FullName.StartsWith(prefix)).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.Status,"INACTIVE"));
    await db.Packages.Where(p=>p.Name.StartsWith(prefix)).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.IsActive,false));
    await db.Services.Where(p=>p.Name.StartsWith(prefix)).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.IsActive,false));
    Console.WriteLine("Retired B33QA accounts and resources; history preserved."); return;
}
var issues = new Dictionary<string,int> {
    ["DuplicateEmails"] = await db.Users.GroupBy(u=>u.Email).CountAsync(g=>g.Count()>1),
    ["DuplicateCustomerUsers"] = await db.Customers.GroupBy(u=>u.UserId).CountAsync(g=>g.Count()>1),
    ["DuplicateMembershipPayments"] = await db.Memberships.GroupBy(u=>u.PaymentId).CountAsync(g=>g.Count()>1),
    ["DuplicateCheckInBookings"] = await db.CheckIns.Where(c=>c.BookingId!=null).GroupBy(c=>c.BookingId).CountAsync(g=>g.Count()>1),
    ["InvalidBookingTimes"] = await db.Bookings.CountAsync(b=>b.EndTime<=b.StartTime),
    ["InvalidPTTimes"] = await db.Ptschedules.CountAsync(b=>b.EndTime<=b.StartTime)
};
Console.WriteLine(JsonSerializer.Serialize(issues));
if (issues.Values.Any(v=>v!=0)) throw new Exception("Database integrity issue");
var assembly=typeof(AuthController).Assembly;
foreach(var type in assembly.GetTypes().Where(t=>t.IsSubclassOf(typeof(Controller))))
    foreach(var action in type.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))
        if(action.GetCustomAttributes<HttpMethodAttribute>().Any(a=>a.HttpMethods.Contains("POST")) && action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()==null)
            throw new Exception($"Missing explicit antiforgery: {type.Name}.{action.Name}");
if (typeof(RegisterViewModel).GetProperty("Role")!=null) throw new Exception("Public registration exposes role");
if (builder.Configuration["AI:Provider"]!="DEMO") throw new Exception("AI must remain DEMO");
Console.WriteLine("PASS: DB integrity, POST antiforgery attributes, registration role and DEMO configuration.");
