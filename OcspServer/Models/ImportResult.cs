namespace OcspServer.Models
{
    public class ImportResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Unchanged { get; set; }
        public List<string> Errors { get; } = new();
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;
    }
}
