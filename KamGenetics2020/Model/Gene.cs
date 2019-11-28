using System;
using System.ComponentModel.DataAnnotations;
using KamGenetics2020.Helpers;
using KamGeneticsLib.Model;
using KBLib.Helpers;

namespace KamGenetics2020.Model
{
    [Serializable]
    public class Gene
    {
        private const double MutationProbability = 0.1;
        private const double DrasticMutationProbability = 0.1;
        private int _currentValue;

        public Gene()
        {
            Minimum = 0;
            Maximum = int.MaxValue;
            CanMutate = true;
            IsDormant = false;
        }

        [Key]
        public int Id { get; set; }

        public int Minimum { get; set; }
        public int Maximum { get; set; }

        public int CurrentValue
        {
            get { return _currentValue; }
            set
            {
                LastValue = _currentValue;
                // Set value never exceeding gene's boundary value settings
                _currentValue = Math.Min(Math.Max(value, Minimum), Maximum);
                ValueDescription = GeneHelper.GetGeneDescriptionByTypeAndValue(GeneType, _currentValue);
            }
        }

        public string ValueDescription { get; set; }

        public int LastValue { get; set; }
        public GeneEnum GeneType { get; set; }
        /// <summary>
        /// Set CanMutate to false to hold constant the value of a gene in the population
        /// </summary>
        public bool CanMutate { get; set; }

        public int Increment(int delta = 1)
        {
            if (int.MaxValue - CurrentValue >= delta)
            {
                CurrentValue += delta;
            }
            return CurrentValue;
        }

        public int Decrement(int delta = 1)
        {
            if (CurrentValue - delta >= 0)
            {
                CurrentValue -= delta;
            }
            return CurrentValue;
        }

        /// <summary>
        /// To be effective a gene must be active and not dormant
        /// </summary>
        public bool IsDormant { get; set; }

        public bool IsActive => !IsDormant;

        public Gene Clone()
        {
            return GenericsHelper.DeepClone(this);
        }

        /// <summary>
        /// If gene type is GeneEnum.UserDefined then user must provide a description for the gene
        /// </summary>
        public string GeneDescription { get; set; }

        public void Mutate()
        {
            if (CanMutate)
            {
                if (RandomHelper.StandardGeneratorInstance.NextDouble() < MutationProbability)
                {
                    if (RandomHelper.StandardGeneratorInstance.NextDouble() < DrasticMutationProbability)
                    {
                        // drastic mutation: can be any value in range
                        CurrentValue = RandomHelper.StandardGeneratorInstance.Next(Minimum, Maximum);
                    }
                    else
                    {
                        // not a drastic mutation. Just increment/decrement
                        var delta = RandomHelper.StandardGeneratorInstance.NextDouble() < 0.5 ? -1 : 1;
                        Modify(delta);
                    }
                }
            }
        }


        public int Modify(int delta)
        {
            return Increment(delta);
        }

        public Organism Organism { get; set; }

        public DateTime Modified { get; set; }

        /// <summary>
        /// Creates & returns copy of a parent gene for the birth process
        /// </summary>
        public Gene CreateCopy()
        {
            return new Gene
            {
                Minimum = Minimum,
                Maximum = Maximum,
                CurrentValue = CurrentValue,
                ValueDescription = ValueDescription,
                GeneType = GeneType,
                GeneDescription = GeneDescription,
                CanMutate = CanMutate,
                IsDormant = IsDormant,
                LastValue = CurrentValue,
            };
        }
    }
}
