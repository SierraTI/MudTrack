using System;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class VolumeBalanceEvent
    {
        public int Id { get; set; }

        public string Hora { get; set; }

        public string Description { get; set; }

        public string CurrentDepth { get; set; }

        public string Activity { get; set; }

        public DateTime CreatedAt { get; set; }
    }
    public enum ActivityType
    {
        Drilling,
        Tripping,
        Mixing,
        Cementing,
        Displacement,
        Circulating,
        Other
    }

    public enum LossCategory
    {
        SCE,        // Surface Control Equipment: Shakers, Centrifuges, Mud Cleaners
        Downhole,   // Filtration, Lost in Hole, Left Behind Casing
        Misc        // Evaporation, Trips, Displacement
    }

    public enum SystemType
    {
        Active,
        Reserve,
        Other
    }

    public enum BaseFluidType
    {
        Water,
        DewateringWater,
        OsmosisWater,
        Oil,
        OilBased,
        Influx,
        Other
    }
}