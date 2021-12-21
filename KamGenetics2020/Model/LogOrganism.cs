using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KamGenetics2020.Model
{
    //   [Serializable]
    public class LogOrganism
   {
      public LogOrganism()
      { }

      public LogOrganism(string priority, string description, double storage, double starvation, double? quantity, int timeIdx)
      {
         PriorityCode = priority;
         Description = description;
         Quantity = quantity;
         TimeIdx = timeIdx;
         Storage = storage;
         Starvation = starvation;
      }

      [Key] public int Id { get; set; }
      

      public Organism Organism { get; set; }
      
      public int TimeIdx { get; set; }
      public string PriorityCode { get; set; }
      public string Description { get; set; }
      public double? Quantity { get; set; }
      public double Storage { get; set; }
      public double Starvation { get; set; }


   }
}
