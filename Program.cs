using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

#if AI_FEATURES
builder.Services.AddSingleton<IWorkoutRecommendationService, WorkoutRecommendationService>();
builder.Services.AddScoped<IWorkoutPlanGeneratorService, WorkoutPlanGeneratorService>();
builder.Services.AddScoped<DemoAIChatService>();
builder.Services.AddHttpClient<OpenAIChatService>(client => client.Timeout = TimeSpan.FromSeconds(45));
builder.Services.AddScoped<IAIChatService, AIChatRouterService>();
#endif

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";

        options.Cookie.Name = "SmartGymBooking.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.Events.OnValidatePrincipal = async context =>
        {
            var validId = long.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var account = validId ? await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userId) : null;
            if (account == null || !account.IsActive || account.Role != context.Principal?.FindFirstValue(ClaimTypes.Role))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };

        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/ServerError");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Error/StatusCode/{0}");

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await DbInitializer.InitializeAsync(app.Services);

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    application = "SmartGymBooking",
    time = DateTime.UtcNow
})).AllowAnonymous();

app.Run();
