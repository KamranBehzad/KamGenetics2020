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
    //[Serializable]
    [DebuggerDisplay("Id:{Id} Group:{GroupId}")]
    public class Organism
    {
        // Organism ID
        private static long NextOrgId = 1;

        protected LogLevel MasterLogLevel = LogLevel.All;
        // Procreation constant
        private const bool SelfAdjustingLibido = true;  // When true libido adjusts down when resources are low.

        // Age constants
        private const int MaxAge = 80;
        private const int MaturityStartSexual = 15;
        private const int MaturityFinishSexual = 65;

        private const int MaturityStartMigration = 12;

        // Termination constants
        private const double TerminationByAccidentPercentage = 0.02;
        private const double TerminationByShortageMigrationPercentage = 10;
        private const double TerminationByVoluntaryMigrationPercentage = 0.1;

        // Log constants
        private const string LogPriorityIsBorn = "00010";
        private const string LogPriorityFormJoinGroup = "00100";
        private const string LogPriorityStoleResourceBefore = "00190";
        private const string LogPriorityFoundResource = "00200";
        private const string LogPriorityStoleResourceAfter = "00210";
        private const string LogPriorityConsumeResource = "00300";
        private const string LogPriorityGaveBirth = "00400";
        private const string LogPriorityResourceExchange = "00500";
        private const string LogPriorityTraining = "00600";
        private const string LogPriorityLostStorage = "00700";
        private const string LogPriorityTermination = "09999";

        // Stealing constants
        private const int MaturityStartStealing = 10;

        private const int StealLowProbability = 10;
        private const int StealMedProbability = 50;
        private const int StealHiProbability = 90;
        private const int StealFullProbability = 100;

        private const double KillNoProbability = 0;
        private const double KillLowProbability = 0.001;
        private const double KillMedProbability = 0.005;
        private const double KillHiProbability = 0.02;

        // Military training constants
        private const int MaturityStartCombat = 15;

        private const double MilitaryTrainingPassive = 1.0;
        private const double MilitaryTrainingActive = 1.2;
        private const double MilitaryTrainingOffensive = 1.4;

        private const double MtPassiveMinAge = 15;
        private const double MtActiveMinAge = 10;
        private const double MtOffensiveMinAge = 5;

        public Organism()
        {
        }

        public Organism(World world, Organism parent) : this()
        {
            World = world;
            IdAux = GetNextOrganismId();
            Parent = parent;
            ParentId = Parent?.Id;

            Group = null;
            GroupId = null;

            Dob = TimeIdx;

            ConsumptionPerPeriod = World.DefaultConsumptionPerPeriod;
            StorageLevel = World.InitialStorageLevel;
            InitGenes();
        }

        public Organism NewBaby(World world, Organism parent, List<Gene> genes)
        {
            var baby = new Organism
            {
                World = world,
                IdAux = GetNextOrganismId(),
                Parent = parent,
                ParentId = parent?.Id,
                Dob = TimeIdx,
                ConsumptionPerPeriod = World.DefaultConsumptionPerPeriod,
                StorageLevel = World.InitialStorageLevel,
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
            AddLogEntry(LogPriorityResourceExchange, "Gave resources", exchangeQty, LogLevel.Important);
            organismReceiver.AddLogEntry(LogPriorityResourceExchange, "Received resources", exchangeQty, LogLevel.Important);
        }

        private void InitGenes()
        {
            Genes.Add(GeneHelper.CreateCooperationGene(RandomHelper.StandardGeneratorInstance.Next(1, 2)));
            Genes.Add(GeneHelper.CreateLibidoGene());

            Genes.Add(GeneHelper.CreateEconomyGeneVariations());
            Genes.Add(GeneHelper.CreateMilitaryGeneVariations());

        }

        [Key]
        public long Id { get; set; }
        public long IdAux { get; set; }

        public int TimeIdx => World.TimeIdx;

        public long? ParentId { get; private set; }

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
        /// Resources can be obtained by seeking the world (hunting & gathering and cultivating) or by stealing.
        /// How it's done and in what order is governed by genes.
        /// </summary>
        private void ObtainResources()
        {
            double stolenResources = 0;
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
                            StealResources(StealLowProbability, false);
                            break;
                        case MilitaryGene.Proactive:
                            StealResources(StealMedProbability, false);
                            break;
                        case MilitaryGene.Offender:
                            StealResources(StealHiProbability, false);
                            break;
                    }

                    break;
                case EconomyGene.Thief:
                    // Steal first
                    switch ((MilitaryGene)GetGeneValueByType(GeneEnum.Military))
                    {
                        default:
                            stolenResources = StealResources(StealFullProbability, true, KillNoProbability);
                            break;
                        case MilitaryGene.Passive:
                            stolenResources = StealResources(StealFullProbability, true, KillLowProbability);
                            break;
                        case MilitaryGene.Proactive:
                            stolenResources = StealResources(StealFullProbability, true, KillMedProbability);
                            break;
                        case MilitaryGene.Offender:
                            stolenResources = StealResources(StealFullProbability, true, KillHiProbability);
                            break;
                    }

                    // Seek if steal yield was not enough
                    if ((StorageLevel >= 1 || Starvation.Equals(0)) && !stolenResources.Equals(0))
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
                            StealResources(StealFullProbability, true);
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

        private double StealResources(int stealProbability, bool beforeFoodSeek, double killProbability = 0)
        {
            var random = 100 * RandomHelper.StandardGeneratorInstance.NextDouble();
            if (random > stealProbability)
            {
                return 0;
            }
            double resourceStolen = World.OrganismStealsFood(this, killProbability);
            if (resourceStolen.Equals(0))
            {
                return 0;
            }
            IncreaseResources(resourceStolen);
            ThisPeriodStats.FoodStolen = resourceStolen;

            if (beforeFoodSeek)
            {
                AddLogEntry(LogPriorityStoleResourceBefore, "Stole resources", resourceStolen, LogLevel.Important);
            }
            else
            {
                AddLogEntry(LogPriorityStoleResourceAfter, "Stole resources", resourceStolen, LogLevel.Important);
            }
            return resourceStolen;
        }

        private void SeekResources()
        {
            // +consumptionRatePerPeriod is because when organism is at full storage capacity, it can still find and consume as per its consumption rate
            var resourceFound = World.OrganismSeeksFood(ConsumptionPerPeriod, AvailableStorageCapacity + ConsumptionPerPeriod);
            IncreaseResources(resourceFound);
            ThisPeriodStats.PeriodCultivation = resourceFound;
            AddLogEntry(LogPriorityFoundResource, "Found resources", resourceFound, LogLevel.EveryInterval);
        }

        private void IncreaseResources(double resourceObtained)
        {
            // First fills personal storage then contributes the rest to the group if any
            var maxPersonalShare = StorageCapacity + ConsumptionPerPeriod - StorageLevel;
            var personalShare = Math.Min(maxPersonalShare, resourceObtained);

            StorageLevel += personalShare;
            StorageLevel = Math.Min(StorageLevel, StorageCapacity + ConsumptionPerPeriod);
            if (IsInGroup)
            {
                var groupShare = resourceObtained - personalShare;
                Group.IncreaseResources(groupShare);
                ThisPeriodStats.FoodToGroup = groupShare;
            }
        }

        public double AvailableStorageCapacity =>
          PersonalAvailableStorageCapacity + (Group?.AvailableStorageCapacity ?? 0);

        private double PersonalAvailableStorageCapacity => StorageCapacity - StorageLevel;

        private void ConsumeResources()
        {
            double consumption = Math.Min(AvailableResources, ConsumptionPerPeriod);
            World.RecordOrganismConsumptionInCurrentPeriod(consumption);
            DecreaseResources(consumption);
            ThisPeriodStats.PeriodConsumption = consumption;
            ThisPeriodStats.PeriodEndResourceLevel = StorageLevel;

            // todo develop consumption & starvation model
            if (consumption < ConsumptionPerPeriod)
            {

                Starvation += (ConsumptionPerPeriod - consumption);
                if (Starvation > ConsumptionPerPeriod)
                {
                    Terminate("Starvation");
                }
            }
            else
            {
                Starvation = 0;
            }
            AddLogEntry(LogPriorityConsumeResource, "Consumed resources", consumption, LogLevel.EveryInterval);
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
                ThisPeriodStats.FoodFromGroup = consumption;
            }
        }

        public void DecreaseStorageLevel(double amount, string reason)
        {
            StorageLevel = Math.Max(0, StorageLevel - amount);
            AddLogEntry(LogPriorityLostStorage, $"Lost storage. Reason: {reason}", amount, LogLevel.Important);
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
            ThisPeriodStats = new OrganismStat()
            {
                TimeIdx = TimeIdx,
                PeriodStartResourceLevel = StorageLevel,
            };

            // Might already be terminated because it was killed this period
            if (IsTerminated)
            {
                return;
            }
            var (result, reason) = WillTerminate();
            if (result)
            {
                Terminate(reason);
                return;
            }

            JoinGroup();
            ObtainResources();
            ConsumeResources();
            ProcreateAsexual();
            MilitaryTraining();
            RecordPeriodStats();
        }

        private void RecordPeriodStats()
        {
            ThisPeriodStats.TimeIdx = TimeIdx;
            PeriodStats.Add(ThisPeriodStats);
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
                    if (Age >= MtPassiveMinAge)
                    {
                        MilitaryPower += MilitaryTrainingPassive;
                    }
                    break;
                case MilitaryGene.Proactive:
                    if (Age >= MtActiveMinAge)
                    {
                        MilitaryPower += MilitaryTrainingActive;
                    }
                    break;
                case MilitaryGene.Offender:
                    if (Age >= MtOffensiveMinAge)
                    {
                        MilitaryPower += MilitaryTrainingOffensive;
                    }
                    break;
            }
            // Non militants do not increase power so no need to log for them
            if (MilitaryPower > 0)
            {
                AddLogEntry(LogPriorityTraining, "Power increase", MilitaryPower, LogLevel.EveryInterval);
            }
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
                    AddLogEntry(LogPriorityFormJoinGroup, "Formed new Group", null, LogLevel.MostImportant);
                    similarOrganism.AddLogEntry(LogPriorityFormJoinGroup, "Formed new Group", null, LogLevel.MostImportant);
                }
            }
            else if (!IsInGroup && similarOrganism.IsInGroup)
            {
                // we join the other group
                similarOrganism.Group.Join(this);
                AddLogEntry(LogPriorityFormJoinGroup, "Joined Group", Group.Id, LogLevel.MostImportant);
            }
            else if (IsInGroup && !similarOrganism.IsInGroup)
            {
                // other organism joins us
                Group.Join(similarOrganism);
                similarOrganism.AddLogEntry(LogPriorityFormJoinGroup, "Invited to Group", Group.Id, LogLevel.MostImportant);
            }
        }

        private bool FormGroup(Organism similarOrganism)
        {
            Group newGroup = new Group(World, this, similarOrganism);
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
            AddLogEntry(LogPriorityGaveBirth, "Gave birth", null, LogLevel.Important);
            var baby = NewBaby(World, this, babyGenes);
            baby.AddLogEntry(LogPriorityIsBorn, "Is born", null, LogLevel.EveryInterval);
            return baby;
        }

        /// <summary>
        /// 211220 - Adding a dependency on available resources. Organism's chance of giving birth
        /// will also depend on how much resources it has (self adjusting). If it's low on resources then 
        /// chances of procreating is reduced according to the following table (roughly).
        /// 
        /// Resource    Procreation %
        /// 1.1     		 2
        /// 1.2     		 9
        /// 1.3     		16
        /// 1.4     		23
        /// 1.5     		30
        /// 1.6     		37
        /// 1.7     		44
        /// 1.8     		51
        /// 1.9     		58
        /// 2	        	65
        /// 2.1     		72
        /// 2.2     		79
        /// 2.3     		86
        /// 2.4     		93
        /// 2.5+     	   100
        /// </summary>
        /// <returns></returns>
        private bool CanProcreate(bool isSelfAdjusting = SelfAdjustingLibido)
        {
            bool result = false;
            double libido = (double)GetGeneValueByType(GeneEnum.Libido) / 100;
            double rand = RandomHelper.StandardGeneratorInstance.NextDouble();
            if (isSelfAdjusting)
            {
                const double resourceDependencyCoef = 0.70;
                double resourceAvailabilityModifier = 1 - (World.DefaultStorageCapacity / 2 - AvailableResources) * resourceDependencyCoef;
                double resourceAvailabilityModifierBounded = Math.Min(Math.Max(resourceAvailabilityModifier, 0), 1);
                double resourceModifiedLibido = libido * resourceAvailabilityModifierBounded;
                result = Age >= MaturityStartSexual
                    && Age <= MaturityFinishSexual
                    && rand < resourceModifiedLibido;
            }
            else
            {
                result = Age >= MaturityStartSexual
                    && Age <= MaturityFinishSexual
                    && rand < libido;
            }
            return result;
        }

        public World World { get; set; }

        private (bool result, string reason) WillTerminate()
        {
            var terminationByAge = Age > MaxAge + RandomHelper.StandardGeneratorInstance.Next(20) - 10;
            if (terminationByAge)
            {
                return (true, "Age");
            }

            var terminationByAccident = RandomHelper.StandardGeneratorInstance.NextDouble() * 100 < TerminationByAccidentPercentage;
            if (terminationByAccident)
            {
                return (true, "Accident");
            }

            if (FacesShortage())
            {
                var terminationByMigrationShortage = RandomHelper.StandardGeneratorInstance.NextDouble() * 100 < TerminationByShortageMigrationPercentage * ShortagesExperienced &&
                    Age >= MaturityStartMigration;
                if (terminationByMigrationShortage)
                {
                    return (true, "MigrationShortage");
                }
            }

            var terminationByMigrationVolunteer = RandomHelper.StandardGeneratorInstance.NextDouble() * 100 < TerminationByVoluntaryMigrationPercentage &&
                    Age >= MaturityStartMigration;
            if (terminationByMigrationVolunteer)
            {
                return (true, "MigrationVolunteer");
            }
            return (false, string.Empty);
        }

        /// <summary>
        /// Shortage is sensed if available resources are less than N periods worth of consumption.
        /// </summary>
        /// <returns></returns>
        private bool FacesShortage()
        {
            int safetyPeriodCount = 4;
            bool result = AvailableResources < World.DefaultConsumptionPerPeriod * safetyPeriodCount;
            if (result)
            {
                ShortagesExperienced++;
            }
            return result;
        }

        public int Age => TimeIdx - Dob;
        public int Dob { get; set; }
        public int Dot { get; set; }
        public int FinalAge { get; set; }

        public void Terminate(string reason)
        {
            AddLogEntry(LogPriorityTermination, $"Terminating: {reason}, Age:", Age, LogLevel.Important);
            IsTerminated = true;
            TerminationReason = reason;
            Dot = TimeIdx;
            FinalAge = Age;
            // Remove from group if any
            Group?.Remove(this);
        }

        public string TerminationReason { get; set; }
        public DateTime Modified { get; set; }

        [NotMapped]
        public bool IsTerminated { get; set; }

        [NotMapped]
        public double ConsumptionPerPeriod { get; set; }

        public int? GroupId { get; set; }

        [NotMapped]

        public Group Group { get; set; }

        public double StorageCapacity => World.DefaultStorageCapacity;

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

        private List<OrganismLog> _logBook;

        //[NotMapped]
        public List<OrganismLog> LogBook
        {
            get
            {
                if (_logBook == null)
                {
                    _logBook = new List<OrganismLog>();
                }

                return _logBook;
            }
        }

        public void AddLogEntry(string priority, string text, double? qty = null, LogLevel level = LogLevel.All)
        {
            var logApproval = (int)level & (int)MasterLogLevel;
            if (logApproval > 0)
            {
                LogBook.Add(new OrganismLog(this, priority, text, qty, TimeIdx));
            }
        }

        public double MilitaryPower { get; set; }

        public bool IsMatureToSteal => Age >= MaturityStartStealing;

        private OrganismStat ThisPeriodStats { get; set; }
        private List<OrganismStat> _periodStats;

        public List<OrganismStat> PeriodStats
        {
            get
            {
                if (_periodStats == null)
                {
                    _periodStats = new List<OrganismStat>();
                }

                return _periodStats;
            }
        }

        public int ShortagesExperienced { get; private set; }

        private long GetNextOrganismId()
        {
            return NextOrgId++;
        }
    }
}
