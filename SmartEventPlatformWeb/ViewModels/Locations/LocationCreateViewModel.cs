namespace SmartEventPlatformWeb.ViewModels.Locations
{
    public class LocationCreateViewModel
    {
        public string LocationName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }
}
