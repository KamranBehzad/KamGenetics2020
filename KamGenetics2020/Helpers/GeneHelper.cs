using KamGenetics2020.Model;
using KamGeneticsLib.Model;
using KBLib.Helpers;
using System;

namespace KamGenetics2020.Helpers
{
   public static class GeneHelper
   {
      // Constants
      private const int MinLibido = 1;
      private const int MaxLibido = 5;


      /// <summary>
      /// Economy Gene:
      /// 1. Worker - will cultivate resources, never steals.
      /// 2. Survivor - will cultivate resources. If none available might steal.
      /// 3. Thief - Steals first. Steals partial resources not all. Cultivates if there is nothing to steal.
      /// 4. Murderer - steals from others and kills them, taking all resources.
      /// </summary>
      public static Gene CreateEconomyGene(EconomyGene geneValue = EconomyGene.Worker)
      {
         int value = (int)geneValue;
         // ReSharper disable once UseObjectOrCollectionInitializer
         var gene = new Gene
         {
            GeneDescription = GeneEnum.Economy.ToString(),
            LastValue = value,
            Minimum = 1,
            Maximum = Enum.GetNames(typeof(EconomyGene)).Length,
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
            GeneDescription = GeneEnum.Cooperation.ToString(),
            LastValue = value,
            Minimum = 1,
            Maximum = Enum.GetNames(typeof(CooperationGene)).Length,
            CanMutate = true,
            IsDormant = false,
            GeneType = GeneEnum.Cooperation
         };
         gene.CurrentValue = value;
         return gene;
      }

      public static Gene CreateLibidoGene(int value = (MaxLibido - MinLibido) / 2)
      {
         // ReSharper disable once UseObjectOrCollectionInitializer
         var gene = new Gene
         {
            GeneDescription = GeneEnum.Libido.ToString(),
            LastValue = value,
            Minimum = MinLibido,
            Maximum = MaxLibido,
            CanMutate = true,
            IsDormant = false,
            GeneType = GeneEnum.Libido
         };
         gene.CurrentValue = value;
         return gene;
      }

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
            case GeneEnum.Libido:
               return $"Libido{value}";
            case GeneEnum.Military:
               return ((MilitaryGene)value).ToString();
         }
      }

      public static Gene CreateEconomyGeneVariations()
      {
         // Assign probabilities
         int workerPercentage = 25;
         int survivorPercentage = 25;
         int thiefPercentage = 25;
         int sumNonMurderers = workerPercentage + survivorPercentage + thiefPercentage;
         //int murdererPercentage = 100 - sumNonMurderers;

         // init gene randomly
         var rand = RandomHelper.StandardGeneratorInstance.Next(0, 100);
         EconomyGene geneValue = EconomyGene.Fungal;
         if (rand < workerPercentage)
            geneValue = EconomyGene.Worker;
         else if (rand < workerPercentage + survivorPercentage)
            geneValue = EconomyGene.Survivor;
         else if (rand < sumNonMurderers)
            geneValue = EconomyGene.Thief;
         return CreateEconomyGene(geneValue);
      }

      /// <summary>
      /// Military Gene:
      /// 1. NonMilitant
      /// 2. Passive Defender
      /// 3. Proactive Defender
      /// 4. Offender
      /// </summary>
      public static Gene CreateMilitaryGeneVariations()
      {
         // Assign probabilities
         int NonPercentage = 25;
         int passivePercentage = 25;
         int activePercentage = 25;
         int sumPrev = NonPercentage + passivePercentage + activePercentage;

         // init gene randomly
         var rand = RandomHelper.StandardGeneratorInstance.Next(0, 100);
         MilitaryGene geneValue = MilitaryGene.Offender;
         if (rand < NonPercentage)
            geneValue = MilitaryGene.NonMilitant;
         else if (rand < NonPercentage + passivePercentage)
            geneValue = MilitaryGene.Passive;
         else if (rand < sumPrev)
            geneValue = MilitaryGene.Proactive;
         return CreateMilitaryGene(geneValue);
      }

      /// <summary>
      /// Military Gene:
      /// 1. Defenseless - non military
      /// 2. Passive Defender
      /// 3. Proactive Defender
      /// 4. Offender
      /// </summary>
      public static Gene CreateMilitaryGene(MilitaryGene geneValue = MilitaryGene.NonMilitant)
      {
         int value = (int)geneValue;
         // ReSharper disable once UseObjectOrCollectionInitializer
         var gene = new Gene
         {
            GeneDescription = GeneEnum.Military.ToString(),
            LastValue = value,
            Minimum = 1,
            Maximum = Enum.GetNames(typeof(MilitaryGene)).Length,
            CanMutate = true,
            IsDormant = false,
            GeneType = GeneEnum.Military
         };
         gene.CurrentValue = value;
         return gene;
      }

   }
}
