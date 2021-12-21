using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace KamGenetics2020.Model
{
//   [Serializable]
 public  class OrganismStat
   {
      [Key]
      public int Id { get; set; }
      public int TimeIdx { get; set; }
      public double PeriodStartResourceLevel { get; set; }
      public double PeriodEndResourceLevel { get; set; }
      public double PeriodConsumption { get; set; }
      public double PeriodCultivation { get; set; }
      public double FoodFound { get; set; }
      public double FoodStolen { get; set; }
      public double FoodFromGroup { get; set; }
      public double FoodToGroup { get; set; }
   }
}
