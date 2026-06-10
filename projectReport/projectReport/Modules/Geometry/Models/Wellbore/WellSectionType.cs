using System;

namespace ProjectReport.Models.Geometry.Wellbore
{
    /// <summary>
    /// Classification of the wellbore section used for UI dropdowns (Well Section column).
    /// </summary>
    public enum WellSectionType
    {
        Riser,
        ConductorCasing,
        SurfaceCasing,
        IntermediateCasing,
        ProductionCasing,
        Liner,
        CasedHole,
        OpenHole
    }
}
