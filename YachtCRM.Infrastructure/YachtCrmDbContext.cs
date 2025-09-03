using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Domain;

namespace YachtCRM.Infrastructure
{
    public class YachtCrmDbContext : IdentityDbContext
    {
        public YachtCrmDbContext(DbContextOptions<YachtCrmDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<YachtModel> YachtModels => Set<YachtModel>();
        public DbSet<CrmTask> Tasks => Set<CrmTask>();
        public DbSet<Interaction> Interactions => Set<Interaction>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
        public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
        public DbSet<ServiceTask> ServiceTasks => Set<ServiceTask>();
        public DbSet<Broker> Brokers => Set<Broker>();
        public DbSet<CustomerBroker> CustomerBrokers => Set<CustomerBroker>();
        public DbSet<Yard> Yards => Set<Yard>();
        public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
        public DbSet<CustomerFeedback> CustomerFeedbacks => Set<CustomerFeedback>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ---------- Value precision ----------
            builder.Entity<Project>().Property(p => p.TotalPrice).HasPrecision(18, 2);
            builder.Entity<YachtModel>().Property(y => y.BasePrice).HasPrecision(18, 2);
            builder.Entity<YachtModel>().Property(y => y.Length).HasPrecision(7, 2);
            builder.Entity<YachtModel>().Property(y => y.Beam).HasPrecision(7, 2);
            builder.Entity<YachtModel>().Property(y => y.Draft).HasPrecision(7, 2);

            // ---------- Relationships ----------
            builder.Entity<Project>()
                .HasOne(p => p.Customer).WithMany(c => c.Projects)
                .HasForeignKey(p => p.CustomerID).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasOne(p => p.YachtModel).WithMany(y => y.Projects)
                .HasForeignKey(p => p.YachtModelID).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CrmTask>()
                .HasOne(t => t.Customer).WithMany(c => c.Tasks)
                .HasForeignKey(t => t.CustomerID).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CrmTask>()
                .HasOne(t => t.Project).WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectID).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Interaction>()
                .HasOne(i => i.Customer).WithMany(c => c.Interactions)
                .HasForeignKey(i => i.CustomerID).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Interaction>()
                .HasOne(i => i.Project).WithMany(p => p.Interactions)
                .HasForeignKey(i => i.ProjectID).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectMilestone>()
                .HasOne(m => m.Project).WithMany(p => p.Milestones)
                .HasForeignKey(m => m.ProjectID).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChangeRequest>()
                .HasOne(cr => cr.Project).WithMany(p => p.ChangeRequests)
                .HasForeignKey(cr => cr.ProjectID).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ServiceTask>()
                .HasOne(st => st.Customer).WithMany(c => c.ServiceTasks)
                .HasForeignKey(st => st.CustomerID).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ServiceTask>()
                .HasOne(st => st.Project).WithMany(p => p.ServiceTasks)
                .HasForeignKey(st => st.ProjectID).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomerBroker>().HasKey(cb => new { cb.CustomerID, cb.BrokerID });
            builder.Entity<CustomerBroker>()
                .HasOne(cb => cb.Customer).WithMany(c => c.CustomerBrokers)
                .HasForeignKey(cb => cb.CustomerID);
            builder.Entity<CustomerBroker>()
                .HasOne(cb => cb.Broker).WithMany(b => b.CustomerBrokers)
                .HasForeignKey(cb => cb.BrokerID);

            // ---------- Seed: Yacht Models ----------
            builder.Entity<YachtModel>().HasData(
                new YachtModel { ModelID = 1, ModelName = "Ocean Explorer 45", YachtType = "Motor Yacht", Length = 45.0m, Beam = 14.0m, Draft = 4.2m, BasePrice = 850000m, Description = "Luxury motor yacht for exploration" },
                new YachtModel { ModelID = 2, ModelName = "Wind Dancer 55", YachtType = "Sailing Yacht", Length = 55.0m, Beam = 16.0m, Draft = 2.8m, BasePrice = 1200000m, Description = "High-performance sailing yacht" },
                new YachtModel { ModelID = 3, ModelName = "Sport Fisher 38", YachtType = "Sport Fisher", Length = 38.0m, Beam = 12.5m, Draft = 3.5m, BasePrice = 650000m, Description = "Professional sport fishing vessel" }
            );

            // ---------- NEW: Yards ----------
            builder.Entity<Yard>(e =>
            {
                e.HasKey(x => x.YardID);
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Country).HasMaxLength(100).IsRequired();
                e.Property(x => x.Brand).HasMaxLength(100).IsRequired();
                e.HasIndex(x => new { x.Name, x.Brand }).IsUnique();

                e.HasData(
                    new Yard { YardID = 1, Name = "Cantieri Demo S.p.A.", Country = "Italy", Brand = "Cantieri Demo" },
                    new Yard { YardID = 2, Name = "Ferretti Group", Country = "Italy", Brand = "Ferretti" },
                    new Yard { YardID = 3, Name = "Azimut|Benetti", Country = "Italy", Brand = "Azimut" },
                    new Yard { YardID = 4, Name = "Sanlorenzo", Country = "Italy", Brand = "Sanlorenzo" },
                    new Yard { YardID = 5, Name = "Feadship", Country = "Netherlands", Brand = "Feadship" },
                    new Yard { YardID = 6, Name = "Lürssen", Country = "Germany", Brand = "Lürssen" }
                );
            });

            // Project → Yard (nullable FK so existing rows remain valid)
            builder.Entity<Project>()
                .HasOne(p => p.Yard)
                .WithMany(y => y.Projects)
                .HasForeignKey(p => p.YardID)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- NEW: ServiceRequest ----------
            builder.Entity<ServiceRequest>(e =>
            {
                e.HasKey(x => x.ServiceRequestID);
                e.Property(x => x.Type).HasMaxLength(50).HasDefaultValue("Maintenance");
                e.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("Open");
                e.Property(x => x.RequestDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.HasIndex(x => new { x.ProjectID, x.Status });

                e.HasOne(x => x.Project)
                    .WithMany(p => p.ServiceRequests)
                    .HasForeignKey(x => x.ProjectID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- NEW: CustomerFeedback ----------
            builder.Entity<CustomerFeedback>(e =>
            {
                e.HasKey(x => x.CustomerFeedbackID);
                e.Property(x => x.Score);
                e.Property(x => x.SubmittedOn).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.HasIndex(x => new { x.CustomerID, x.ProjectID });

                // If your Customer class doesn't have ICollection<CustomerFeedback> Feedbacks,
                // change WithMany(c => c.Feedbacks) to just .WithMany()
                e.HasOne(x => x.Customer)
                    .WithMany() // .WithMany(c => c.Feedbacks) if you added the nav
                    .HasForeignKey(x => x.CustomerID)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Project)
                    .WithMany() // or add a nav collection in Project if you prefer
                    .HasForeignKey(x => x.ProjectID)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
