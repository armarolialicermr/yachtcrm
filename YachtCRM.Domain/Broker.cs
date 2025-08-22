namespace YachtCRM.Domain
{
    public class Broker
    {
        public int BrokerID { get; set; }
        public string Name { get; set; } = default!;
        public string? Company { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        public ICollection<CustomerBroker> CustomerBrokers { get; set; } = new List<CustomerBroker>();
    }

    public class CustomerBroker
    {
        public int CustomerID { get; set; }
        public int BrokerID { get; set; }

        public Customer Customer { get; set; } = default!;
        public Broker Broker { get; set; } = default!;
    }
}
