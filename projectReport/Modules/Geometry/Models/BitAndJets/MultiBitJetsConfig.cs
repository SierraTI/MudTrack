using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectReport.Models.Geometry.BitAndJets
{
    public class MultiBitJetsConfig
    {
        public List<JetSet> JetSets { get; set; } = new List<JetSet>();

        public double TfaTotal => Math.Round(JetSets.Where(s => s.TFACalculated.HasValue).Sum(s => s.TFACalculated.Value), 3);

        public MultiBitJetsConfig()
        {
        }

        public void AddJetSet(JetSet set)
        {
            if (set == null) return;
            set.Id = NextId();
            set.Recalculate();
            JetSets.Add(set);
        }

        public void RemoveJetSet(int id)
        {
            JetSets.RemoveAll(s => s.Id == id);
            Reindex();
        }

        public void UpdateJetSet(int id, int? numberOfJets, int? diameter32)
        {
            var s = JetSets.FirstOrDefault(x => x.Id == id);
            if (s == null) return;
            s.NumberOfJets = numberOfJets;
            s.JetDiameter32nds = diameter32;
            s.Recalculate();
        }

        public void Reindex()
        {
            for (int i = 0; i < JetSets.Count; i++)
                JetSets[i].Id = i + 1;
        }

        private int NextId()
        {
            return JetSets.Count == 0 ? 1 : (JetSets.Max(s => s.Id) + 1);
        }
    }
}
