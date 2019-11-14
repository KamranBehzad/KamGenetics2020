using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using KamGenetics2020.Helpers;
using KamGeneticsLib.Model;
using KBLib.Helpers;

namespace KamGenetics2020.Model
{
    [Serializable]
    public class World
    {
        private const int DefaultTimeIncrement = 1;

        // Population constants
        private const int OrganismCount = 100;
        private const int MaxPopulationToSupport = 100;
        private const int MinPopulationToSupport = 50;
        private const int MeanConsumption = 100;

        // Resource constants
        private const double DefaultMaxResourceLevel = double.MaxValue;

        public World()
        {
            TimeIdx = 0;
            TimeIncrement = DefaultTimeIncrement;
            InitResourceLevel();
        }

        private void InitResourceLevel()
        {
            MaxResourceLevel = DefaultMaxResourceLevel;
            ResourceLevel = MaxPopulationToSupport * MeanConsumption;
        }

        private List<Organism> _organisms;
        private List<OrganismGroup> _groups;

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

        private List<OrganismGroup> Groups
        {
           get
           {
              if (_groups == null)
              {
                 _groups = new List<OrganismGroup>();
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
            for (var i = 0; i < OrganismCount; i++)
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

        public void AddGroup(OrganismGroup group)
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
            var amountFound = Math.Min(availableResourcesPerOrganism,  Math.Max(consumptionRatePerPeriod, allItCanHandle));

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
            amountFound = Math.Min(amountFound, ResourceLevel-PeriodCultivation);

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
        /// Similar minded means they have the same economy gene value (
        /// </summary>
        public Organism SearchVicinityForSimilarCooperativeIndividuals(Organism organism)
        {
           return null;
        }
    }
}
