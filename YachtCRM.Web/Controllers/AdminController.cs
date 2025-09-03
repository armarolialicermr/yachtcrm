using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Authorization; // uncomment if you add [Authorize]
using YachtCRM.Infrastructure;
using YachtCRM.Web.Seed;
using YachtCRM.Infrastructure.Services;


namespace YachtCRM.Web.Controllers
{
    // In production add: [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly YachtCrmDbContext _db;
        public AdminController(YachtCrmDbContext db) => _db = db;

        // POST /admin/seed/big  (triggered by the dashboard button)
        [HttpPost("/admin/seed/big")]
        [ValidateAntiForgeryToken] // add @Html.AntiForgeryToken() in the form
        public async Task<IActionResult> SeedBig([FromQuery] int count = 500, [FromQuery] int startYear = 2022)
        {
            await BigSeeder.GenerateAsync(_db, count, startYear);
            TempData["Flash"] = $"Seeded {count} synthetic projects.";
            return RedirectToAction("Index", "Home");
        }
        // POST /admin/train/delayrf
        [HttpPost("/admin/train/delayrf")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> TrainDelayRf([FromServices] MlDelayPredictionService trainer)
{
    var count = await trainer.TrainAsync();
    TempData["Flash"] = $"Trained delay model on {count} projects.";
    return RedirectToAction("Index", "Home");
}


    }
}



