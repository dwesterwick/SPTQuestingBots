using QuestingBots.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuestingBots.Helpers
{
    public static class SharedConfigHelpers
    {
        public static bool IsModEnabled(this ModConfig? modConfig) => modConfig?.Enabled == true;
        public static bool IsDebugEnabled(this ModConfig? modConfig) => modConfig?.Debug?.Enabled == true;

        public static void DisableMod(this ModConfig modConfig) => modConfig.Enabled = false;

        public static double InterpolateForFirstCol(this double[][] array, double value)
        {
            ValidateChanceArray(array);

            if (array.Length == 1)
            {
                return array.Last()[1];
            }

            if (value <= array[0][0])
            {
                return array[0][1];
            }

            for (int i = 1; i < array.Length; i++)
            {
                if (array[i][0] >= value)
                {
                    if (array[i][0] - array[i - 1][0] == 0)
                    {
                        return array[i][1];
                    }

                    return array[i - 1][1] + (value - array[i - 1][0]) * (array[i][1] - array[i - 1][1]) / (array[i][0] - array[i - 1][0]);
                }
            }

            return array.Last()[1];
        }

        public static double GetValueFromTotalChanceFraction(this double[][] array, double fraction)
        {
            ValidateChanceArray(array);

            if (array.Length == 1)
            {
                return array.Last()[1];
            }

            double chancesSum = array.Sum(x => x[1]);
            double targetCumulativeChances = chancesSum * fraction;

            int i = 0;
            double cumulativeChances = 0;
            while (i < array.Length)
            {
                cumulativeChances += array[i][1];

                if (cumulativeChances > targetCumulativeChances)
                {
                    return array[i][0];
                }

                i++;
            }

            return array.Last()[0];
        }

        public static void ValidateChanceArray(this double[][] array, bool leftColumnMustBeInts = false)
        {
            if (array.Length == 0)
            {
                throw new ArgumentOutOfRangeException("The array must have at least one row.");
            }

            if (array.Any(x => x.Length != 2))
            {
                throw new ArgumentOutOfRangeException("All rows in the array must have two columns.");
            }

            if (leftColumnMustBeInts && array.Any(pair => !pair[0].IsAnInteger()))
            {
                throw new ArgumentOutOfRangeException("The left column has a floating-point number, but only integers are expected.");
            }
        }
    }
}
