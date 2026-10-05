using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "EMPLOYEE")]
    public class EmployeeDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
