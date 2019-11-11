﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KamGenetics2020.Model
{
    [Serializable]
    public class OrganismGroup
    {
        [Key]
        public int Id { get; set; }

        public double Count => Organisms.Count;

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

        public double StorageLevel { get; set; }


        public double StorageCapacity { get; set; }

        public double OrganismResourceShare => StorageLevel / Count;
        public double RemainingStorageCapacity => StorageCapacity - StorageLevel;

        public OrganismGroup Add(ref Organism organism)
        {
            Organisms.Add(organism);
            organism.GroupId = Id;
            StorageCapacity += organism.StorageCapacity;
            StorageLevel += organism.StorageLevel;
            return this;
        }

        public OrganismGroup Remove(ref Organism organism)
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

    }
}