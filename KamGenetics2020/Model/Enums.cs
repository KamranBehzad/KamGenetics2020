using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KamGeneticsLib.Model
{
   public enum GeneEnum
   {
      UserDefined,
      Cooperation,
      Economy,
      Libido,
      Military
   }

   public enum CooperationGene
   {
      Solo = 1,           // organism prefers to work solo
      Cooperative = 2,    // organism prefers to work in teams
   }

   public enum EconomyGene
   {
      Worker = 1,     // organism cultivates resources, never steals.
      Survivor = 2,   // will cultivate resources but if none available will steal.
      Thief = 3,      // steals first. Will cultivate (at a lower rate) if nothing found to steal.
      Fungal = 4, // will put little to no effort in cultivating resources. Will intensely try to join group and feed off the group. 
   }

   public enum MilitaryGene
   {
      NonMilitant = 1,     // no attempt to form defensive measures
      Passive = 2,   // will adopt defensive measures but is always only responsive to attacks
      Proactive = 3,      // adopts defensive measures plus may attack first if feels threatened by an "Offender" body
      Offender = 4,   // Will attack without provocation.
   }


   public enum LogLevel
   {
      None = 0,
      MostImportant = 1,
      Important = 2,
      EveryInterval = 128,
      All = 255
   }
}
