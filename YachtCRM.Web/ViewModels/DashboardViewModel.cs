namespace YachtCRM.Web.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalProjects { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalChangeRequests { get; set; }
        public int TotalInteractions { get; set; }
        public bool NeedsSeed { get; set; }

        // Status chart
        public List<StatusCount> StatusCounts { get; set; } = new();
        public List<OffenderRow> TopPredictedDelays { get; set; } = new();

        // Pre-serialized JSON for charts (so Razor stays simple)
        public string StatusLabelsJson { get; set; } = "[]";
        public string StatusDataJson { get; set; } = "[]";
        public string TasksVsLengthPointsJson { get; set; } = "[]"; // [{x,y,label,id}]
        public string CrVsDelayPointsJson { get; set; } = "[]";     // [{x,y,label,id}]

        public string TasksVsLengthLabelsJson { get; set; } = "[]";
        public string TasksVsLengthXJson { get; set; } = "[]";
        public string TasksVsLengthYJson { get; set; } = "[]";

        public string CrVsDelayLabelsJson { get; set; } = "[]";
        public string CrVsDelayXJson { get; set; } = "[]"; // ChangeRequests
        public string CrVsDelayYJson { get; set; } = "[]"; // PredictedDelay
    public double? AvgFeedbackScore { get; set; }
        public int OpenServiceRequests { get; set; }
    }

    public class OffenderRow
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public float PredictedDelayDays { get; set; }
        public int ChangeRequests { get; set; }
        public int Tasks { get; set; }
        public float Length { get; set; }
    }

    public class StatusCount
    {
        public string Status { get; set; } = "Unknown";
        public int Count { get; set; }
    }
}
