namespace hum.Models
{
    public class RepositoryInfo
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Url { get; set; }
        public required string CloneUrl { get; set; }
        public required string DefaultBranch { get; set; }
        public required string Owner { get; set; }
        public required string ProviderSpecificId { get; set; }
    }
}
