using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace KamGenetics2020.Model
{
   [Serializable]
public   class GroupStat
   {
      [Key]
      public int Id { get; set; }
      public int TimeIdx { get; set; }
      public int Population { get; set; }
      //public int Born { get; set; }
      //public int Terminated { get; set; }
      public double PeriodStartResourceLevel { get; set; }
      public double EconomyScore { get; set; }
      public double MilitaryScore { get; set; }
   }
}
