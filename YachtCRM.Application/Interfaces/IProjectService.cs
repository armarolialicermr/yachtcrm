using YachtCRM.Domain;

namespace YachtCRM.Application.Interfaces
{
    public interface IProjectService
    {
        Task<List<Project>> ListAsync();
        Task<Project?> GetAsync(int id);
    }
}
