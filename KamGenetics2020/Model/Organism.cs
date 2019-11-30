using KamGenetics2020.Helpers;
using KamGeneticsLib.Model;
using KBLib.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;

namespace KamGenetics2020.Model
{
   [Serializable]
   [DebuggerDisplay("Id:{Id} Group:{GroupId}")]
   public class Organism
   {
      // Age constants
      private const int MaxAge = 80;
      private const int MaturityStart = 15;
      private const int MaturityFinish = 65;
      private const double DeathByAccidentPercentage = 0.02;

      // Resource Manipulation constants
      private const double DefaultConsumptionRatePerPeriod = 1;
      private const double DefaultStorageCapacity = 10;
      private const double InitialStorageLevel = 1;

      // Log constants
      private const string LogPriorityIsBorn = "0010";
      private const string LogPriorityFormJoinGroup = "0100";
      private const string LogPriorityObtainedResource = "0200";
      private const string LogPriorityStoleResource = "0210";
      private const string LogPriorityConsumeResource = "0300";
      private const string LogPriorityGaveBirth = "0400";
      private const string LogPriorityResourceExchange = "0500";
      private const string LogPriorityTraining = "0600";
      private const string LogPriorityLostStorage = "0700";
      private const string LogPriorityDeath = "9999";

      // Stealing constants
      private const int StealLowProbability = 10;
      private const int StealMedProbability = 50;
      private const int StealHiProbability = 90;
      private const int StealFullProbability = 100;

      private const int KillNoProbability = 0;
      private const int KillLowProbability = 10;
      private const int KillMedProbability = 50;
      private const int KillHiProbability = 90;
      
      // Military training constants
      private const double MilitaryTrainingPassive = 1.0;
      private const double MilitaryTrainingActive = 1.2;
      private const double MilitaryTrainingOffensive = 1.4;

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
         AddLogEntry(LogPriorityResourceExchange, "Gave resources", StorageLevel, Starvation, exchangeQty, LogLevel.Important);
         organismReceiver.AddLogEntry(LogPriorityResourceExchange, "Received resources", organismReceiver.StorageLevel, organismReceiver.Starvation, exchangeQty, LogLevel.Important);
      }

      private void InitGenes()
      {
         Genes.Add(GeneHelper.CreateCooperationGene(RandomHelper.StandardGeneratorInstance.Next(1, 2)));
         Genes.Add(GeneHelper.CreateLibidoGene());

         Genes.Add(GeneHelper.CreateEconomyGeneVariations());
         Genes.Add(GeneHelper.CreateMilitaryGeneVariations());

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

      /// <summary>
      /// Resources can be obtained by seeking the world (hunting & gathering and cultivating) or bu stealing.
      /// How it's done and in what order is governed by genes.
      /// </summary>
      private void ObtainResources()
      {
         switch ((EconomyGene)GetGeneValueByType(GeneEnum.Economy))
         {
            case EconomyGene.Worker:
               // Seek only
               SeekResources();
               break;
            case EconomyGene.Survivor:
               // Seek first
               SeekResources();
               // Steal if necessary
               if (StorageLevel >= 1 || Starvation.Equals(0))
               {
                  return;
               }

               switch ((MilitaryGene)GetGeneValueByType(GeneEnum.Military))
               {
                  default:
                     break;
                  case MilitaryGene.Passive:
                     StealResources(StealLowProbability);
                     break;
                  case MilitaryGene.Proactive:
                     StealResources(StealMedProbability);
                     break;
                  case MilitaryGene.Offender:
                     StealResources(StealHiProbability);
                     break;
               }

               break;
            case EconomyGene.Thief:
               // Steal first
               switch ((MilitaryGene)GetGeneValueByType(GeneEnum.Military))
               {
                  default:
                     StealResources(StealFullProbability, KillNoProbability);
                     break;
                  case MilitaryGene.Passive:
                     StealResources(StealFullProbability, KillLowProbability);
                     break;
                  case MilitaryGene.Proactive:
                     StealResources(StealFullProbability, KillMedProbability);
                     break;
                  case MilitaryGene.Offender:
                     StealResources(StealFullProbability, KillHiProbability);
                     break;
               }

               // Seek if steal yield was not enough
               if (StorageLevel >= 1 || Starvation.Equals(0))
               {
                  return;
               }

               SeekResources();

               break;
            case EconomyGene.Fungal:
               // Don't do anything if you don't have to!
               if (IsInGroup || StorageLevel >= 1)
               {
                  return;
               }

               switch ((MilitaryGene)GetGeneValueByType(GeneEnum.Military))
               {
                  default:
                     SeekResources();
                     break;
                  case MilitaryGene.Passive:
                  case MilitaryGene.Proactive:
                  case MilitaryGene.Offender:
                     StealResources(StealFullProbability);
                     // Seek if steal yield was not enough
                     if (StorageLevel >= 1)
                     {
                        return;
                     }

                     SeekResources();
                     break;
               }

               break;
         }
      }

      private void StealResources(int stealProbability, int killProbability = 0)
      {
         var random = RandomHelper.StandardGeneratorInstance.NextDouble();
         if (random > stealProbability)
         {
            return;
         }
         double resourceStolen = World.OrganismStealsFood(this, AvailableStorageCapacity + ConsumptionRatePerPeriod, killProbability);
         IncreaseResources(resourceStolen);
         AddLogEntry(LogPriorityStoleResource, "Stole resources", StorageLevel, Starvation, resourceStolen, LogLevel.Important);
      }

      private void SeekResources()
      {
         // +consumptionRatePerPeriod is because when organism is at full storage capacity, it can still find and consume as per its consumption rate
         var resourceFound = World.OrganismSeeksFood(ConsumptionRatePerPeriod, AvailableStorageCapacity + ConsumptionRatePerPeriod);
         IncreaseResources(resourceFound);
         AddLogEntry(LogPriorityObtainedResource, "Found resources", StorageLevel, Starvation, resourceFound, LogLevel.EveryInterval);
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
         AddLogEntry(LogPriorityConsumeResource, "Consumed resources", StorageLevel, Starvation, consumption, LogLevel.EveryInterval);
      }

      /// <summary>
      /// Organism first consumes from personal storage. Once that's depleted it can consume from group resources.
      /// </summary>
      public void DecreaseResources(double consumption)
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

      public void DecreaseStorageLevel(double amount, string reason)
      {
         StorageLevel = Math.Max(0, StorageLevel - amount);
         AddLogEntry(LogPriorityLostStorage, $"Lost storage. Reason: {reason}", StorageLevel, Starvation, amount, LogLevel.Important);
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
         // Might already be dead because it was killed this period
         if (IsDead)
         {
            return;
         }
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
         MilitaryTraining();
      }

      /// <summary>
      /// If organism is not non militant then every period they gain military power due to training, etc.
      /// Activities such as stealing and killing or group sanctioned training may also contribute.
      /// </summary>
      private void MilitaryTraining()
      {
         switch ((MilitaryGene)GetGeneValueByType(GeneEnum.Military))
         {
            default:
               break;
            case MilitaryGene.Passive:
               MilitaryPower += MilitaryTrainingPassive;
               break;
            case MilitaryGene.Proactive:
               MilitaryPower += MilitaryTrainingActive;
               break;
            case MilitaryGene.Offender:
               MilitaryPower += MilitaryTrainingOffensive;
               break;
         }
         AddLogEntry(LogPriorityTraining, "Power increase", StorageLevel, Starvation, MilitaryPower, LogLevel.EveryInterval);
      }

      /// <summary>
      /// If individual has tendency to form/join groups and not already part of one, will try here to form or join a group with other similar organisms.
      /// Will search for a like-minded individual in its vicinity.
      /// If other individual is in a group then will join that group.
      /// If other individual is not in a group then both will form a new group.
      /// </summary>
      private void JoinGroup()
      {
         // Ignore if not inclined to join groups
         if (GetGeneValueByType(GeneEnum.Cooperation) == (int)CooperationGene.Solo)
         {
            return;
         }
         // Organism is cooperative and not already part of a group, so will look to join or form a group
         // Will search for a like-minded individual in its vicinity.
         // If other individual is in a group then will join that group.
         // If other individual is not in a group then both will form a new group.
         Organism similarOrganism = World.SearchVicinityForSimilarCooperativeIndividuals(this);
         if (similarOrganism == null)
         {
            return;
         }
         // Both cannot be in a group but one or none can be in a group. So 3 cases to cover
         if (!IsInGroup && !similarOrganism.IsInGroup)
         {
            // None are in a group. Form a new group.
            // Need to form a new group comprising of the two organisms
            bool formedGroup = FormGroup(similarOrganism);
            if (formedGroup)
            {
               AddLogEntry(LogPriorityFormJoinGroup, "Formed new Group", StorageLevel, Starvation, null, LogLevel.MostImportant);
               similarOrganism.AddLogEntry(LogPriorityFormJoinGroup, "Formed new Group", similarOrganism.StorageLevel, similarOrganism.Starvation, null, LogLevel.MostImportant);
            }
         }
         else if (!IsInGroup && similarOrganism.IsInGroup)
         {
            // we join the other group
            similarOrganism.Group.Join(this);
            AddLogEntry(LogPriorityFormJoinGroup, "Joined Group", StorageLevel, Starvation, Group.Id, LogLevel.MostImportant);
         }
         else if (IsInGroup && !similarOrganism.IsInGroup)
         {
            // other organism joins us
            Group.Join(similarOrganism);
            similarOrganism.AddLogEntry(LogPriorityFormJoinGroup, "Invited to Group", similarOrganism.StorageLevel, similarOrganism.Starvation, Group.Id, LogLevel.MostImportant);
         }
      }

      private bool FormGroup(Organism similarOrganism)
      {
         OrganismGroup newGroup = new OrganismGroup(World, this, similarOrganism);
         World.AddGroup(newGroup);
         return true;
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
         AddLogEntry(LogPriorityGaveBirth, "Gave birth", StorageLevel, Starvation, null, LogLevel.Important);
         var baby = NewBaby(World, this, babyGenes);
         baby.AddLogEntry(LogPriorityIsBorn, "Is born", StorageLevel, Starvation, null, LogLevel.EveryInterval);
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

      public void Die(string reason)
      {
         AddLogEntry(LogPriorityDeath, $"Dying: {reason}, Age:", StorageLevel, Starvation, Age, LogLevel.Important);
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

      public void AddLogEntry(string priority, string text, double storage, double starvation, double? qty = null, LogLevel level = LogLevel.All)
      {
         var logApproval = (int)level & (int)OrgLogLevel;
         if (logApproval > 0)
         {
            LogBook.Add(new Log(priority, text, storage, starvation, qty, TimeIdx));
         }
      }

      public double MilitaryPower { get; set; }
   }
}
