using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using KamGenetics2020.Helpers;
using KBLib.Helpers;

namespace KamGenetics2020.Model
{
    [Serializable]
    public class Organism
    {
        // Age constants
        private const int MaxAge = 82;
        private const int MaturityStart = 15;
        private const int MaturityFinish = 70;
        private const double DeathByAccidentPercentage = 0.1;

        // Breeding constants

        // Resource Manipulation constants
        private const double DefaultConsumptionRatePerPeriod = 1;
        private const double DefaultStorageCapacity = 10;
        private const double InitialStorageLevel = 1;



        public Organism()
        {
        }

        public Organism(World world, Organism parent) : this()
        {
            World = world;
            Parent = parent;
            ParentId = Parent?.Id;

            Group = null;
            GroupId = null;

            Dob = TimeIdx;

            ConsumptionRatePerPeriod = DefaultConsumptionRatePerPeriod;
            StorageLevel = InitialStorageLevel;
            InitGenes();
        }

        public Organism NewBaby(World world, Organism parent, List<Gene> genes)
        {
            var baby = new Organism
            {
                World = world,
                Parent = parent,
                ParentId = parent?.Id,
                Group = parent?.Group,
                GroupId = parent?.GroupId,
                Dob = TimeIdx,
                ConsumptionRatePerPeriod = DefaultConsumptionRatePerPeriod,
                StorageLevel = InitialStorageLevel,
                Genes = genes
            };
            return baby;
        }

        private void InitGenes()
        {
            Genes.Add(GeneHelper.CreateCooperationGene(RandomHelper.StandardGeneratorInstance.Next(1,2)));
            Genes.Add(GeneHelper.CreateEconomyGene());
            Genes.Add(GeneHelper.CreateLibidoGene(2));
        }

        [Key]
        public int Id { get; set; }

        public int TimeIdx => World.TimeIdx;
        public int? WorldId { get; set; }
        public int? ParentId { get; set; }

        public Organism Parent { get; set; }

        private List<Gene> _genes;

        public List<Gene> Genes
        {
            get
            {
                if (_genes == null)
                {
                    _genes = new List<Gene>();
                }

                return _genes;
            }
            set => _genes = value;
        }

        public void ObtainResources()
        {
            // +consumptionRatePerPeriod is because when organism is at full storage capacity, it can still find and consume as per its consumption rate
            var resourceFound = World.OrganismSeeksFood(ConsumptionRatePerPeriod, RemainingStorageCapacity + ConsumptionRatePerPeriod);
            if (IsInGroup)
            {
                Group.IncreaseResources(resourceFound);
            }
            else
            {
                IncreaseStorage(resourceFound);
            }
        }

        private void IncreaseStorage(double resourceFound)
        {
            StorageLevel += resourceFound;
            StorageLevel = Math.Min(StorageLevel, StorageCapacity+ConsumptionRatePerPeriod);
        }

        public double RemainingStorageCapacity
        {
            get
            {
                return IsInGroup
                    ? Group.RemainingStorageCapacity
                    : StorageCapacity - StorageLevel;
            }
        }

        private void ConsumeResources()
        {
            double consumption = Math.Min(AvailableResources, ConsumptionRatePerPeriod);
            World.RecordPeriodConsumption(consumption);
            DecreaseResources(consumption);
            // develop consumption & starvation model
            if (consumption < ConsumptionRatePerPeriod)
            {

                Starvation += (ConsumptionRatePerPeriod - consumption);
                if (Starvation > ConsumptionRatePerPeriod)
                {
                    Die("Starvation");
                }
            }
        }

        private void DecreaseResources(double consumption)
        {
            if (IsInGroup)
            {
                Group.DecreaseResources(consumption);
            }
            else
            {
                StorageLevel -= consumption;
            }
        }


        public bool IsInGroup => Group != null;

        private double AvailableResources => IsInGroup ? Group.OrganismResourceShare : StorageLevel;

        [NotMapped]
        public double Starvation { get; set; }

        public void ProcreateSexual(Organism organism)
        {
        }

        public void Live()
        {
            var (result, reason) = WillDie();
            if (result)
            {
                Die(reason);
                return;
            }
            ObtainResources();
            ConsumeResources();
            ProcreateAsexual();
        }


        public void ProcreateAsexual()
        {
            if (CanProcreate())
            {
                var baby = BirthBaby();
                World.AddBaby(baby);
            }
        }

        private Organism BirthBaby()
        {
            var babyGenes = new List<Gene>();
            // baby's genes initiate from parent
            foreach (var parentGene in Genes)
            {
               var babyGene = parentGene.CreateCopy();
                babyGene.Mutate();
                babyGenes.Add(babyGene);
            }
            return NewBaby(World, this, babyGenes);
        }

        private bool CanProcreate()
        {
            return Age >= MaturityStart && Age <= MaturityFinish 
                                        && RandomHelper.StandardGeneratorInstance.Next(100) < Genes.First(g => g.GeneType == GeneHelper.GeneEnum.Libido).CurrentValue;
        }

        public World World { get; set; }

        private (bool result, string reason) WillDie()
        {
            var deathByAge = Age > MaxAge + RandomHelper.StandardGeneratorInstance.Next(20) - 10;
            if (deathByAge)
            {
                return (true, "Natural Causes");
            }

            var deathByAccident = RandomHelper.StandardGeneratorInstance.NextDouble() * 100 < DeathByAccidentPercentage;
            if (deathByAccident)
            {
                return (true, "Accident");
            }
            return (false, string.Empty);
        }

        public int Age => TimeIdx - Dob;
        public int Dob { get; set; }
        public int Dod { get; set; }
        public int FinalAge { get; set; }

        private void Die(string reason)
        {
            IsDead = true;
            DeathReason = reason;
            Dod = TimeIdx;
            FinalAge = Age;
        }

        public string DeathReason { get; set; }
        public DateTime Modified { get; set; }
        [NotMapped]
        public bool IsDead { get; set; }
        public double ConsumptionRatePerPeriod { get; set; }

        public int? GroupId { get; set; }
        public OrganismGroup Group { get; set; }
        public double StorageCapacity => DefaultStorageCapacity;
        [NotMapped]
        public double StorageLevel { get; set; }
        public double GroupStorage
        {
            get { return Group == null ? 0 : Group.StorageLevel; }
        }


        public double GroupStorageCapacity
        {
            get { return Group == null ? 0 : Group.StorageCapacity; }
        }
    }
}
