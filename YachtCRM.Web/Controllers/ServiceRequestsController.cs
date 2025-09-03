using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Domain;
using YachtCRM.Infrastructure;

namespace YachtCRM.Web.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly YachtCrmDbContext _db;
        public ServiceRequestsController(YachtCrmDbContext db) => _db = db;

        // GET: /ServiceRequests?projectId=#
        public async Task<IActionResult> Index(int? projectId)
        {
            var q = _db.ServiceRequests
                .AsNoTracking()
                .Include(s => s.Project)
                .OrderByDescending(s => s.ServiceRequestID)
                .AsQueryable();

            if (projectId.HasValue)
            {
                q = q.Where(s => s.ProjectID == projectId.Value);
                ViewBag.ProjectId = projectId;
            }

            var list = await q.ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var s = await _db.ServiceRequests
                .Include(x => x.Project)
                .FirstOrDefaultAsync(x => x.ServiceRequestID == id);
            if (s == null) return NotFound();
            return View(s);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projectId)
        {
            await LoadProjects(projectId);
            return View(new ServiceRequest
            {
                ProjectID = projectId ?? 0,
                Status = "Open",
                Type = "Maintenance",
                RequestDate = DateTime.UtcNow
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest model)
        {
            if (!ModelState.IsValid)
            {
                await LoadProjects(model.ProjectID);
                return View(model);
            }

            _db.ServiceRequests.Add(model);
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Service request created.";
            return RedirectAfterSave(model.ProjectID);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var s = await _db.ServiceRequests.FindAsync(id);
            if (s == null) return NotFound();
            await LoadProjects(s.ProjectID);
            return View(s);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceRequest model)
        {
            if (id != model.ServiceRequestID) return BadRequest();
            if (!ModelState.IsValid)
            {
                await LoadProjects(model.ProjectID);
                return View(model);
            }

            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Service request updated.";
            return RedirectAfterSave(model.ProjectID);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _db.ServiceRequests
                .Include(x => x.Project)
                .FirstOrDefaultAsync(x => x.ServiceRequestID == id);
            if (s == null) return NotFound();
            return View(s);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var s = await _db.ServiceRequests.FindAsync(id);
            if (s == null) return NotFound();
            var projId = s.ProjectID;

            _db.ServiceRequests.Remove(s);
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Service request deleted.";
            return RedirectAfterSave(projId);
        }

        private async Task LoadProjects(int? selectedId = null)
        {
            var items = await _db.Projects.AsNoTracking()
                .OrderBy(p => p.ProjectID)
                .Select(p => new { p.ProjectID, p.Name })
                .ToListAsync();
            ViewBag.Projects = new SelectList(items, "ProjectID", "Name", selectedId);
        }

        private IActionResult RedirectAfterSave(int projectId)
        {
            // If coming from a project page, go back there; else index.
            if (projectId > 0) return RedirectToAction("Details", "Projects", new { id = projectId });
            return RedirectToAction(nameof(Index));
        }
    }
}
