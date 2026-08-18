using System.Collections.ObjectModel;
using ProjectReport.Models;

namespace ProjectReport.Modules.VolumeBalance.Models
{
    public class VolumeInfoTable
    {
        public ObservableCollection<VolumeBalanceSummaryRow> VolumeInformation { get; set; }

        public ObservableCollection<VolChemicalAdded> ChemicalAdded { get; set; }

        public VolumeInfoTable()
        {
            VolumeInformation =
                new ObservableCollection<VolumeBalanceSummaryRow>();

            ChemicalAdded =
                new ObservableCollection<VolChemicalAdded>();
        }
    }
}