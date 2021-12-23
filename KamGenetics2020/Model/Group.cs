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
    [DebuggerDisplay("Id:{Id} Pop:{Population}")]
    public class Group
    {
        private static int DefaultThiefGroupPopulationLimit = 10;
        private static int DefaultWorkerGroupPopulationLimit = 500;

        private LogLevel GroupLogLevel = LogLevel.All;

        // Log constants
        private const string LogPriorityIsFormed = "10010";
        private const string LogPriorityLive = "10020";

        public Group()
        {
        }

        public Group(World world, Organism organism1, Organism organism2) : this()
        {
            World = world;
            Add(organism1);
            Add(organism2);
        }
        public World World { get; set; }


        [Key]
        public int Id { get; set; }

        public int Population => Organisms.Count;

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

        public Group Add(Organism organism)
        {
            Organisms.Add(organism);
            organism.Group = this;
            organism.GroupId = Id;
            StorageCapacity += organism.StorageCapacity;
            StorageLevel += organism.StorageLevel;
            EconomyScore = GetEconomyScore();
            MilitaryScore = GetMilitaryScore();
            return this;
        }

        public bool Join(Organism organism)
        {
            var canJoin = CanJoin();
            if (!canJoin)
            {
                return false;
            }
            Organisms.Add(organism);
            organism.Group = this;
            organism.GroupId = Id;
            StorageCapacity += organism.StorageCapacity;
            StorageLevel += organism.StorageLevel;
            EconomyScore = GetEconomyScore();
            MilitaryScore = GetMilitaryScore();
            return true;
        }

        public Group Remove(Organism organism)
        {
            // we do not physically remove the organism from the group for record keeping purposes
            DepartedOrganisms.Add(organism);
            Organisms.Remove(organism);
            // A member is gone. Capacity is diminished. Ensure actual level does not exceed capacity.
            //StorageCapacity -= organism.StorageCapacity;
            //StorageLevel = Math.Min(StorageLevel, StorageCapacity);
            EconomyScore = GetEconomyScore();
            MilitaryScore = GetMilitaryScore();
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

        public double EconomyScore { get; set; }

        private double GetEconomyScore()
        {
            return Population == 0
               ? 0
               : Organisms.Average(org => (double)org.GetGeneValueByType(GeneEnum.Economy));
        }

        public double MilitaryScore { get; set; }

        private double GetMilitaryScore()
        {
            return Population == 0
               ? 0
               : Organisms.Average(org => (double)org.GetGeneValueByType(GeneEnum.Military));
        }

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

        /// <summary>
        /// How a group lives in one year
        /// Only action for now is if group will attack another group or not
        /// </summary>
        public void Live()
        {
            ThisPeriodStats = new GroupStat()
            {
                TimeIdx = TimeIdx,
                PeriodStartResourceLevel = StorageLevel,
                EconomyScore = EconomyScore,
                MilitaryScore = MilitaryScore,
                Population = this.Population,
            };

            AddLogEntry(LogPriorityLive, "Population", StorageLevel, Population, LogLevel.EveryInterval);
            AddLogEntry(LogPriorityLive, "Economy", StorageLevel, EconomyScore, LogLevel.EveryInterval);
            AddLogEntry(LogPriorityLive, "Military", StorageLevel, MilitaryScore, LogLevel.EveryInterval);

            PeriodStats.Add(ThisPeriodStats);
        }

        private List<LogGroup> _logBook;

        //[NotMapped]
        public List<LogGroup> LogBook
        {
            get
            {
                if (_logBook == null)
                {
                    _logBook = new List<LogGroup>();
                }

                return _logBook;
            }
        }

        public void AddLogEntry(string priority, string text, double storage, double? qty = null, LogLevel level = LogLevel.All)
        {
            var logApproval = (int)level & (int)GroupLogLevel;
            if (logApproval > 0)
            {
                LogBook.Add(new LogGroup(priority, text, storage, qty, TimeIdx));
            }
        }

        public int TimeIdx => World.TimeIdx;

        private GroupStat ThisPeriodStats { get; set; }
        private List<GroupStat> _periodStats;

        public List<GroupStat> PeriodStats
        {
            get
            {
                if (_periodStats == null)
                {
                    _periodStats = new List<GroupStat>();
                }

                return _periodStats;
            }
        }

        /// <summary>
        /// Absolute maximum acceptaable population is 120% of the default limit
        /// At 80% and below we accept @ 100% rate.
        /// At 100% we accept @ 50% rate.
        /// At 120% we accept @ 0% rate.
        /// </summary>
        /// <returns></returns>
        public double GetJoinProbability()
        {
            var PopulationLimit = (EconomyScore < 2.5) ? DefaultWorkerGroupPopulationLimit : DefaultThiefGroupPopulationLimit;
            double result = Math.Max(100, 1.2 - (Population / PopulationLimit) * 2.5);
            return result;
        }

        public bool CanJoin()
        {
            var rand = RandomHelper.StandardGeneratorInstance.NextDouble();
            return rand <= GetJoinProbability();
        }
    }
}