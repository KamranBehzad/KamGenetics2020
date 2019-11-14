using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using KamGenetics2020.Helpers;
using KamGeneticsLib.Model;
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
            var geneValue = RandomHelper.StandardGeneratorInstance.Next(0, 10);
            Genes.Add(GeneHelper.CreateLibidoGene(geneValue));
        }

        [Key]
        public int Id { get; set; }

        public int TimeIdx => World.TimeIdx;
        
        public int? ParentId { get; private set; }

        public Organism Parent { get; private set; }

        private List<Gene> _genes;

        //[NotMapped]
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
            var resourceFound = World.OrganismSeeksFood(ConsumptionRatePerPeriod, UnfilledStorageCapacity + ConsumptionRatePerPeriod);
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

        public double UnfilledStorageCapacity =>
           IsInGroup
              ? Group.UnfilledStorageCapacity
              : StorageCapacity - StorageLevel;

        private void ConsumeResources()
        {
            double consumption = Math.Min(AvailableResources, ConsumptionRatePerPeriod);
            World.RecordOrganismConsumptionInCurrentPeriod(consumption);
            DecreaseResources(consumption);
            // todo develop consumption & starvation model
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

            JoinGroup();
            ObtainResources();
            ConsumeResources();
            ProcreateAsexual();
        }

        /// <summary>
        /// If individual has tendency to form/join groups and not already part of one, will try here to form or join a group with other similar organisms.
        /// Will search for a like-minded individual in its vicinity.
        /// If other individual is in a group then will join that group.
        /// If other individual is not in a group then both will form a new group.
        /// </summary>
        private void JoinGroup()
        {
           // Ignore if already in group
           if (Group != null)
           {
              return;
           }
           // Ignore if not inclined to join groups
           if (GetGeneValueByType(GeneEnum.Cooperation) == (int)CooperationGene.Solo)
           {
              return;
           }
           // Is cooperative and not already part of a group, so will look to join or form a group
           // Will search for a like-minded individual in its vicinity.
           // If other individual is in a group then will join that group.
           // If other individual is not in a group then both will form a new group.
           Organism similarOrganism = World.SearchVicinityForSimilarCooperativeIndividuals(this);
           if (similarOrganism == null)
           {
              return;
           }

           if (similarOrganism.Group != null)
           {
              similarOrganism.Group.Join(this);
              AddLogEntry($"Joined Group {Group.Id}");
           }
           else
           {
              // Need to form a new group comprising of the two organisms
              OrganismGroup newGroup = new OrganismGroup(this, similarOrganism);
              World.AddGroup(newGroup);
              AddLogEntry($"Formed new Group");
           }
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
            AddLogEntry($"Gave birth");
            return NewBaby(World, this, babyGenes);
        }

        private bool CanProcreate()
        {
           return Age >= MaturityStart
                  && Age <= MaturityFinish
                  && RandomHelper.StandardGeneratorInstance.Next(100) < Genes.First(g => g.GeneType == GeneEnum.Libido).CurrentValue;
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
            // Remove from group if any
            Group?.Remove(this);
        }

        public string DeathReason { get; set; }
        public DateTime Modified { get; set; }
        
        [NotMapped]
        public bool IsDead { get; set; }
        
        [NotMapped]
        public double ConsumptionRatePerPeriod { get; set; }

        public int? GroupId { get; set; }
        
        public OrganismGroup Group { get; set; }
        
        public double StorageCapacity => DefaultStorageCapacity;
        
        [NotMapped]
        public double StorageLevel { get; set; }
        
        public double GroupStorage => Group?.StorageLevel ?? 0;


        public double GroupStorageCapacity => Group?.StorageCapacity ?? 0;

        public Gene GetGeneByType(GeneEnum geneType)
        {
          return Genes.FirstOrDefault(g => g.GeneType == geneType);
        }
        
        public int GetGeneValueByType(GeneEnum geneType)
        {
           return Genes.FirstOrDefault(g => g.GeneType == geneType)?.CurrentValue ?? 0;
        }

        private List<Log> _logBook;

        //[NotMapped]
        public List<Log> LogBook
        {
           get
           {
              if (_logBook == null)
              {
                 _logBook = new List<Log>();
              }

              return _logBook;
           }
        }

        private void AddLogEntry(string text)
        {
           LogBook.Add(new Log(text, TimeIdx));
        }

    }
}
