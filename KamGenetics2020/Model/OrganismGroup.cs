using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using KamGeneticsLib.Model;

namespace KamGenetics2020.Model
{
    [Serializable]
    [DebuggerDisplay("Id:{Id} Pop:{Population}")]
    public class OrganismGroup
    {
       public OrganismGroup()
       {
       }

       public OrganismGroup(World world, Organism organism1, Organism organism2): this()
       {
          World = world;
          //world.AssignGroupId(this);
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
        public double UnfilledStorageCapacity => StorageCapacity - StorageLevel;

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
            Organisms.Remove(organism);
            StorageCapacity -= organism.StorageCapacity;
            // A member is gone. Capacity is diminished. Ensure actual level does not exceed capacity.
            StorageLevel = Math.Min(StorageLevel, StorageCapacity);
            return this;
        }

        /// <summary>
        /// Called when an organism consumes from Group storage
        /// </summary>
        public double DecreaseResources(double consumption)
        {
            StorageLevel -= consumption;
            // ensure not negative - cannot be
            StorageLevel = Math.Max(0, StorageLevel);
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

        public EconomyGene GroupEconomy => (EconomyGene)Organisms.FirstOrDefault().GetGeneValueByType(GeneEnum.Economy);

    }
}