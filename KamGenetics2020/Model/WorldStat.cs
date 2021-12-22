using System;
using System.ComponentModel.DataAnnotations;

namespace KamGenetics2020.Model
{
    [Serializable]
    public class WorldStat
    {
        [Key]
        public int Id { get; set; }
        public int TimeIdx { get; set; }
        public int Population { get; set; }
        public int Born { get; set; }
        public int Terminated { get; set; }
        public double PeriodStartResourceLevel { get; set; }
        public double PeriodEndResourceLevel { get; set; }
        public double PeriodConsumption { get; set; }
        public double PeriodCultivation { get; set; }
        public double CalculatedReplenishmentAmount { get; set; }
        public double ActualReplenishmentAmount { get; set; }
        public DateTime Modified { get; set; }
        public double MeanLibido{ get; set; }

    }
}
