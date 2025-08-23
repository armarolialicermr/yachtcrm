using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Domain;
using YachtCRM.Infrastructure;

namespace YachtCRM.Web.Controllers
{
    public class InteractionsController : Controller
    {
        private readonly YachtCrmDbContext _db;
        public InteractionsController(YachtCrmDbContext db) => _db = db;

        public async Task<IActionResult> Index(int? customerId, int? projectId)
        {
            var q = _db.Interactions
                .Include(i => i.Customer)
                .Include(i => i.Project)
                .OrderByDescending(i => i.OccurredOn)
                .AsQueryable();

            if (customerId.HasValue) q = q.Where(i => i.CustomerID == customerId.Value);
            if (projectId.HasValue)  q = q.Where(i => i.ProjectID == projectId.Value);

            ViewBag.CustomerId = customerId;
            ViewBag.ProjectId  = projectId;

            return View(await q.Take(200).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId, int? projectId)
        {
            await PopulateSelects(customerId, projectId);
            return View(new Interaction {
                CustomerID = customerId ?? 0,
                ProjectID  = projectId,
                Type = "Meeting",
                OccurredOn = DateTime.UtcNow
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Interaction model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelects(model.CustomerID, model.ProjectID);
                return View(model);
            }

            // Normalize time to UTC if user typed local
            model.OccurredOn = DateTime.SpecifyKind(model.OccurredOn, DateTimeKind.Utc);

            _db.Interactions.Add(model);
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Interaction created.";
            return RedirectToAction(nameof(Index), new { customerId = model.CustomerID });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var i = await _db.Interactions.FindAsync(id);
            if (i == null) return NotFound();
            await PopulateSelects(i.CustomerID, i.ProjectID);
            return View(i);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Interaction model)
        {
            if (id != model.InteractionID) return BadRequest();
            if (!ModelState.IsValid)
            {
                await PopulateSelects(model.CustomerID, model.ProjectID);
                return View(model);
            }

            model.OccurredOn = DateTime.SpecifyKind(model.OccurredOn, DateTimeKind.Utc);

            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Interaction updated.";
            return RedirectToAction(nameof(Index), new { customerId = model.CustomerID });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var i = await _db.Interactions
                .Include(x => x.Customer)
                .Include(x => x.Project)
                .FirstOrDefaultAsync(x => x.InteractionID == id);
            if (i == null) return NotFound();
            return View(i);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var i = await _db.Interactions.FindAsync(id);
            if (i == null) return NotFound();

            var custId = i.CustomerID;
            _db.Interactions.Remove(i);
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Interaction deleted.";
            return RedirectToAction(nameof(Index), new { customerId = custId });
        }

        private async Task PopulateSelects(int? customerId, int? projectId)
        {
            ViewBag.Customers = new SelectList(await _db.Customers.OrderBy(c => c.Name).ToListAsync(),
                "CustomerID", "Name", customerId);

            var projs = Enumerable.Empty<Project>().ToList();
            if (customerId.HasValue && customerId.Value > 0)
            {
                projs = await _db.Projects
                    .Where(p => p.CustomerID == customerId.Value)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            ViewBag.Projects = new SelectList(projs, "ProjectID", "Name", projectId);
        }
    }
}
