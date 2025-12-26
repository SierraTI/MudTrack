using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Services
{
    public class GeometryCalculationService
    {
        private const double BblVolumeConstant = 1029.4; // bbl = (dia_in)^2 * length_ft / 1029.4

        /// <summary>
        /// Calculates the volume of a cylinder given diameter and length
        /// </summary>
        /// <param name="diameter">Diameter in current unit system</param>
        /// <param name="length">Length in current unit system</param>
        /// <param name="unitSystem">"Imperial" or "Metric"</param>
        /// <returns>Volume in base units (bbl for Imperial, m³ for Metric)</returns>
        public double CalculateCylindricalVolume(double diameter, double length, string unitSystem)
        {
            if (diameter <= 0 || length <= 0)
                return 0;

            if (unitSystem == "Imperial")
            {
                // Volume (bbl) = (diameter_in)^2 * length_ft / 1029.4
                return (diameter * diameter * length) / BblVolumeConstant;
            }
            else // Metric
            {
                // Volume (m³) = π * (diameter_mm / 1000 / 2)^2 * length_m
                // Converting mm to m: diameter_mm / 1000
                double diameterM = diameter / 1000;
                double radiusM = diameterM / 2;
                return Math.PI * radiusM * radiusM * length;
            }
        }

        /// <summary>
        /// Calculates annular volume for a specific segment
        /// </summary>
        /// <param name="outerDiameter">Outer diameter (e.g., wellbore ID)</param>
        /// <param name="innerDiameter">Inner diameter (e.g., drill string OD)</param>
        /// <param name="length">Length of segment</param>
        /// <param name="unitSystem">"Imperial" or "Metric"</param>
        /// <returns>Annular volume in base units</returns>
        public double CalculateAnnularVolume(double outerDiameter, double innerDiameter, double length, string unitSystem)
        {
            double outerVolume = CalculateCylindricalVolume(outerDiameter, length, unitSystem);
            double innerVolume = CalculateCylindricalVolume(innerDiameter, length, unitSystem);
            return Math.Max(0, outerVolume - innerVolume);
        }

        // NOTE: CalculateDrillStringComponentVolumes method removed
        // InternalVolume and DisplacementVolume are now calculated properties
        // in the DrillStringComponent model and update automatically based on OD, ID, and Length


        /// <summary>
        /// Calculates volume for a wellbore component.
        /// For Casing/Liner: Annular Volume = (ID_prev^2 - OD_curr^2) * Length / 1029.4
        /// For first casing without previous section: Internal Capacity = ID^2 * Length / 1029.4
        /// For OpenHole: Uses internal calculation with washout adjustment
        /// </summary>
        public double CalculateWellboreComponentVolume(WellboreComponent component, string unitSystem, WellboreComponent? previousComponent = null)
        {
            if (component == null) return 0;

            // Verify we have minimum required data
            if (component.Length <= 0 || (!component.OD.HasValue && !component.ID.HasValue))
            {
                component.Volume = 0;
                return 0;
            }

            // Case 1: OpenHole
            if (component.SectionType == WellboreSectionType.OpenHole)
            {
                // For OpenHole: OD = Hole Diameter, ID = 0
                if (component.OD.HasValue && component.OD.Value > 0)
                {
                    double holeDiameter = component.OD.Value;
                    double washoutFactor = 1.0 + (component.Washout.GetValueOrDefault(0) / 100.0);
                    double vol = (Math.PI / 4.0) * Math.Pow(holeDiameter, 2) * component.Length * washoutFactor / BblVolumeConstant;
                    component.Volume = vol;
                    return vol;
                }
                component.Volume = 0;
                return 0;
            }
            
            // Case 2: Casing/Liner with previous component
            if (previousComponent != null && previousComponent.ID.HasValue && component.OD.HasValue && previousComponent.ID.Value > component.OD.Value)
            {
                // Annular Volume = (ID_prev^2 - OD_curr^2) * Length / 1029.4
                double annularVol = Math.Max(0, (Math.Pow(previousComponent.ID.Value, 2) - Math.Pow(component.OD.Value, 2)) / BblVolumeConstant * component.Length);
                component.Volume = annularVol;
                return annularVol;
            }
            
            // Case 3: First Casing/Liner (no previous component) - Use Internal Capacity
            if (previousComponent == null && component.ID.HasValue && component.ID.Value > 0)
            {
                // Internal Capacity = ID^2 * Length / 1029.4
                double capacity = (Math.Pow(component.ID.Value, 2) / BblVolumeConstant) * component.Length;
                component.Volume = capacity;
                return capacity;
            }
            
            // Default: insufficient data
            component.Volume = 0;
            return 0;
        }

        /// <summary>
        /// Calculates total volume for drill string components
        /// </summary>
        public double CalculateTotalDrillStringVolume(IEnumerable<DrillStringComponent> components, bool useDisplacement, string unitSystem)
        {
            double total = 0;
            foreach (var component in components)
            {
                // Volumes are now calculated automatically in the DrillStringComponent model
                total += useDisplacement ? component.DisplacementVolume : component.InternalVolume;
            }
            return total;
        }

        /// <summary>
        /// Calculates total volume for wellbore components
        /// </summary>
        public double CalculateTotalWellboreVolume(IEnumerable<WellboreComponent> components, string unitSystem)
        {
            double total = 0;
            foreach (var component in components)
            {
                CalculateWellboreComponentVolume(component, unitSystem);
                total += component.Volume;
            }
            return total;
        }

        /// <summary>
        /// Calculates total annular volume using simple method
        /// </summary>
        public double CalculateTotalAnnularVolume(double totalWellboreVolume, double totalDrillStringVolume)
        {
            return Math.Max(0, totalWellboreVolume - totalDrillStringVolume);
        }

        /// <summary>
        /// Calculates detailed annular volume breakdown for the UI table
        /// </summary>
        public List<AnnularVolumeDetail> CalculateAnnularVolumeDetails(
            IEnumerable<WellboreComponent> wellboreComponents,
            IEnumerable<DrillStringComponent> drillStringComponents,
            string unitSystem)
        {
            var details = new List<AnnularVolumeDetail>();
            
            if (wellboreComponents == null || !wellboreComponents.Any())
                return details;

            // Sort components by depth
            var sortedWellbore = wellboreComponents.OrderBy(w => w.TopMD).ToList();
            
            // If no drill string, return wellbore volumes (open hole capacity)
            if (drillStringComponents == null || !drillStringComponents.Any())
            {
                int id = 1;
                foreach (var section in sortedWellbore)
                {
                    if ((section.ID ?? 0) <= 0 || (section.TopMD ?? 0) >= (section.BottomMD ?? 0)) continue;
                    
                    double length = (section.BottomMD ?? 0) - (section.TopMD ?? 0);
                    double volume = CalculateCylindricalVolume(section.ID ?? 0, length, unitSystem);
                    
                    details.Add(new AnnularVolumeDetail
                    {
                        Id = id++,
                        Name = $"{section.Name} (No String)",
                        WellboreID = section.ID ?? 0,
                        DrillStringOD = 0,
                        TopMD = section.TopMD ?? 0,
                        BottomMD = section.BottomMD ?? 0
                    });
                }
                return details;
            }

            var sortedDrillString = drillStringComponents.ToList(); // Order preserved from UI (bottom to top usually, but we need top to bottom for calculation)
            // Drill string is usually entered bottom-to-top (Bit first). Let's reverse if needed or handle logic carefully.
            // Actually, the SRS says "Configure from bottom to top", so index 0 is bottom? 
            // Let's assume the list passed here is in the order they appear in the grid.
            // We need to calculate absolute depths for drill string components.
            
            // Calculate absolute depths for drill string components (assuming they are connected)
            // The SRS implies "Force to Bottom" logic, but for calculation we just stack them.
            // If "Force to Bottom" is active, the ViewModel handles the length adjustment.
            // Here we just stack them from Top (0) or Bottom? 
            // Standard practice: Drill string hangs from surface (0). 
            // BUT SRS says "Configure from bottom to top". 
            // Let's assume the ViewModel provides them in a logical order or we need to infer.
            // Usually, Bit is at the bottom. 
            // Let's assume the list is ordered Top-to-Bottom for calculation simplicity, OR we reverse it if it's Bottom-to-Top.
            // Checking DrillStringGeometryView... usually users add Bit, then DC, then Pipe.
            // So the list is likely Bottom-to-Top.
            // Let's reverse it to stack from Surface.
            
            // WAIT: If the user enters Bit first, then DC, then Pipe...
            // The Pipe is at the top (0 ft). The Bit is at the bottom.
            // So we should iterate backwards to stack from 0?
            // Let's assume the ViewModel passes them in the order they are in the collection.
            // If the collection is Bottom-to-Top (Bit at index 0), then the last item is at Surface.
            
            // Let's try to determine order. 
            // If we assume the last component in the list starts at 0.
            
            var dsComponents = sortedDrillString.ToList();
            // We need to know the order. Let's assume the ViewModel handles the order or we check ComponentType.
            // For now, let's assume the list is ordered such that we can stack them.
            // Actually, standard UI for drill string is often Top-to-Bottom in grids, or Bottom-to-Top.
            // SRS 5.2 says "Configure from bottom to top".
            // So index 0 = Bottom component (Bit). Last index = Top component (Drill Pipe).
            
            // So we stack from the END of the list (Top) downwards.
            
            var dsWithDepths = new List<(DrillStringComponent Component, double Top, double Bottom)>();
            double currentDepth = 0;
            
            // Stack from Top (Last item) to Bottom (First item)
            for (int i = dsComponents.Count - 1; i >= 0; i--)
            {
                var comp = dsComponents[i];
                double top = currentDepth;
                double bottom = currentDepth + comp.Length.GetValueOrDefault();
                dsWithDepths.Add((comp, top, bottom));
                currentDepth = bottom;
            }
            
            // Now we have drill string components with absolute depths.
            // Let's intersect with wellbore sections.
            
            int detailIdCounter = 1;
            
            foreach (var section in sortedWellbore)
            {
                if ((section.ID ?? 0) <= 0 || (section.TopMD ?? 0) >= (section.BottomMD ?? 0)) continue;
                
                double sectionTop = section.TopMD ?? 0;
                double sectionBottom = section.BottomMD ?? 0;
                
                // Find all DS components that overlap this section
                var overlaps = dsWithDepths
                    .Where(d => d.Bottom > sectionTop && d.Top < sectionBottom)
                    .OrderBy(d => d.Top)
                    .ToList();
                
                if (!overlaps.Any())
                {
                    // Empty hole (below drill string)
                    double vol = CalculateCylindricalVolume(section.ID ?? 0, sectionBottom - sectionTop, unitSystem);
                    details.Add(new AnnularVolumeDetail
                    {
                        Id = detailIdCounter++,
                        Name = $"{section.Name} (Empty)",
                        WellboreID = section.ID.GetValueOrDefault(),
                        DrillStringOD = 0,
                        TopMD = sectionTop,
                        BottomMD = sectionBottom
                    });
                    continue;
                }
                
                // Process overlaps
                double currentCursor = sectionTop;
                
                foreach (var (comp, dsTop, dsBottom) in overlaps)
                {
                    double start = Math.Max(currentCursor, dsTop);
                    double end = Math.Min(sectionBottom, dsBottom);
                    
                    if (start < dsTop) 
                    {
                        // Gap before this component (shouldn't happen if string is continuous from surface, but possible if string is short)
                        // Actually, if we stack from 0, there are no gaps between components.
                        // But there might be a gap between sectionTop and the first component if the string starts below sectionTop?
                        // No, string starts at 0.
                    }
                    
                    if (start > currentCursor)
                    {
                        // Gap between previous component and this one? (Shouldn't happen with continuous string)
                        // Or gap between section start and string start (if string starts deeper? No, string starts at 0).
                        // Gap between string end and section end? Yes.
                    }
                    
                    if (end > start)
                    {
                        double length = end - start;
                        double vol = CalculateAnnularVolume(section.ID.GetValueOrDefault(), comp.OD.GetValueOrDefault(), length, unitSystem);
                        
                        details.Add(new AnnularVolumeDetail
                        {
                            Id = detailIdCounter++,
                            Name = $"{section.Name} / {comp.Name}",
                            WellboreID = section.ID.GetValueOrDefault(),
                            DrillStringOD = comp.OD.GetValueOrDefault(),
                            TopMD = start,
                            BottomMD = end
                        });
                        
                        currentCursor = end;
                    }
                }
                
                // Check for remaining section below drill string
                if (currentCursor < sectionBottom)
                {
                    double length = sectionBottom - currentCursor;
                    double vol = CalculateCylindricalVolume(section.ID.GetValueOrDefault(), length, unitSystem);
                    
                    details.Add(new AnnularVolumeDetail
                    {
                        Id = detailIdCounter++,
                        Name = $"{section.Name} (Below String)",
                        WellboreID = section.ID.GetValueOrDefault(),
                        DrillStringOD = 0,
                        TopMD = currentCursor,
                        BottomMD = sectionBottom
                    });
                }
            }

            return details;
        }

        /// <summary>
        /// Returns a truncated copy of the drill string components up to bitDepth.
        /// Preserves order and splits the last component if needed.
        /// </summary>
        public List<DrillStringComponent> SliceDrillStringByBitDepth(List<DrillStringComponent> components, double bitDepth)
        {
            var sliced = new List<DrillStringComponent>();
            double remaining = bitDepth;
            foreach (var c in components)
            {
                if (remaining <= 0) break;
                double take = Math.Min(c.Length.GetValueOrDefault(), remaining);
                if (take <= 0) break;

                var copy = new DrillStringComponent
                {
                    Name = c.Name,
                    ComponentType = c.ComponentType,
                    ID = c.ID,
                    OD = c.OD,
                    Length = take,
                    ToolJointOD = c.ToolJointOD,
                    ToolJointId = c.ToolJointId,
                    JointLength = c.JointLength,
                    ToolJointLength = c.ToolJointLength
                };
                sliced.Add(copy);
                remaining -= take;
            }
            return sliced;
        }

        /// <summary>
        /// Builds a simple annular volume breakdown per wellbore section using a constant drill string OD at depth.
        /// Uses wellbore internal ID for outer diameter in annular calc.
        /// </summary>
        public List<(string label, double volume)> BuildAnnularBreakdown(
            List<WellboreComponent> wellboreComponents,
            List<DrillStringComponent> slicedDrillString,
            string unitSystem)
        {
            var breakdown = new List<(string, double)>();
            var sortedWellbore = wellboreComponents.OrderBy(w => w.TopMD).ToList();

            foreach (var w in sortedWellbore)
            {
                double sectionVolume = 0;
                double sectionTop = w.TopMD ?? 0;
                double sectionBottom = w.BottomMD ?? 0;
                double remaining = sectionBottom - sectionTop;
                if (remaining <= 0) continue;

                double cursor = 0;
                foreach (var ds in slicedDrillString)
                {
                    double dsBottom = cursor + (ds.Length ?? 0);
                    double overlapTop = Math.Max(sectionTop, cursor);
                    double overlapBottom = Math.Min(sectionBottom, dsBottom);
                    if (overlapBottom > overlapTop)
                    {
                        double overlapLen = overlapBottom - overlapTop;
                        sectionVolume += CalculateAnnularVolume(w.ID.GetValueOrDefault(), ds.OD.GetValueOrDefault(), overlapLen, unitSystem);
                    }
                    cursor = dsBottom;
                    if (cursor >= sectionBottom) break;
                }

                if (sectionVolume > 0)
                {
                    breakdown.Add(($"{w.Name} ({w.TopMD:F0}-{w.BottomMD:F0} ft)", sectionVolume));
                }
            }

            return breakdown;
        }

        private double GetTotalDrillStringLength(List<DrillStringComponent> components)
        {
            return components.Sum(c => c.Length.GetValueOrDefault());
        }

        private double GetComponentTop(DrillStringComponent component, List<DrillStringComponent> allComponents)
        {
            double top = 0;
            foreach (var c in allComponents)
            {
                if (c == component) break;
                top += c.Length.GetValueOrDefault();
            }
            return top;
        }

        /// <summary>
        /// Validates a drill string component
        /// </summary>
        public string? ValidateDrillStringComponent(DrillStringComponent component)
        {
            if (string.IsNullOrWhiteSpace(component.Name))
                return "Component name is required";
            
            if (component.OD.GetValueOrDefault() <= 0)
                return "OD must be greater than 0";

            if (component.ID.GetValueOrDefault() <= 0)
                return "ID must be greater than 0";

            // Explicit error message for ID >= OD per spec
            if (component.OD.GetValueOrDefault() <= component.ID.GetValueOrDefault())
                return "Internal diameter cannot be greater than or equal to external diameter.";

            if (component.Length.GetValueOrDefault() <= 0)
                return "Length must be greater than 0";

            return null;
        }

        /// <summary>
        /// Validates a wellbore component
        /// </summary>
        public string? ValidateWellboreComponent(WellboreComponent component)
        {
            if (string.IsNullOrWhiteSpace(component.Name))
                return "Component name is required";
            
            if (component.ID.GetValueOrDefault() <= 0)
                return "ID must be greater than 0";

            // If OD is present (casings/liners), validate OD > ID
            if (component.OD.GetValueOrDefault() > 0 && component.OD.GetValueOrDefault() <= component.ID.GetValueOrDefault())
                return "OD must be greater than ID";
            
            if (component.TopMD < 0)
                return "Top MD must be greater than or equal to 0";
            
            if (component.BottomMD <= component.TopMD)
                return "Bottom MD must be greater than Top MD";

            return null;
        }

        /// <summary>
        /// Converts a value from one unit system to another
        /// </summary>
        public double ConvertValue(double value, string fromUnit, string toUnit, string valueType)
        {
            if (fromUnit == toUnit) return value;

            // Conversion factors
            if (valueType == "length")
            {
                if (fromUnit == "Imperial" && toUnit == "Metric")
                    return value * 0.3048; // feet to meters
                if (fromUnit == "Metric" && toUnit == "Imperial")
                    return value * 3.28084; // meters to feet
            }
            else if (valueType == "diameter")
            {
                if (fromUnit == "Imperial" && toUnit == "Metric")
                    return value * 25.4; // inches to mm
                if (fromUnit == "Metric" && toUnit == "Imperial")
                    return value / 25.4; // mm to inches
            }
            else if (valueType == "volume")
            {
                if (fromUnit == "Imperial" && toUnit == "Metric")
                    return value * 0.158987; // bbl to m³
                if (fromUnit == "Metric" && toUnit == "Imperial")
                    return value / 0.158987; // m³ to bbl
            }

            return value;
        }
    }
}

