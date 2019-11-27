using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using KamGenetics2020.Helpers;
using KamGeneticsLib.Model;
using KBLib.Helpers;

namespace KamGenetics2020.Model
{
   [Serializable]
   [DebuggerDisplay("Id:{Id} Group:{GroupId}")]
   public class Organism
   {
      // Age constants
      private const int MaxAge = 82;
      private const int MaturityStart = 15;
      private const int MaturityFinish = 70;
      private const double DeathByAccidentPercentage = 0.01;

      // Breeding constants

      // Resource Manipulation constants
      private const double DefaultConsumptionRatePerPeriod = 1;
      private const double DefaultStorageCapacity = 10;
      private const double InitialStorageLevel = 1;

      // Log constants
      private string _logPriority1 = "0001";
      private string _logPriorityIsBorn = "0010";
      private string _logPriorityFormJoinGroup = "0100";
      private string _logPriorityFoundResource = "0200";
      private string _logPriorityConsumeResource = "0300";
      private string _logPriorityGaveBirth = "0400";
      private string _logPriorityResourceExchange = "0500";
      private string _logPriorityDeath = "9999";

      protected LogLevel OrgLogLevel = LogLevel.All;

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
            Dob = TimeIdx,
            ConsumptionRatePerPeriod = DefaultConsumptionRatePerPeriod,
            StorageLevel = InitialStorageLevel,
            Genes = genes
         };
         // Give half resources to baby
         ShareResourcesWith(baby, StorageLevel / 2);
         // Baby automatically becomes part of parent's group
         parent?.Group?.Join(baby);
         return baby;
      }

      private void ShareResourcesWith(Organism organismReceiver, double exchangeQty)
      {
         organismReceiver.StorageLevel += exchangeQty;
         StorageLevel -= exchangeQty;
         AddLogEntry(_logPriorityResourceExchange, "Gave resources", StorageLevel, Starvation, exchangeQty, LogLevel.Important);
         organismReceiver.AddLogEntry(_logPriorityResourceExchange, "Received resources", organismReceiver.StorageLevel, organismReceiver.Starvation, exchangeQty, LogLevel.Important);
      }

      private void InitGenes()
      {
         Genes.Add(GeneHelper.CreateCooperationGene(RandomHelper.StandardGeneratorInstance.Next(1, 2)));
         Genes.Add(GeneHelper.CreateEconomyGene());
         Genes.Add(GeneHelper.CreateLibidoGene());
      }

      [Key]
      public int Id { get; set; }

      // ReSharper disable once MemberCanBePrivate.Global
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
         var resourceFound = World.OrganismSeeksFood(ConsumptionRatePerPeriod, AvailableStorageCapacity + ConsumptionRatePerPeriod);
         IncreaseResources(resourceFound);
         AddLogEntry(_logPriorityFoundResource,"Found resources", StorageLevel, Starvation, resourceFound, LogLevel.EveryInterval);
      }

      private void IncreaseResources(double resourceFound)
      {
         StorageLevel += resourceFound;
         StorageLevel = Math.Min(StorageLevel, StorageCapacity + ConsumptionRatePerPeriod);
         if (IsInGroup)
         {
            Group.IncreaseResources(resourceFound);
         }
      }

      public double AvailableStorageCapacity =>
        PersonalAvailableStorageCapacity + (Group?.AvailableStorageCapacity ?? 0);

      private double PersonalAvailableStorageCapacity => StorageCapacity - StorageLevel;

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
         else
         {
            Starvation = 0;
         }
         AddLogEntry(_logPriorityConsumeResource,"Consumed resources", StorageLevel, Starvation, consumption, LogLevel.EveryInterval);
      }

      /// <summary>
      /// Organism first consumes from personal storage. Once that's depleted it can consume from group resources.
      /// </summary>
      private void DecreaseResources(double consumption)
      {
         if (StorageLevel >= consumption)
         {
            StorageLevel -= consumption;
         }
         else if (IsInGroup)
         {
            Group.DecreaseResources(consumption);
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
            AddLogEntry(_logPriorityFormJoinGroup,"Joined Group", StorageLevel, Starvation, Group.Id, LogLevel.MostImportant);
         }
         else
         {
            // Need to form a new group comprising of the two organisms
            OrganismGroup newGroup = new OrganismGroup(World, this, similarOrganism);
            World.AddGroup(newGroup);
            AddLogEntry(_logPriorityFormJoinGroup,"Formed new Group", StorageLevel, Starvation, null, LogLevel.MostImportant);
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
         AddLogEntry(_logPriorityGaveBirth, "Gave birth", StorageLevel, Starvation, null, LogLevel.Important);
         var baby = NewBaby(World, this, babyGenes);
         baby.AddLogEntry(_logPriorityIsBorn, "Is born", StorageLevel, Starvation, null, LogLevel.EveryInterval);
         return baby;
      }

      private bool CanProcreate()
      {
         return Age >= MaturityStart
                && Age <= MaturityFinish
                && RandomHelper.StandardGeneratorInstance.Next(100) < GetGeneValueByType(GeneEnum.Libido);
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
         AddLogEntry(_logPriorityDeath,$"Dying: {reason}, Age:", StorageLevel, Starvation, Age, LogLevel.Important);
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

      private void AddLogEntry(string priority, string text, double storage, double starvation, double? qty = null, LogLevel level = LogLevel.All)
      {
         var logApproval = (int)level & (int)OrgLogLevel;
         if (logApproval > 0)
         {
            LogBook.Add(new Log(priority, text, storage, starvation, qty, TimeIdx));
         }
      }

   }
}
