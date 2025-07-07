using Microsoft.AspNetCore.Mvc;

namespace EcoConnect_Hanoi.Controllers;

public class RecyclingCategoriesController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}