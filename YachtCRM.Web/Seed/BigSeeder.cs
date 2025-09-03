using Microsoft.EntityFrameworkCore;
using YachtCRM.Infrastructure;
using YachtCRM.Domain;

namespace YachtCRM.Web.Seed
{
    /// <summary>
    /// Synthetic dataset generator with real-world tendencies:
    /// - Tasks scale with length
    /// - Change Requests drive delays
    /// - Custom builds incur extra variability
    /// - Interactions (good CRM) slightly mitigate delays
    /// - Yard quality, supplier risk, and seasonality add realistic variance
    /// Also seeds ServiceRequests and CustomerFeedback.
    /// </summary>
    public static class BigSeeder
    {
        /// <summary>
        /// Generate N synthetic projects.
        /// - singleCustomer=false (default): spread across multiple synthetic shipyards (customers).
        /// - singleCustomer=true: all projects assigned to singleCustomerName (created if missing).
        /// </summary>
        public static async Task GenerateAsync(
            YachtCrmDbContext db,
            int count = 500,
            int startYear = 2022,
            bool singleCustomer = false,
            string? singleCustomerName = "Cantieri Demo S.p.A.")
        {
            // --- Ensure models exist ---
            if (!await db.YachtModels.AnyAsync())
            {
                db.YachtModels.AddRange(
                    new YachtModel { ModelName = "Ocean Explorer 45", YachtType = "Motor Yacht", Length = 45m, Beam = 14m, Draft = 4.2m, BasePrice = 850000m, Description = "Seed" },
                    new YachtModel { ModelName = "Wind Dancer 55",   YachtType = "Sailing Yacht", Length = 55m, Beam = 16m, Draft = 2.8m, BasePrice = 1200000m, Description = "Seed" },
                    new YachtModel { ModelName = "Sport Fisher 38",  YachtType = "Sport Fisher", Length = 38m, Beam = 12.5m, Draft = 3.5m, BasePrice = 650000m, Description = "Seed" },
                    new YachtModel { ModelName = "Tri-Deck 90",      YachtType = "Mega Yacht",    Length = 90m, Beam = 18m, Draft = 5.2m, BasePrice = 3500000m, Description = "Seed" }
                );
                await db.SaveChangesAsync();
            }

            // --- Ensure customers set ---
            List<Customer> customers;
            if (singleCustomer)
            {
                var name = string.IsNullOrWhiteSpace(singleCustomerName) ? "Cantieri Demo S.p.A." : singleCustomerName!.Trim();
                var solo = await db.Customers.FirstOrDefaultAsync(c => c.Name == name);
                if (solo == null)
                {
                    solo = new Customer { Name = name, Email = "info@demo-yard.it", Country = "Italy" };
                    db.Customers.Add(solo);
                    await db.SaveChangesAsync();
                }
                customers = new List<Customer> { solo };
            }
            else
            {
                var mustHave = new (string Name, string Email, string Country)[]
                {
                    ("Cantieri Demo S.p.A.", "info@demo-yard.it",     "Italy"),
                    ("Azimut (Synthetic)",   "info@azimut.example",   "Italy"),
                    ("Ferretti (Synthetic)", "info@ferretti.example", "Italy"),
                    ("Benetti (Synthetic)",  "info@benetti.example",  "Italy"),
                };

                foreach (var m in mustHave)
                {
                    if (!await db.Customers.AnyAsync(c => c.Name == m.Name))
                        db.Customers.Add(new Customer { Name = m.Name, Email = m.Email, Country = m.Country });
                }
                await db.SaveChangesAsync();

                customers = await db.Customers.ToListAsync();
            }

            var models = await db.YachtModels.ToListAsync();
            var rand   = new Random();

            // Yard-level “quality” and “communication” modifiers (persisted in-memory per run)
            var yardMods = customers.ToDictionary(
                c => c.CustomerID,
                c => new YardMod
                {
                    // small positive means fewer delays; negative means more delays
                    Quality = SampleFromName(c.Name, ("Ferretti", +0.12), ("Cantieri Demo", +0.10), ("Benetti", +0.08), ("Azimut", +0.05)),
                    // comms reduces CR-caused delays a bit (mitigation)
                    Comms   = SampleFromName(c.Name, ("Cantieri Demo", +0.15), ("Benetti", +0.12), ("Ferretti", +0.10), ("Azimut", +0.05))
                });

            string[] adjectives = { "Aurora","Luna","Tridente","Sirena","Maestrale","Eolo","Nettuno","Stella","Aria","Onda","Levante","Scirocco","Aliseo" };
            int[] series        = { 38,45,52,58,63,70,78,90,110 };

            // Coefficients (document in the dissertation)
            const double tasksAlphaPerMeter   = 1.2;
            const double tasksNoiseSigma      = 6.0;
            const int    tasksMin             = 12;
            const int    tasksMax             = 200;

            const double crBaseIntercept      = 0.8;
            const double crPer12m             = 0.9;
            const double crCustomBoost        = 2.2;
            const double crNoiseSigma         = 1.2;

            // Delay model (days)
            const double delayPerCR           = 3.1;
            const double delayPerMeterAbove40 = 0.55;
            const int    customDelayMin       = 18;
            const int    customDelayMax       = 45;
            const double supplierRiskProb     = 0.09;
            const int    supplierDelayMin     = 10;
            const int    supplierDelayMax     = 60;
            const double interactionsMitigate = 0.15; // each 10 interactions ≈ 1.5 days shaved (× quality)
            const double delayNoiseSigma      = 10.0;
            const int    delayFloor           = -25;

            // -------- Create Projects (+ Tasks, CRs, Interactions) --------
            for (int i = 0; i < count; i++)
            {
                var cust  = customers[rand.Next(customers.Count)];
                var mod   = yardMods[cust.CustomerID];
                var model = models[rand.Next(models.Count)];
                bool isCustom = rand.NextDouble() < 0.30;

                var startFrom  = new DateTime(startYear, 1, 1);
                var start      = startFrom.AddDays(rand.Next((int)(DateTime.UtcNow - startFrom).TotalDays));
                var plannedDays = rand.Next(150, 540); // 5–18 months

                // Tasks scale with length (+ noise)
                var tasks = (int)Math.Round(tasksAlphaPerMeter * (double)model.Length + Normal(rand, 0, tasksNoiseSigma));
                tasks = Math.Clamp(tasks, tasksMin, tasksMax);

                // CRs depend on length, custom, + noise
                var crMean = crBaseIntercept
                           + crPer12m * (double)model.Length / 12.0
                           + (isCustom ? crCustomBoost : 0.0)
                           + Normal(rand, 0, crNoiseSigma);
                var crs = Math.Clamp((int)Math.Round(crMean), 0, 20);

                // Interactions
                var interactions = Math.Clamp((int)Math.Round(tasks / 12.0 + crs * 0.6 + 5 * mod.Comms + Normal(rand, 0, 2.0)), 0, 60);

                // Seasonality: summer starts → slightly higher supplier risk
                var seasonBump = (start.Month is >= 6 and <= 9) ? 0.08 : 0.0;
                var supplierHit = rand.NextDouble() < (supplierRiskProb + seasonBump);
                var supplierDelay = supplierHit ? rand.Next(supplierDelayMin, supplierDelayMax + 1) : 0;

                // Core delay
                var delay =
                    delayPerCR * crs
                    + delayPerMeterAbove40 * Math.Max(0.0, (double)model.Length - 40.0)
                    + (isCustom ? rand.Next(customDelayMin, customDelayMax + 1) : 0)
                    + supplierDelay
                    - interactionsMitigate * (interactions / 10.0) * 10.0 * mod.Quality
                    + Normal(rand, 0, delayNoiseSigma);

                var delayDays = Math.Max((int)Math.Round(delay), delayFloor);

                // Price
                var priceMultiplier = isCustom ? 1.22 + rand.NextDouble() * 0.25 : 1.04 + rand.NextDouble() * 0.22;

                // Name
                var name = $"Project {adjectives[rand.Next(adjectives.Length)]} {series[rand.Next(series.Length)]}-{rand.Next(10, 99)}" + (isCustom ? " Custom" : "");

                var proj = new Project
                {
                    Name         = name,
                    CustomerID   = cust.CustomerID,
                    YachtModelID = model.ModelID,
                    TotalPrice   = model.BasePrice * (decimal)priceMultiplier,
                    PlannedStart = start,
                    PlannedEnd   = start.AddDays(plannedDays),
                    ActualStart  = start.AddDays(rand.Next(0, 30)),
                    ActualEnd    = start.AddDays(plannedDays + delayDays),
                    Status       = ComputeStatus(start, plannedDays, delayDays)
                };
                db.Projects.Add(proj);

                // Tasks
                for (int t = 0; t < tasks; t++)
                {
                    db.Tasks.Add(new CrmTask
                    {
                        Project = proj,
                        Title = $"Task {t + 1}",
                        DueDate = start.AddDays(rand.Next(plannedDays))
                    });
                }

                // Change Requests
                for (int c = 0; c < crs; c++)
                {
                    db.ChangeRequests.Add(new ChangeRequest
                    {
                        Project    = proj,
                        Title      = $"CR {c + 1}" + (isCustom && c < 2 ? " (custom option)" : ""),
                        CostImpact = (decimal)(5000 + rand.NextDouble() * 75000),
                        DaysImpact = Math.Clamp((int)Math.Round(Normal(rand, 6, 4)), 1, 32),
                        Approved   = true,
                        ApprovedOn = DateTime.UtcNow.AddDays(-rand.Next(365))
                    });
                }

                // Interactions
                for (int k = 0; k < interactions; k++)
                {
                    db.Interactions.Add(new Interaction
                    {
                        Project    = proj,
                        CustomerID = cust.CustomerID,
                        Type       = RandomType(rand),
                        Notes      = $"Interaction {k + 1}"
                    });
                }
            }

            await db.SaveChangesAsync();

            // -------- Seed Service Requests (up to 3 per first 300 projects) --------
            var srRand = new Random(3);
            var projIds = await db.Projects
                .OrderBy(p => p.ProjectID)
                .Select(p => new { p.ProjectID, p.PlannedStart })
                .ToListAsync();

            foreach (var p in projIds.Take(Math.Min(300, projIds.Count)))
            {
                var n = srRand.Next(0, 4); // up to 3 per project
                for (int i = 0; i < n; i++)
                {
                    var opened = p.PlannedStart.AddDays(srRand.Next(0, 90));
                    DateTime? closed = srRand.NextDouble() < 0.6 ? opened.AddDays(srRand.Next(2, 40)) : null;

                    db.ServiceRequests.Add(new ServiceRequest
                    {
                        ProjectID   = p.ProjectID,
                        Title       = srRand.NextDouble() < 0.5 ? "Warranty claim" : "Maintenance request",
                        Type        = srRand.NextDouble() < 0.5 ? "Warranty" : "Maintenance",
                        Status      = closed == null ? "Open" : "Closed",
                        RequestDate = opened,
                        CompletedOn = closed,
                        Description = "Auto-seeded"
                    });
                }
            }

            // -------- Seed Customer Feedback (skew positive; 15% detractors) --------
            var fbRand = new Random(4);
            var projMeta = await db.Projects
                .OrderBy(p => p.ProjectID)
                .Select(p => new { p.ProjectID, p.CustomerID, End = p.ActualEnd ?? p.PlannedEnd })
                .ToListAsync();

            foreach (var p in projMeta.Take(Math.Min(300, projMeta.Count)))
            {
                if (fbRand.NextDouble() < 0.65) // 65% have feedback
                {
                    var score = fbRand.NextDouble() < 0.15 ? fbRand.Next(0, 6) : fbRand.Next(7, 10);
                    db.CustomerFeedbacks.Add(new CustomerFeedback
                    {
                        CustomerID = p.CustomerID,
                        ProjectID  = p.ProjectID,
                        Score      = score,
                        Comments   = score >= 9 ? "Excellent delivery and communication."
                                   : score >= 7 ? "Good overall; a few delays."
                                                : "Unclear updates; delays impacted schedule.",
                        SubmittedOn = p.End.AddDays(fbRand.Next(0, 30))
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        // --- Helpers ---

        private static string RandomType(Random r)
        {
            var v = r.NextDouble();
            if (v < 0.40) return "Meeting";
            if (v < 0.70) return "Email";
            if (v < 0.90) return "Call";
            return "Site Visit";
        }

        private static string ComputeStatus(DateTime start, int plannedDays, int delayDays)
        {
            var today = DateTime.UtcNow.Date;
            var actualEnd  = start.AddDays(plannedDays + delayDays).Date;

            if (today < start.Date) return "Planning";
            if (today >= actualEnd) return "Completed";
            return "InProgress";
        }

        private static double Normal(Random r, double mean = 0.0, double stdDev = 1.0)
        {
            // Box-Muller
            var u1 = 1.0 - r.NextDouble();
            var u2 = 1.0 - r.NextDouble();
            var randStd = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStd;
        }

        private static double SampleFromName(string name, params (string key, double val)[] prefs)
        {
            foreach (var p in prefs)
            {
                if (name.Contains(p.key, StringComparison.OrdinalIgnoreCase))
                    return p.val;
            }
            return 0.05; // default small positive
        }

        private class YardMod
        {
            public double Quality { get; set; } // higher => slightly fewer delays
            public double Comms   { get; set; } // higher => more interactions (mitigation)
        }
    }
}





