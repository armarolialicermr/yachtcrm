using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Application.Interfaces;
using YachtCRM.Domain;
using YachtCRM.Infrastructure;

namespace YachtCRM.Web.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly IProjectService _svc;
        private readonly YachtCrmDbContext _db;

        public ProjectsController(IProjectService svc, YachtCrmDbContext db)
        {
            _svc = svc;
            _db = db;
        }

        // /Projects?status=InProgress&q=aurora&customerId=12&page=1&pageSize=25
        public async Task<IActionResult> Index(
            string? status, string? q, int? customerId, int page = 1, int pageSize = 25)
        {
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5;
            if (pageSize > 200) pageSize = 200;

            var query = _db.Projects
                .AsNoTracking()
                .Include(p => p.Customer)
                .Include(p => p.YachtModel)
                .AsQueryable();

            if (customerId.HasValue)
            {
                query = query.Where(p => p.CustomerID == customerId.Value);
                ViewBag.CustomerId = customerId;
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                query = query.Where(p => (p.Status ?? "").ToLower() == s.ToLower());
                ViewBag.FilterStatus = s;
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p =>
                    EF.Functions.Like(p.Name, $"%{q}%") ||
                    (p.Customer != null && EF.Functions.Like(p.Customer.Name, $"%{q}%")) ||
                    (p.YachtModel != null && EF.Functions.Like(p.YachtModel.ModelName, $"%{q}%")));
                ViewBag.Query = q;
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(p => p.ProjectID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = totalPages;

            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var project = await _svc.GetAsync(id);
            if (project == null) return NotFound();
            return View(project);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = new SelectList(
                await _db.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
                "CustomerID", "Name");

            ViewBag.YachtModels = new SelectList(
                await _db.YachtModels.AsNoTracking().OrderBy(m => m.ModelName).ToListAsync(),
                "ModelID", "ModelName");

            return View(new Project
            {
                PlannedStart = DateTime.UtcNow.Date,
                PlannedEnd = DateTime.UtcNow.Date.AddDays(120),
                Status = "Planning"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = new SelectList(
                    await _db.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
                    "CustomerID", "Name", model.CustomerID);

                ViewBag.YachtModels = new SelectList(
                    await _db.YachtModels.AsNoTracking().OrderBy(m => m.ModelName).ToListAsync(),
                    "ModelID", "ModelName", model.YachtModelID);

                return View(model);
            }

            _db.Projects.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = model.ProjectID });
        }

        // ---------- QUICK ADD ENDPOINTS ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAddInteraction(int id, string? type, string? notes)
        {
            var proj = await _db.Projects.FindAsync(id);
            if (proj == null) return NotFound();

            _db.Interactions.Add(new Interaction
            {
                CustomerID = proj.CustomerID,
                ProjectID = id,
                Type = string.IsNullOrWhiteSpace(type) ? "Meeting" : type,
                Notes = notes
            });

            await _db.SaveChangesAsync();
            TempData["Flash"] = "Interaction added.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAddChange(int id, string title, int? daysImpact, decimal? costImpact)
        {
            var proj = await _db.Projects.FindAsync(id);
            if (proj == null) return NotFound();

            _db.ChangeRequests.Add(new ChangeRequest
            {
                ProjectID = id,
                Title = string.IsNullOrWhiteSpace(title) ? "Change request" : title,
                DaysImpact = daysImpact,
                CostImpact = costImpact,
                Approved = true,
                ApprovedOn = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            TempData["Flash"] = "Change request added.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}




