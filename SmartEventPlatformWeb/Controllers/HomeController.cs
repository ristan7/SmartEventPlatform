using Microsoft.AspNetCore.Mvc;
using SmartEventPlatformWeb.Models;
using System.Diagnostics;

namespace SmartEventPlatformWeb.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

    }
}
