namespace YachtCRM.Domain
{
    public class Document
    {
        public int DocumentID { get; set; }
        public int? CustomerID { get; set; }
        public int? ProjectID { get; set; }

        public string FileName { get; set; } = default!;
        public string FilePath { get; set; } = default!;
        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;

        public Customer? Customer { get; set; }
        public Project? Project { get; set; }
    }
}
