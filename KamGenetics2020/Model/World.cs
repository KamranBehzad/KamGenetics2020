using KamGenetics2020.Helpers;
using KamGeneticsLib.Model;
using KBLib.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;

namespace KamGenetics2020.Model
{
   [Serializable]
   public class World
   {
      private const int DefaultTimeIncrement = 1;

      // Population constants
      private const int GeneralMultiplier = 10;
      private const int InitialOrganismCount = 100 * GeneralMultiplier;
      private const int MaxPopulationToSupport = 100 * GeneralMultiplier;
      private const int MinPopulationToSupport = 100;
      private const int ExpectedOrganismLifetimeConsumption = 80;

      // Resource constants
      private const double DefaultMaxResourceLevel = double.MaxValue;

      // Group join constants
      // Economy Group Join: Economy-based probability for organism in category 1 to accept joining category 2.
      private const int EjFungal = 100;
      
      private const int EjWorkerWorker = 100;
      private const int EjWorkerSurvivor = 20;
      private const int EjWorkerThief = 0;
      private const int EjWorkerFungal = 5;

      private const int EjSurvivorWorker = 20;
      private const int EjSurvivorSurvivor = 100;
      private const int EjSurvivorThief = 0;
      private const int EjSurvivorFungal = 5;
      
      
      private const int EjThiefWorker = 0;
      private const int EjThiefSurvivor = 0;
      private const int EjThiefThief = 100;
      private const int EjThiefFungal = 20;

      // Military Group Join: Military-based probability for organism in category 1 to accept joining category 2.
      private const int MjNonNon = 100;
      private const int MjNonPassive = 50;
      private const int MjNonActive = 5;
      private const int MjNonOffensive = 0;

      private const int MjPassiveNon = 90;
      private const int MjPassivePassive = 100;
      private const int MjPassiveActive = 10;
      private const int MjPassiveOffensive = 0;

      private const int MjActiveNon = 70;
      private const int MjActivePassive = 80;
      private const int MjActiveActive = 100;
      private const int MjActiveOffensive = 10;

      private const int MjOffensiveNon = 0;
      private const int MjOffensivePassive = 0;
      private const int MjOffensiveActive = 20;
      private const int MjOffensiveOffensive = 100;

      // Stealing constants
      private const int BaseStealingModifier = 30;

      public World()
      {
         TimeIdx = 0;
         TimeIncrement = DefaultTimeIncrement;
         InitResourceLevel();
      }

      private void InitResourceLevel()
      {
         MaxResourceLevel = DefaultMaxResourceLevel;
         ResourceLevel = MaxPopulationToSupport * ExpectedOrganismLifetimeConsumption;
      }

      private List<Organism> _organisms;
      private List<Group> _groups;

      private List<Organism> Organisms
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

      private List<Group> Groups
      {
         get
         {
            if (_groups == null)
            {
               _groups = new List<Group>();
            }

            return _groups;
         }
      }

      private List<Organism> _deadOrganisms;

      public List<Organism> DeadOrganisms
      {
         get
         {
            if (_deadOrganisms == null)
            {
               _deadOrganisms = new List<Organism>();
            }

            return _deadOrganisms;
         }
      }

      private List<Organism> _babies;

      private List<Organism> Babies
      {
         get
         {
            if (_babies == null)
            {
               _babies = new List<Organism>();
            }

            return _babies;
         }
      }


      [Key]
      public int Id { get; set; }

      private void ReplenishResources()
      {
         var minReplenishment = MinPopulationToSupport;
         var maxReplenishment = MaxPopulationToSupport;
         var replenishment = ResourceHelper.GetResourceRegenerationByTimeIndexWithMean(TimeIdx, maxReplenishment);
         // cannot be below min population support level
         replenishment = Math.Max(replenishment, minReplenishment);
         var lastResourceLevel = ResourceLevel;
         ResourceLevel = Math.Min(MaxResourceLevel, ResourceLevel + replenishment);

         ThisPeriodStats.CalculatedReplenishmentAmount = replenishment;
         ThisPeriodStats.ActualReplenishmentAmount = ResourceLevel - lastResourceLevel;
         ThisPeriodStats.PeriodEndResourceLevel = ResourceLevel;
      }

      [NotMapped]
      public int TimeIdx { get; set; }

      [NotMapped]
      public int Population => Organisms.Count;

      private void Populate()
      {
         _organisms = new List<Organism>();
         for (var i = 0; i < InitialOrganismCount; i++)
         {
            var organism = new Organism(this, null);
            Organisms.Add(organism);
         }
      }

      public void SimulateSinglePeriod()
      {
         Thread.Sleep(10);
         ThisPeriodStats = new WorldStat
         {
            TimeIdx = TimeIdx,
            Population = Population,
            PeriodStartResourceLevel = ResourceLevel,
            MeanLibido = CalculateMeanLibido()
         };
         // Run world events
         // Run organism events
         foreach (var group in Groups)
         {
            group.Live();
         }
         //todo RemoveTheDead?();
         // Run organism events
         foreach (var organism in Organisms)
         {
            organism.Live();
         }
         RemoveTheDead();
         AddTheBabies();
         AdjustResourcesByCultivation();
         ReplenishResources();
         PeriodStats.Add(ThisPeriodStats);
         // Increment Time Index
         TimeIdx += TimeIncrement;
      }

      private double CalculateMeanLibido()
      {
         if (Population == 0)
         {
            return 0;
         }
         return Organisms.Average(o => o.GetGeneByType(GeneEnum.Libido).CurrentValue);
      }

      private void AddTheBabies()
      {
         ThisPeriodStats.Born = CurrentBabyCount;

         Organisms.AddRange(Babies);
         LastBabyCount = CurrentBabyCount;
         Babies.RemoveAll(r => true);
      }

      private int CurrentBabyCount => Babies.Count;
      private int CurrentDyingCount => Organisms.Count(o => o.IsDead);

      [NotMapped]
      public int LastBabyCount { get; private set; }

      [NotMapped]
      public int LastDyingCount { get; private set; }

      private void RemoveTheDead()
      {
         ThisPeriodStats.Died = CurrentDyingCount;
         LastDyingCount = CurrentDyingCount;
         // Adding to a public list for db persistence
         DeadOrganisms.AddRange(Organisms.Where(r => r.IsDead));
         Organisms.RemoveAll(r => r.IsDead);
      }

      public void AddBaby(Organism baby)
      {
         Babies.Add(baby);
      }

      public void AddGroup(Group group)
      {
         Groups.Add(group);
      }

      public void Reset()
      {
         TimeIdx = 0;
         Populate();
      }

      /// <summary>
      /// Units of time incremented at each step of the simulation
      /// </summary>
      [NotMapped]
      public int TimeIncrement { get; set; }

      private int GetResourcePerPopulation()
      {
         double resourcePerCapita = ResourceLevel / Population;
         var result = resourcePerCapita >= int.MaxValue ? int.MaxValue : (int)resourcePerCapita;
         return result;
      }

      [NotMapped]
      public double ResourceLevel { get; set; }

      [NotMapped]
      public double PeriodCultivation { get; set; }

      [NotMapped]
      public double MaxResourceLevel { get; set; }

      public DateTime Modified { get; set; }

      /// <summary>
      /// We cannot reduce resources every time an organism finds and takes food because then resource levels diminish before other organisms
      /// have even began searching. It disrupts the availability per organism. We must accumulate all resources cultivated and reduce
      /// the resource level after all organisms have been simulated.
      /// </summary>
      private void AccumulateCultivations(double cultivation)
      {
         PeriodCultivation += cultivation;
      }


      private double AdjustResourcesByCultivation()
      {
         ThisPeriodStats.PeriodCultivation = PeriodCultivation;

         ResourceLevel -= PeriodCultivation;
         // Reset cultivation
         PeriodCultivation = 0;
         // Can never exceed capacity
         ResourceLevel = Math.Max(ResourceLevel, 0);
         return ResourceLevel;
      }

      /// <summary>
      /// Return how much resources the organism was able to find.
      /// Organism searches for maximum of (consumptionRatePerPeriod, allItCanHandle+consumptionRatePerPeriod).
      ///
      /// How much it finds depends on world resource levels and population and some luck
      /// </summary>
      public double OrganismSeeksFood(double consumptionRatePerPeriod, double allItCanHandle, bool useLuck = true)
      {
         // If there is enough food for all then return full amount requested
         // otherwise return a proportion
         var availableResourcesPerOrganism = ResourceLevel / Population;
         var amountFound = Math.Min(availableResourcesPerOrganism, Math.Max(consumptionRatePerPeriod, allItCanHandle));

         // vary amount found based on luck, set to false to return same amount for all organism
         if (useLuck)
         {
            amountFound = ApplyLuckToResourceFoundLevel(amountFound, allItCanHandle);
         }
         // record how much was cultivated from the world
         AccumulateCultivations(amountFound);
         return amountFound;
      }

      private double ApplyLuckToResourceFoundLevel(double amountFound, double allItCanHandle)
      {
         double luckVariationPercentage = 50;
         double luck = RandomHelper.StandardGeneratorInstance.NextDouble(-luckVariationPercentage, luckVariationPercentage);
         amountFound *= (1 + luck / 100);
         // Never more than it can handle
         amountFound = Math.Min(amountFound, allItCanHandle);

         // Can never exceed world resource availability
         amountFound = Math.Min(amountFound, ResourceLevel - PeriodCultivation);

         return amountFound;
      }

      private WorldStat ThisPeriodStats { get; set; }
      private List<WorldStat> _periodStats;

      public List<WorldStat> PeriodStats
      {
         get
         {
            if (_periodStats == null)
            {
               _periodStats = new List<WorldStat>();
            }

            return _periodStats;
         }
      }

      public double RecordOrganismConsumptionInCurrentPeriod(double consumption)
      {
         ThisPeriodStats.PeriodConsumption += consumption;
         return ThisPeriodStats.PeriodConsumption;
      }

      /// <summary>
      /// Used when a cooperative organism is searching its vicinity for another cooperative organism that is similar minded.
      /// Cannot search just vicinity because if there is no one in vicinity then for many iterations this will not change
      /// Also individuals meet different individuals every iteration in real life. So meetings must be random
      /// </summary>
      public Organism SearchVicinityForSimilarCooperativeIndividuals(Organism organism)
      {
         if (Population < 2)
         {
            return null;
         }
         int searchCount = MaxPopulationToSupport / 10;
         var curIdx = Organisms.IndexOf(organism);

         for (int i = 0; i < searchCount; i++)
         {
            int foundIdx;
            // find random organism, ignore self
            do
               foundIdx = RandomHelper.StandardGeneratorInstance.Next(0, Population);
            while (curIdx == foundIdx);
            if (AreMatched(organism, Organisms[foundIdx]))
            {
               return Organisms[foundIdx];
            }
         }
         return null;
      }

      /// <summary>
      /// Checks if current organism and another organism in the vicinity are a good match to form a group.
      /// To be a good match:
      /// 1. The organism in vicinity must also be cooperative and not solo
      /// 2. Both organisms must have similar/close economy gene value (worker. thief, etc.)
      /// 3.  Both organisms must have similar/close military gene value (non, passive, etc.)
      /// </summary>
      private bool AreMatched(Organism curOrganism, Organism vicinityOrganism)
      {
         if (vicinityOrganism.GetGeneValueByType(GeneEnum.Cooperation) == (int)CooperationGene.Solo)
         {
            return false;
         }
         //todo decide how group mergers occur
         // return false if both organisms are already in a group
         if (curOrganism.IsInGroup && vicinityOrganism.IsInGroup)
         {
            return false;
         }

         var curOrganismEconomyMatchPercentage = GetEconomyMatchPercentage(curOrganism, vicinityOrganism);
         var vicinityOrganismEconomyMatchPercentage = GetEconomyMatchPercentage(vicinityOrganism, curOrganism);

         var curOrganismMilitaryMatchPercentage = GetMilitaryMatchPercentage(curOrganism, vicinityOrganism);
         var vicinityOrganismMilitaryMatchPercentage = GetMilitaryMatchPercentage(vicinityOrganism, curOrganism);
         // If any of above are 0 then there is no joining
         if ((curOrganismEconomyMatchPercentage * curOrganismMilitaryMatchPercentage * vicinityOrganismEconomyMatchPercentage * vicinityOrganismMilitaryMatchPercentage).Equals(0))
         {
            return false;
         }

         // todo What is the weight on economy vs military match?
         // for now lets give equal weight to economy vs military match
         double economyWeight = 1;
         double militaryWeight = 1;
         double sumWeights = economyWeight + militaryWeight;

         double curOrganismAcceptancePercentage = (economyWeight * curOrganismEconomyMatchPercentage + militaryWeight * curOrganismMilitaryMatchPercentage) / (sumWeights * 100);
         double vicinityOrganismAcceptancePercentage = (economyWeight * vicinityOrganismEconomyMatchPercentage + militaryWeight * vicinityOrganismMilitaryMatchPercentage) / (sumWeights * 100);
         // Both parties must accept one another for the join to happen
         bool curOrganismAccepts = RandomHelper.StandardGeneratorInstance.NextDouble() < curOrganismAcceptancePercentage;
         bool vicinityOrganismAccepts = RandomHelper.StandardGeneratorInstance.NextDouble() < vicinityOrganismAcceptancePercentage;
         return curOrganismAccepts && vicinityOrganismAccepts;
      }

      /// <summary>
      /// Economy Match Score = 0-100
      /// Fungal types will join anyone.
      /// Workers and survivors will join each other. Higher rate if both organisms are identical
      /// Thieves only join thieves.
      /// All non-fungals may still accept to join fungals because of the strength in numbers. More so in case of thieves since they are stealing
      /// any way and greater numbers works better and hardly much resources are lost to fungals.
      /// </summary>
      private double GetEconomyMatchPercentage(Organism curOrganism, Organism vicinityOrganism)
      {
         EconomyGene currentOrganismEconomy = curOrganism.IsInGroup
            ? curOrganism.Group.GroupEconomyGene
            : (EconomyGene)curOrganism.GetGeneValueByType(GeneEnum.Economy);
         EconomyGene vicinityOrganismEconomy = vicinityOrganism.IsInGroup
            ? vicinityOrganism.Group.GroupEconomyGene
            : (EconomyGene)vicinityOrganism.GetGeneValueByType(GeneEnum.Economy);
         switch (currentOrganismEconomy)
         {
            default:
               return 0;
            case EconomyGene.Fungal:
               // Fungal types always want to join 
               return EjFungal;
            case EconomyGene.Worker:
               switch (vicinityOrganismEconomy)
               {
                  default:
                     return 0;
                  case EconomyGene.Fungal:
                     return EjWorkerFungal;
                  case EconomyGene.Worker:
                     return EjWorkerWorker;
                  case EconomyGene.Survivor:
                     return EjWorkerSurvivor;
               }
            case EconomyGene.Survivor:
               switch (vicinityOrganismEconomy)
               {
                  default:
                     return 0;
                  case EconomyGene.Fungal:
                     return EjSurvivorFungal;
                  case EconomyGene.Worker:
                     return EjSurvivorWorker;
                  case EconomyGene.Survivor:
                     return EjSurvivorSurvivor;
               }
            case EconomyGene.Thief:
               switch (vicinityOrganismEconomy)
               {
                  default:
                     return 0;
                  case EconomyGene.Fungal:
                     return EjThiefFungal;
                  case EconomyGene.Thief:
                     return EjThiefThief;
               }
         }
      }

      /// <summary>
      /// Military Match Score = 0-100
      /// NonMilitants and passives will join anyone but offenders.
      /// Actives will join anyone.
      /// Actives and offenders join one of their own kind and a small chance to join one another.
      /// For actives and passives, there is a higher chance to accept someone less offensive than yourself than more offensive.
      /// </summary>
      private double GetMilitaryMatchPercentage(Organism curOrganism, Organism vicinityOrganism)
      {
         var currentOrganismMilitary = curOrganism.IsInGroup
            ? curOrganism.Group.GroupMilitaryGene
            : (MilitaryGene)curOrganism.GetGeneValueByType(GeneEnum.Military);
         var vicinityOrganismMilitary = vicinityOrganism.IsInGroup
            ? vicinityOrganism.Group.GroupMilitaryGene
            : (MilitaryGene)vicinityOrganism.GetGeneValueByType(GeneEnum.Military);
         switch (currentOrganismMilitary)
         {
            default:
               return 0;
            case MilitaryGene.NonMilitant:
               switch (vicinityOrganismMilitary)
               {
                  default:
                     return 0;
                  case MilitaryGene.NonMilitant:
                     return MjNonNon;
                  case MilitaryGene.Passive:
                     return MjNonPassive;
                  case MilitaryGene.Proactive:
                     return MjNonActive;
               }
            case MilitaryGene.Passive:
               switch (vicinityOrganismMilitary)
               {
                  default:
                     return 0;
                  case MilitaryGene.NonMilitant:
                     return MjPassiveNon;
                  case MilitaryGene.Passive:
                     return MjPassivePassive;
                  case MilitaryGene.Proactive:
                     return MjPassiveActive;
               }
            case MilitaryGene.Proactive:
               switch (vicinityOrganismMilitary)
               {
                  default:
                     return 0;
                  case MilitaryGene.NonMilitant:
                     return MjActiveNon;
                  case MilitaryGene.Passive:
                     return MjActivePassive;
                  case MilitaryGene.Proactive:
                     return MjActiveActive;
                  case MilitaryGene.Offender:
                     return MjActiveOffensive;
               }
            case MilitaryGene.Offender:
               switch (vicinityOrganismMilitary)
               {
                  default:
                     return 0;
                  case MilitaryGene.Proactive:
                     return MjOffensiveActive;
                  case MilitaryGene.Offender:
                     return MjOffensiveOffensive;
               }
         }
      }

      public double OrganismStealsFood(Organism organism, int killProbability)
      {
         if (Population < 2)
         {
            // Nobody to steal from
            return 0;
         }
         Organism victim = null;
         int searchCount = MaxPopulationToSupport / 10;
         var curIdx = Organisms.IndexOf(organism);

         bool killsVictim = false;
         for (int i = 0; i < searchCount; i++)
         {
            int foundIdx;
            // find random organism, ignore self
            do
               foundIdx = RandomHelper.StandardGeneratorInstance.Next(0, Population);
            while (curIdx == foundIdx);
            if (CanSteal(organism, Organisms[foundIdx], killProbability, out killsVictim))
            {
               victim = Organisms[foundIdx];
               break;
            }
         }

         if (victim == null)
         {
            return 0;
         }
         // Steal from found victim
         // How much is stolen?
         // Let's assume 50% of victim's inventory is stolen. If the victim is killed then 100% of the inventory is stolen.
         double stolenAmount = killsVictim ? victim.StorageLevel : victim.StorageLevel / 2;
         if (killsVictim)
         {
            victim.Die("Killed");
         }
         else
         {
            victim.DecreaseStorageLevel(stolenAmount, "Stolen");
         }
         return stolenAmount;
      }

      private bool CanSteal(Organism thief, Organism victim, int killProbability, out bool killsVictim)
      {
         killsVictim = false;
         // Cannot steal if not mature yet
         if (!thief.IsMatureToSteal)
         {
            return false;
         }
         // The success of the steal depends on individual military preparedness plus chance
         var baseSuccessPercentage = Math.Max(0, thief.MilitaryPower - victim.MilitaryPower + BaseStealingModifier);
         // Add any bonus if willing to kill
         var killSuccessPercentage = baseSuccessPercentage + killProbability;
         // Randomly determine if victim was killed, robbed or none
         var rand = 100 * RandomHelper.StandardGeneratorInstance.NextDouble();
         if (rand < baseSuccessPercentage)
         {
            return true;
         }
         if (rand < killSuccessPercentage)
         {
            killsVictim = true;
            return true;
         }
         // todo: should we allow for possibility that victim is killed but the robbery is still unsuccessful? Can happen in real life!
         return false;
      }
   }
}
