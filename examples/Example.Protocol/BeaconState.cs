using Microsoft.Extensions.Options;

namespace Example.Protocol
{
    public class BeaconState
    {
        private readonly OptionsMonitor<BeaconSettings> _beaconSettingsMonitor;

        public BeaconState(OptionsMonitor<BeaconSettings> beaconSettingsMonitor)
        {
            _beaconSettingsMonitor = beaconSettingsMonitor;
        }

        public int HeadSlot => _beaconSettingsMonitor.CurrentValue.HeadSlot;
    }
}
