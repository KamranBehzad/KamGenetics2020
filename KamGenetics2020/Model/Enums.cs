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
      Libido
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
      Thief = 3,      // steals from others. Will not kill to steal. Will cultivate if nothing found to steal.
      Murderer = 4,   // steals from others. Kills them if necessary.
   }

}
