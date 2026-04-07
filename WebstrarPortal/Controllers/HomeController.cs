using Microsoft.AspNetCore.Mvc;

namespace WebstrarPortal.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        return Redirect("/portal");
    }
}
