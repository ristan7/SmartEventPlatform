namespace SmartEventPlatformWeb.ViewModels.Locations
{
    public class LocationListViewModel
    {
        public long LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }
}
