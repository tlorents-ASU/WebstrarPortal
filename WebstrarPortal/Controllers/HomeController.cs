using Microsoft.AspNetCore.Mvc;

namespace WebstrarPortal.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        return Redirect("/portal");
    }

    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok("healthy");
    }
}
