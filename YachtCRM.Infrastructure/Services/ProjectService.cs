using Microsoft.EntityFrameworkCore;
using YachtCRM.Application.Interfaces;
using YachtCRM.Domain;

namespace YachtCRM.Infrastructure.Services
{
    public class ProjectService : IProjectService
    {
        private readonly YachtCrmDbContext _db;
        public ProjectService(YachtCrmDbContext db) => _db = db;

        public Task<List<Project>> ListAsync() =>
            _db.Projects
               .Include(p => p.Customer)
               .Include(p => p.YachtModel)
               .OrderByDescending(p => p.ProjectID)
               .ToListAsync();

        public Task<Project?> GetAsync(int id) =>
            _db.Projects
               .Include(p => p.Customer)
               .Include(p => p.YachtModel)
               .Include(p => p.Tasks)
               .Include(p => p.ChangeRequests)
               .Include(p => p.Interactions)
               .Include(p => p.Milestones)
               .FirstOrDefaultAsync(p => p.ProjectID == id);
    }
}
