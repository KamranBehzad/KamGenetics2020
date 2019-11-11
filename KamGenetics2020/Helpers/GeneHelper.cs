using KamGenetics2020.Model;

namespace KamGenetics2020.Helpers
{
    public static class GeneHelper
    {
        public enum GeneEnum
        {
            UserDefined,
            Cooperation,
            Economy,
            Libido
        }

        public enum CooperationGene
        {
            Solo = 1,           // organism prefers to work solo
            Cooperative = 2,    // organism prefers to work in teams
        }

        public enum EconomyGene
        {
            Worker = 1,     // organism cultivates resources, never steals.
            Survivor = 2,   // will cultivate resources but if none available will steal.
            Thief = 3,      // steals from others. Will cultivate if nothing found to steal.
            Murderer = 4,   // steals from others and kills them.
        }

        /// <summary>
        /// Economy Gene:
        /// 1. Worker - will cultivate resources, never steals.
        /// 2. Survivor - will cultivate resources. If none available might steal.
        /// 3. Thief - only steals from others.
        /// 4. Murderer - steals from others and kills them.
        /// </summary>
        public static Gene CreateEconomyGene(int value = 1)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var gene = new Gene
            {
                TypeDescription = "Economy",
                LastValue = value,
                Minimum = 1,
                Maximum = 4,
                CanMutate = true,
                IsDormant = false,
                GeneType = GeneEnum.Economy
            };
            gene.CurrentValue = value;
            return gene;
        }

        /// <summary>
        /// Cooperation Gene
        /// 1. Solo - works alone
        /// 2. Cooperative - will form group with others and share resources
        /// </summary>
        public static Gene CreateCooperationGene(int value = 1)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var gene = new Gene
            {
                TypeDescription = "Cooperation",
                LastValue = value,
                Minimum = 1,
                Maximum = 2,
                CanMutate = true,
                IsDormant = false,
                GeneType = GeneEnum.Cooperation
            };
            gene.CurrentValue = value;
            return gene;
        }

        public static Gene CreateLibidoGene(int value = 5)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var gene = new Gene
            {
                TypeDescription = "Libido",
                LastValue = value,
                Minimum = 0,
                Maximum = 10,
                CanMutate = true,
                IsDormant = false,
                GeneType = GeneEnum.Libido
            };
            gene.CurrentValue = value;
            return gene;
        }

        //public static string GetCooperativeGeneStringValue(CooperationGene value)
        //{
        //    return value.ToString();
        //}

        //public static string GetEconomyGeneStringValue(EconomyGene value)
        //{
        //    return value.ToString();
        //}

        //public static string GetGeneDescriptionByValue<T>(object value)
        //{
        //    return Enum.GetName(typeof(T), value);
        //}

        public static string GetGeneDescriptionByTypeAndValue(GeneEnum geneType, int value)
        {
            switch (geneType)
            {
                default:
                    return string.Empty;
                case GeneEnum.Cooperation:
                    return ((CooperationGene)value).ToString();
                case GeneEnum.Economy:
                    return ((EconomyGene)value).ToString();
            }
        }

    }
}
