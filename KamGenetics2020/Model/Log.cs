using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Xml;

namespace KamGenetics2020.Model
{
   public class Log
   {
      public Log()
      { }

      public Log(string description, double quantity, int timeIdx)
      {
         Description = description;
         Quantity = quantity;
         TimeIdx = timeIdx;
      }

      [Key] public int Id { get; set; }
      
      public string Description { get; set; }
      public double Quantity { get; set; }

      public Organism Organism { get; set; }
      
      public int TimeIdx { get; set; }
   

   }
}
