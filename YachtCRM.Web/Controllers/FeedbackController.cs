using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Domain;
using YachtCRM.Infrastructure;

namespace YachtCRM.Web.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly YachtCrmDbContext _db;
        public FeedbackController(YachtCrmDbContext db) => _db = db;

        // GET: /Feedback?projectId=#&customerId=#
        public async Task<IActionResult> Index(int? projectId, int? customerId)
        {
            var q = _db.CustomerFeedbacks
                .AsNoTracking()
                .Include(f => f.Project)
                .Include(f => f.Customer)
                .OrderByDescending(f => f.CustomerFeedbackID)
                .AsQueryable();

            if (projectId.HasValue)
            {
                q = q.Where(f => f.ProjectID == projectId.Value);
                ViewBag.ProjectId = projectId;
            }
            if (customerId.HasValue)
            {
                q = q.Where(f => f.CustomerID == customerId.Value);
                ViewBag.CustomerId = customerId;
            }

            var list = await q.ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var f = await _db.CustomerFeedbacks
                .Include(x => x.Project)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.CustomerFeedbackID == id);
            if (f == null) return NotFound();
            return View(f);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projectId, int customerId)
        {
            await LoadDropdowns(projectId, customerId);
            return View(new CustomerFeedback
            {
                ProjectID = projectId,
                CustomerID = customerId,
                SubmittedOn = DateTime.UtcNow
            });
        }

        // example around your POST Create/Edit action

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerFeedback model, int? customerId, int? projectId)
        {
            model.CustomerID = customerId ?? model.CustomerID;
            model.ProjectID  = projectId  ?? model.ProjectID;

            if (model.CustomerID == 0)
                ModelState.AddModelError(nameof(model.CustomerID), "Customer is required.");
            if (model.ProjectID == 0)
                ModelState.AddModelError(nameof(model.ProjectID), "Project is required.");

            if (!ModelState.IsValid) return View(model);

            _db.CustomerFeedbacks.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = model.CustomerFeedbackID });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var f = await _db.CustomerFeedbacks.FindAsync(id);
            if (f == null) return NotFound();
            await LoadDropdowns(f.ProjectID, f.CustomerID);
            return View(f);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerFeedback model)
        {
            if (id != model.CustomerFeedbackID) return BadRequest();
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model.ProjectID, model.CustomerID);
                return View(model);
            }
            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Feedback updated.";
            return RedirectAfterSave(model.ProjectID, model.CustomerID);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var f = await _db.CustomerFeedbacks
                .Include(x => x.Project)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.CustomerFeedbackID == id);
            if (f == null) return NotFound();
            return View(f);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var f = await _db.CustomerFeedbacks.FindAsync(id);
            if (f == null) return NotFound();

            var projectId = f.ProjectID;
            var customerId = f.CustomerID;

            _db.CustomerFeedbacks.Remove(f);
            await _db.SaveChangesAsync();
            TempData["Flash"] = "Feedback deleted.";
            return RedirectAfterSave(projectId, customerId);
        }

        private async Task LoadDropdowns(int? projectId, int? customerId)
        {
            var projects = await _db.Projects.AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new { p.ProjectID, p.Name })
                .ToListAsync();

            var customers = await _db.Customers.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.CustomerID, c.Name })
                .ToListAsync();

            ViewBag.Projects = new SelectList(projects, "ProjectID", "Name", projectId);
            ViewBag.Customers = new SelectList(customers, "CustomerID", "Name", customerId);
        }

        private IActionResult RedirectAfterSave(int? projectId, int? customerId)
        {
            if (projectId.HasValue) return RedirectToAction("Details", "Projects", new { id = projectId.Value });
            if (customerId.HasValue) return RedirectToAction(nameof(Index), new { customerId = customerId.Value });
            return RedirectToAction(nameof(Index));
        }
    }
}


