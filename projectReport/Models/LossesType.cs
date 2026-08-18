using System.Collections.Generic;

namespace ProjectReport.Models
{
    public class LossesType
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;


        public List<LossesSubType> SubTypes { get; set; }
            = new();


        public override string ToString()
        {
            return Name;
        }
    }
}