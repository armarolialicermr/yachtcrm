using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace YachtCRM.Infrastructure
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<YachtCrmDbContext>
    {
        public YachtCrmDbContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "YachtCRM.Web");
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = config.GetConnectionString("DefaultConnection") ?? "Data Source=YachtCrm.db";
            var options = new DbContextOptionsBuilder<YachtCrmDbContext>().UseSqlite(cs).Options;
            return new YachtCrmDbContext(options);
        }
    }
}
