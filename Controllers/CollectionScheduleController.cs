using Microsoft.AspNetCore.Mvc;

namespace EcoConnect_Hanoi.Controllers;

public class CollectionScheduleController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}