using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace KamGenetics2020.Model
{
   public class Log
   {
      public Log()
      { }

      public Log(string entry, int timeIdx)
      {
         Entry = entry;
         TimeIdx = timeIdx;
      }

      [Key] public int Id { get; set; }
      public string Entry { get; set; }

      public Organism Organism { get; set; }
      
      public int TimeIdx { get; set; }
   

   }
}
