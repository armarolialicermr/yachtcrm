using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Domain;
using YachtCRM.Infrastructure;

namespace YachtCRM.Web.Controllers
{
    public class CustomersController : Controller
    {
        private readonly YachtCrmDbContext _db;
        public CustomersController(YachtCrmDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var customers = await _db.Customers
                .Include(c => c.Projects)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(customers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var c = await _db.Customers
                .Include(c => c.Projects).ThenInclude(p => p.YachtModel)
                .Include(c => c.Interactions)
                .FirstOrDefaultAsync(c => c.CustomerID == id);
            if (c == null) return NotFound();
            return View(c);
        }

        [HttpGet]
        public IActionResult Create() => View(new Customer());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Customers.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = model.CustomerID });
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c == null) return NotFound();
            return View(c);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer model)
        {
            if (id != model.CustomerID) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Customer updated.";
            return RedirectToAction(nameof(Details), new { id = model.CustomerID });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Customers.FirstOrDefaultAsync(x => x.CustomerID == id);
            if (c == null) return NotFound();
            return View(c);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c == null) return NotFound();

            // Optional: guard if there are projects
            var hasProjects = await _db.Projects.AnyAsync(p => p.CustomerID == id);
            if (hasProjects)
            {
                TempData["Flash"] = "Cannot delete: customer has projects.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _db.Customers.Remove(c);
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Customer deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}

