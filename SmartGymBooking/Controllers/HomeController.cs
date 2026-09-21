using Microsoft.AspNetCore.Mvc;

namespace SmartGymBooking.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
