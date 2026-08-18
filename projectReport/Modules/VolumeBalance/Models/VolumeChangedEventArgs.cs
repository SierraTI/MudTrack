using System;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class VolumeChangedEventArgs : EventArgs
    {
        public int VolumeBalanceEventId { get; }

        public int EventFluidSystemId { get; }

        public int PitId { get; }

        public double? PreviousVolume { get; }

        public double? CurrentVolume { get; }

        public double? Density { get; }

        public VolumeChangedEventArgs(
            int volumeBalanceEventId,
            int eventFluidSystemId,
            int pitId,
            double? previousVolume,
            double? currentVolume,
            double? density)
        {
            VolumeBalanceEventId =
                volumeBalanceEventId;

            EventFluidSystemId =
                eventFluidSystemId;

            PitId =
                pitId;

            PreviousVolume =
                previousVolume;

            CurrentVolume =
                currentVolume;

            Density =
                density;
        }
    }
}