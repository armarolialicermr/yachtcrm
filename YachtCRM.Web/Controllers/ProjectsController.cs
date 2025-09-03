using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Application.Interfaces;
using YachtCRM.Domain;               // <-- needed for Interaction, ChangeRequest, etc.
using YachtCRM.Infrastructure;

namespace YachtCRM.Web.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly IProjectService _svc;
        private readonly IPredictionService _pred;
        private readonly YachtCrmDbContext _db;

        public ProjectsController(IProjectService svc, IPredictionService pred, YachtCrmDbContext db)
        {
            _svc = svc;
            _pred = pred;
            _db = db;
        }

        // /Projects?status=InProgress&q=aurora&customerId=12&page=1&pageSize=25
        public async Task<IActionResult> Index(string? status, string? q, int? customerId, int page = 1, int pageSize = 25)
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
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
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
            var project = await _db.Projects
                .Include(p => p.Customer)
                .Include(p => p.YachtModel)
                .Include(p => p.Tasks)
                .Include(p => p.ChangeRequests)
                .Include(p => p.Interactions)
                .Include(p => p.ServiceRequests)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (project == null) return NotFound();

            // Latest 5 feedback rows for the project
            var latestFb = await _db.CustomerFeedbacks
                .Where(f => f.ProjectID == id)
                .OrderByDescending(f => f.SubmittedOn)
                .Take(5)
                .ToListAsync();

            ViewBag.LatestFeedback = latestFb;

            return View(project);
        }

        // JSON prediction used by the Details view
        [HttpGet("/projects/{id:int}/prediction")]
        public async Task<IActionResult> Prediction(int id)
        {
            var p = await _db.Projects
                .Include(x => x.YachtModel)
                .Include(x => x.Tasks)
                .Include(x => x.ChangeRequests)
                .Include(x => x.Interactions)
                .FirstOrDefaultAsync(x => x.ProjectID == id);

            if (p == null || p.YachtModel == null)
                return NotFound(new { error = "Project not found" });

            var days = _pred.PredictDelayDays(
                (float)p.YachtModel.Length,
                (float)p.YachtModel.BasePrice,
                p.Tasks.Count,
                p.ChangeRequests.Count,
                p.Interactions.Count
            );

            return Json(new { predictedDelayDays = days });
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





