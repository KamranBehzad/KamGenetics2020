using System;

namespace KamGenetics2020.Helpers
{
    public static class ResourceHelper
    {
        const double maxAmplitude = 1000;
        const double coefLong = 36; // I want my sine function to have period of 36 units
        const double coefShort = 13; // I want my sine function to have period of 7 units

        static readonly double degreeCoefLong = 360 / coefLong;
        static readonly double degreeCoefShort = 360 / coefShort;

        private static double GetResourceRegenerationUnitByTimeIndex(int timeIdx)
        {
            var sinShort = Math.Sin(degreeCoefShort * timeIdx * Math.PI / 180);
            var sinLong = Math.Sin(degreeCoefLong * timeIdx * Math.PI / 180);
            var result = (sinShort + sinLong + 2);
            return result;
        }

        /// <summary>
        /// Returns the amount of resources that will be regenerated for the current time index.
        /// The amount is calculated by mixing two sine functions.
        /// The amount over the years will have a max value as given and a mean of 0.5x Max.
        /// </summary>
        public static double GetResourceRegenerationByTimeIndexWithMax(int timeIdx, int maxAmount)
        {
            var amplitude = maxAmount / 4;
            return GetResourceRegenerationUnitByTimeIndex(timeIdx) * amplitude;
        }

        /// <summary>
        /// Returns the amount of resources that will be regenerated for the current time index.
        /// The amount is calculated by mixing two sine functions.
        /// The amount over the years will have a max value as given and a mean of 0.5x Max.
        /// </summary>
        public static double GetResourceRegenerationByTimeIndexWithMean(int timeIdx, int meanAmount)
        {
            var amplitude = meanAmount / 2;
            return GetResourceRegenerationUnitByTimeIndex(timeIdx) * amplitude;
        }

    }
}
