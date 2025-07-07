using Microsoft.AspNetCore.Mvc;

namespace EcoConnect_Hanoi.Controllers;

public class CommunityItemManagementController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}