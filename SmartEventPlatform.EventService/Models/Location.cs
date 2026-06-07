namespace SmartEventPlatformWeb.Domains
{
    public class Location
    {
        public long LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
