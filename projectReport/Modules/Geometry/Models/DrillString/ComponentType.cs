namespace ProjectReport.Models.Geometry.DrillString
{
    public enum ComponentType
    {
        DrillPipe,
        HWDP,
        Casing,
        Liner,
        SettingTool,
        DC,
        LWD,
        MWD,
        PWO,       // Added as per specification
        PWD,       // Kept for backward compatibility
        Motor,
        XO,
        Jar,
        Accelerator,
        NearBit,
        Stabilizer, // Added for stabilizer components
        Bit,        // Kept for backward compatibility
        BitSub      // Kept for backward compatibility
    }
}