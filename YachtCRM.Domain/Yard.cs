namespace YachtCRM.Domain
{
    public class Yard
    {
        public int YardID { get; set; }
        public string Name { get; set; } = "";
        public string Country { get; set; } = "";
        public string Brand { get; set; } = "";

        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
