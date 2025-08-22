using Microsoft.AspNetCore.Mvc;

namespace YachtCRM.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
