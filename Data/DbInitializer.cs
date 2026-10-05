using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Models;

namespace SmartGymBooking.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var passwordHasher = scope.ServiceProvider
                .GetRequiredService<IPasswordHasher<User>>();

            var users = await context.Users.ToListAsync();

            foreach (var user in users)
            {
                string? newPassword = null;

                if (user.PasswordHash == "TEMP_ADMIN_PASSWORD")
                {
                    newPassword = "Admin@123";
                }
                else if (user.PasswordHash == "TEMP_PASSWORD")
                {
                    newPassword = user.Role switch
                    {
                        "EMPLOYEE" => "Employee@123",
                        "CUSTOMER" => "Customer@123",
                        _ => "SmartGym@123"
                    };
                }

                if (newPassword != null)
                {
                    user.PasswordHash =
                        passwordHasher.HashPassword(user, newPassword);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
