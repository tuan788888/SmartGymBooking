using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartGymBooking.Controllers;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorController : Controller
{
    [Route("/Error/StatusCode/{code:int}")]
    public IActionResult StatusCodePage(int code)
    {
        code = code is >= 400 and <= 599 ? code : 404;
        Response.StatusCode = code;
        ViewData["StatusCode"] = code;
        return View(code switch { 404 => "NotFound", 403 => "AccessDenied", _ => "Error" });
    }

    [Route("/Error/ServerError")]
    public IActionResult ServerError()
    {
        Response.StatusCode = 500;
        ViewData["StatusCode"] = 500;
        return View("Error");
    }
}
