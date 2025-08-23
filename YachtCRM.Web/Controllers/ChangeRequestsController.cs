using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Domain;
using YachtCRM.Infrastructure;

namespace YachtCRM.Web.Controllers
{
    public class ChangeRequestsController : Controller
    {
        private readonly YachtCrmDbContext _db;
        public ChangeRequestsController(YachtCrmDbContext db) => _db = db;

        // List with optional filters
        public async Task<IActionResult> Index(int? customerId, int? projectId)
        {
            var q = _db.ChangeRequests
                       .Include(c => c.Project).ThenInclude(p => p.Customer)
                       .OrderByDescending(c => c.ApprovedOn)
                       .AsQueryable();

            if (customerId.HasValue) q = q.Where(c => c.Project.CustomerID == customerId.Value);
            if (projectId.HasValue)  q = q.Where(c => c.ProjectID == projectId.Value);

            ViewBag.CustomerId = customerId;
            ViewBag.ProjectId  = projectId;

            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var cr = await _db.ChangeRequests
                              .Include(c => c.Project).ThenInclude(p => p.Customer)
                              .FirstOrDefaultAsync(c => c.ChangeRequestID == id);
            if (cr == null) return NotFound();
            return View(cr);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projectId)
        {
            await PopulateProjects(projectId);
            return View(new ChangeRequest {
                ProjectID = projectId ?? 0,
                Approved = true,
                ApprovedOn = DateTime.UtcNow
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChangeRequest model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateProjects(model.ProjectID);
                return View(model);
            }

            // Normalize ApprovedOn to UTC
            if (model.Approved && model.ApprovedOn == default)
                model.ApprovedOn = DateTime.UtcNow;

            _db.ChangeRequests.Add(model);
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Change request created.";
            return RedirectToAction(nameof(Details), new { id = model.ChangeRequestID });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var cr = await _db.ChangeRequests.FindAsync(id);
            if (cr == null) return NotFound();
            await PopulateProjects(cr.ProjectID);
            return View(cr);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChangeRequest model)
        {
            if (id != model.ChangeRequestID) return BadRequest();
            if (!ModelState.IsValid)
            {
                await PopulateProjects(model.ProjectID);
                return View(model);
            }

            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Change request updated.";
            return RedirectToAction(nameof(Details), new { id = model.ChangeRequestID });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var cr = await _db.ChangeRequests
                              .Include(c => c.Project).ThenInclude(p => p.Customer)
                              .FirstOrDefaultAsync(c => c.ChangeRequestID == id);
            if (cr == null) return NotFound();
            return View(cr);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cr = await _db.ChangeRequests.FindAsync(id);
            if (cr == null) return NotFound();

            _db.ChangeRequests.Remove(cr);
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Change request deleted.";
            return RedirectToAction(nameof(Index), new { projectId = cr.ProjectID });
        }

        private async Task PopulateProjects(int? projectId)
        {
            var projects = await _db.Projects
                                    .Include(p => p.Customer)
                                    .OrderBy(p => p.Customer.Name)
                                    .ThenBy(p => p.Name)
                                    .ToListAsync();

            // Show as "Customer – Project"
            var items = projects.Select(p => new SelectListItem {
                Value = p.ProjectID.ToString(),
                Text = $"{p.Customer.Name} — {p.Name}"
            }).ToList();

            ViewBag.Projects = new SelectList(items, "Value", "Text", projectId);
        }
    }
}
