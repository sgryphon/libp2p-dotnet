namespace Example.Client
{
    public class DiscoverySettings
    {
        public int MinimumDesired { get; set; }
        public string[] BootstrapAddresses { get; set; } = new string[0];
    }
}
