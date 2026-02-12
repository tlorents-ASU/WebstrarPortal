using Microsoft.AspNetCore.Mvc;

namespace WebstrarPortal.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        // Keep it simple for now: just prove the app is alive
        return Content(
            "WebstrarPortal is running. Try /portal/deploy/test?asurite=tlorents&page=Page0",
            "text/plain"
        );
    }
}
