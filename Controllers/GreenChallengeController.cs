using Microsoft.AspNetCore.Mvc;

namespace EcoConnect_Hanoi.Controllers;

public class GreenChallengeController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}