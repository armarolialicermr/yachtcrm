using YachtCRM.Infrastructure;
using YachtCRM.Domain;

namespace YachtCRM.Web.Seed
{
    public static class RealShipyardsSeeder
    {
        /// <summary>
        /// Seeds realistic synthetic data with controlled correlations for regression:
        /// - Tasks ~ alphaTasksPerMeter * length + noise
        /// - DelayDays ~ betaDaysPerCR * CRs + gammaCustom + noise
        /// - Custom projects have higher CRs, price uplift, and added delay
        /// </summary>
        public static void Seed(
            YachtCrmDbContext db,
            int count = 500,
            double alphaTasksPerMeterMin = 0.6,   // tasks per meter (min)
            double alphaTasksPerMeterMax = 0.8,   // tasks per meter (max)
            double betaDaysPerCRMin      = 2.5,   // days added per CR (min)
            double betaDaysPerCRMax      = 3.5,   // days added per CR (max)
            int    gammaCustomMin        = 20,    // extra days if custom (min)
            int    gammaCustomMax        = 45,    // extra days if custom (max)
            double priceUpliftCustom     = 0.18,  // +18% for custom
            int    minPlannedDays        = 120,   // 4 months
            int    maxPlannedDays        = 480    // 16 months
        )
        {
            var rand = new Random();

            // --- ensure customers (multi-tenant SaaS look) ---
            var yardNames = new[]
            {
                "Ferretti Group", "Azimut|Benetti", "Sanlorenzo S.p.A.", "CRN Ancona",
                "Baglietto", "Riva", "Perini Navi", "Custom Line", "Cantiere del Pardo",
                "Cantiere Rossini", "Codecasa", "Palumbo Superyachts", "ISA Yachts",
                "Cantieri di Sarnico", "Mondo Marine", "Overmarine (Mangusta)"
            };

            if (!db.Customers.Any())
            {
                foreach (var name in yardNames)
                {
                    db.Customers.Add(new Customer
                    {
                        Name = name,
                        Email = $"info@{Slug(name)}.it",
                        Country = "Italy"
                    });
                }
                db.SaveChanges();
            }

            var customers = db.Customers.ToList();
            var models    = db.YachtModels.ToList();
            if (!customers.Any() || !models.Any()) return;

            // --- name pools ---
            var adjectives = new[] { "Aurora", "Luna", "Tridente", "Sirena", "Maestrale", "Eolo", "Nettuno", "Stella", "Aria", "Onda", "Vento", "Levante", "Scirocco", "Aliseo" };
            var series     = new[] { "45", "52", "58", "63", "70", "78", "90", "110", "130" };

            for (int i = 0; i < count; i++)
            {
                var cust  = customers[rand.Next(customers.Count)];
                var model = models[rand.Next(models.Count)];

                var isCustom = rand.NextDouble() < 0.28; // ~28% custom orders

                // pick coefficients per project (adds natural variation)
                var alphaTasksPerMeter = Lerp(alphaTasksPerMeterMin, alphaTasksPerMeterMax, rand.NextDouble());
                var betaDaysPerCR      = Lerp(betaDaysPerCRMin,      betaDaysPerCRMax,      rand.NextDouble());
                var gammaCustom        = rand.Next(gammaCustomMin, gammaCustomMax + 1);

                var start           = DateTime.UtcNow.AddDays(-rand.Next(900)); // ~2.5 years back
                var plannedDuration = rand.Next(minPlannedDays, maxPlannedDays + 1);

                // --- change requests: scale with length + “custom” uplift + noise ---
                var crBase = (int)Math.Round((double)(model.Length / 12m)); // 30m => ~2–3 CRs
                if (isCustom) crBase += 2;                                  // custom adds CR pressure
                var crCount = Math.Clamp(crBase + rand.Next(-1, 4), 0, 16);

                // --- tasks: scale with length, plus small noise ---
                var meanTasks = alphaTasksPerMeter * (double)model.Length;  // e.g., 60m * 0.7 => 42 tasks
                var taskCount = (int)Math.Round(meanTasks + Normal(rand, 0, 4));
                taskCount = Math.Clamp(taskCount, 10, 140);

                // --- interactions scale with tasks and CRs ---
                var interCount = Math.Clamp(taskCount / 10 + crCount + rand.Next(-2, 4), 0, 40);

                // --- pricing ---
                var priceUplift = isCustom ? priceUpliftCustom : (0.02 + rand.NextDouble() * 0.20); // 2%–22% for standard
                var totalPrice  = model.BasePrice * (decimal)(1.00 + priceUplift);

                // --- construct “actual duration” so regressions are crisp ---
                // delayDays = beta * CRs + gammaCustom(if custom) + noise
                var noiseDays  = (int)Math.Round(Normal(rand, mean: 0, stdDev: 10)); // symmetric noise
                var delayDays  = (int)Math.Round(betaDaysPerCR * crCount) + (isCustom ? gammaCustom : 0) + noiseDays;
                delayDays      = Math.Max(delayDays, -20); // allow some early finishes but cap

                var actualDuration = plannedDuration + delayDays;

                // --- project entity ---
                var name = $"Project {adjectives[rand.Next(adjectives.Length)]} {series[rand.Next(series.Length)]}-{rand.Next(10, 99)}";
                var proj = new Project
                {
                    Name          = isCustom ? name + " Custom" : name,
                    CustomerID    = cust.CustomerID,
                    YachtModelID  = model.ModelID,
                    TotalPrice    = totalPrice,
                    PlannedStart  = start,
                    PlannedEnd    = start.AddDays(plannedDuration),
                    ActualStart   = start.AddDays(rand.Next(0, 20)),
                    ActualEnd     = start.AddDays(actualDuration),
                    Status        = StatusFromProgress(rand, plannedDuration, actualDuration, start),
                    // You can store IsCustom via naming convention (already used) or add a bool field in your Domain if desired.
                };

                db.Projects.Add(proj);

                // --- tasks ---
                for (int t = 0; t < taskCount; t++)
                {
                    db.Tasks.Add(new CrmTask
                    {
                        Project = proj,
                        Title   = $"Task {t + 1}",
                        DueDate = start.AddDays(rand.Next(plannedDuration))
                    });
                }

                // --- change requests ---
                for (int c = 0; c < crCount; c++)
                {
                    db.ChangeRequests.Add(new ChangeRequest
                    {
                        Project     = proj,
                        Title       = $"CR {c + 1}" + (isCustom && c < 2 ? " (custom option)" : ""),
                        CostImpact  = (decimal)(3000 + rand.NextDouble() * 65000),
                        DaysImpact  = Math.Clamp((int)Math.Round(Normal(rand, 6, 4)), 1, 30), // avg around 6d
                        Approved    = true,
                        ApprovedOn  = DateTime.UtcNow.AddDays(-rand.Next(365))
                    });
                }

                // --- interactions ---
                for (int k = 0; k < interCount; k++)
                {
                    db.Interactions.Add(new Interaction
                    {
                        Project     = proj,
                        CustomerID  = cust.CustomerID,
                        Type        = WeightedPick(rand, ("Meeting", 0.5), ("Email", 0.3), ("Call", 0.2)),
                        Notes       = $"Interaction {k + 1}"
                    });
                }
            }

            db.SaveChanges();

            // --------- helpers ---------
            static string Slug(string s) =>
                new string(s.ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch == ' ').ToArray()).Replace(' ', '-');

            static double Lerp(double a, double b, double t) => a + (b - a) * t;

            static string StatusFromProgress(Random r, int planned, int actual, DateTime start)
            {
                var elapsed = (DateTime.UtcNow - start).TotalDays;
                if (elapsed < planned * 0.15) return "Planning";
                if (elapsed < actual * 0.9)  return "InProgress";
                return "Completed";
            }

            // Box–Muller transform for normal noise
            static double Normal(Random r, double mean = 0.0, double stdDev = 1.0)
            {
                var u1 = 1.0 - r.NextDouble();
                var u2 = 1.0 - r.NextDouble();
                var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                return mean + stdDev * randStdNormal;
            }

            static string WeightedPick(Random r, params (string value, double weight)[] options)
            {
                var sum = options.Sum(o => o.weight);
                var x = r.NextDouble() * sum;
                double acc = 0;
                foreach (var o in options)
                {
                    acc += o.weight;
                    if (x <= acc) return o.value;
                }
                return options.Last().value;
            }
        }
    }
}

