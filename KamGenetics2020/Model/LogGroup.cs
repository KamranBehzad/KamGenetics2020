using System.ComponentModel.DataAnnotations;

namespace KamGenetics2020.Model
{
   public class LogGroup
   {
      public LogGroup()
      { }

      public LogGroup(string priority, string description, double storage, double? quantity, int timeIdx)
      {
         PriorityCode = priority;
         Description = description;
         Quantity = quantity;
         TimeIdx = timeIdx;
         Storage = storage;
      }

      [Key] public int Id { get; set; }


      public Group Group { get; set; }

      public int TimeIdx { get; set; }
      public string PriorityCode { get; set; }
      public string Description { get; set; }
      public double? Quantity { get; set; }
      public double Storage { get; set; }
      //public double EWorker { get; set; }
      //public double ESurvivor { get; set; }
      //public double EThief { get; set; }
      //public double EFungus { get; set; }
      //public double MNon { get; set; }
      //public double MPassive { get; set; }
      //public double MActive { get; set; }
      //public double MOffensive { get; set; }

   }
}
