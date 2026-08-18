namespace ProjectReport.Models
{
    public class FluidOption
    {
        public int Id { get; set; }

        public string FluidName { get; set; } = string.Empty;

        public string FluidType { get; set; } = string.Empty;

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FluidType))
                    return FluidName;

                return $"{FluidName} ({FluidType})";
            }
        }
    }
}