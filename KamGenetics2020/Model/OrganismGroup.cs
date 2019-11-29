using KamGeneticsLib.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;

namespace KamGenetics2020.Model
{
   [Serializable]
   [DebuggerDisplay("Id:{Id} Pop:{Population}")]
   public class OrganismGroup
   {
      public OrganismGroup()
      {
      }

      public OrganismGroup(World world, Organism organism1, Organism organism2) : this()
      {
         World = world;
         Join(organism1);
         Join(organism2);
      }
      public World World { get; set; }


      [Key]
      public int Id { get; set; }

      public double Population => Organisms.Count;

      private List<Organism> _organisms;

      public List<Organism> Organisms
      {
         get
         {
            if (_organisms == null)
            {
               _organisms = new List<Organism>();
            }

            return _organisms;
         }
      }

      [NotMapped]
      public double StorageLevel { get; set; }

      [NotMapped]
      public double StorageCapacity { get; set; }

      public double OrganismResourceShare => Population > 0 ? StorageLevel / Population : 0;
      public double AvailableStorageCapacity => StorageCapacity - StorageLevel;

      public OrganismGroup Join(Organism organism)
      {
         Organisms.Add(organism);
         organism.Group = this;
         organism.GroupId = Id;
         StorageCapacity += organism.StorageCapacity;
         StorageLevel += organism.StorageLevel;
         return this;
      }

      public OrganismGroup Remove(Organism organism)
      {
         // we do not physically remove the organism from the group for record keeping purposes
         DepartedOrganisms.Add(organism);
         Organisms.Remove(organism);
         // A member is gone. Capacity is diminished. Ensure actual level does not exceed capacity.
         //StorageCapacity -= organism.StorageCapacity;
         //StorageLevel = Math.Min(StorageLevel, StorageCapacity);
         return this;
      }

      /// <summary>
      /// Called when an organism consumes from Group storage
      /// </summary>
      public double DecreaseResources(double consumption)
      {
         // Ensure consume only what's available
         consumption = Math.Min(consumption, StorageLevel);
         StorageLevel -= consumption;
         return StorageLevel;
      }

      /// <summary>
      /// Called when an organism adds to Group storage
      /// </summary>
      public double IncreaseResources(double cultivation)
      {
         StorageLevel += cultivation;
         // ensure not above max
         StorageLevel = Math.Min(StorageLevel, StorageCapacity);
         return StorageLevel;
      }

      public double EconomyScore => Organisms.Average(org => (double)org.GetGeneValueByType(GeneEnum.Economy));
      public double MilitaryScore => Organisms.Average(org => (double)org.GetGeneValueByType(GeneEnum.Military));

      public EconomyGene GroupEconomyGene => GetGroupEconomyGene();

      /// <summary>
      /// The group gene is dictated by the genes of most of the individuals in the group
      /// </summary>
      private EconomyGene GetGroupEconomyGene()
      {
         int roundedValue = (int)Math.Round(EconomyScore, 0);
         return (EconomyGene)roundedValue;
      }

      public MilitaryGene GroupMilitaryGene => GetGroupMilitaryGene();

      private MilitaryGene GetGroupMilitaryGene()
      {
         int roundedValue = (int)Math.Round(MilitaryScore, 0);
         return (MilitaryGene)roundedValue;
      }

      private List<Organism> _departedOrganisms;

      public List<Organism> DepartedOrganisms
      {
         get
         {
            if (_departedOrganisms == null)
            {
               _departedOrganisms = new List<Organism>();
            }

            return _departedOrganisms;
         }
      }

   }
}