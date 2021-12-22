using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KamGenetics2020.Model
{
    public class OrganismLog
    {
        private static long NextId = 1;
        public OrganismLog()
        { }


        public OrganismLog(Organism organism, string priority, string description, double? quantity, int timeIdx)
        {
            PriorityCode = priority;
            Description = description;
            Quantity = quantity;
            TimeIdx = timeIdx;
            Storage = organism.StorageLevel;
            Starvation = organism.Starvation;
            ShortagesExperienced = organism.ShortagesExperienced;
        }

        [Key] public int Id { get; set; }

        public Organism Organism { get; set; }

        public int TimeIdx { get; set; }
        public string PriorityCode { get; set; }
        public string Description { get; set; }
        public double? Quantity { get; set; }
        public double Storage { get; set; }
        public double Starvation { get; set; }
        public int ShortagesExperienced { get; private set; }
    }
}
