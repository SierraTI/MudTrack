using System.ComponentModel;

namespace ProjectReport.Models
{
    public class LossesSubType
    {
        public int Id { get; set; }

        public int LossesTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public override string ToString()
        {
            return Name;
        }
    }
}