using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL3Tools.GameData {

    public static class PlayerXP {

        // PlayerExperienceFormula={Multiplier: 65.0, Power: 2.7999999523162841796875, Offset: 7.3299999237060546875}
        // PlayerGuardianRankExperienceFormula={BaseValue: 40.0, BaseMultiplier: 1.0, Multiplier: 60.0, Power: 2.799999952316284, Offset: 12.3299999237060546875}
        private const float expMultiplier = 65.0f;
        private const float expPower = 2.7999999523162841796875f;
        private const float expOffset = 7.3299999237060546875f;
        private const float mythBaseValue = 40.0f;
        private const float mythBaseMultiplier = 1.0f;
        private const float mythMultiplier = 60.0f;
        private const float mythPower = 2.2999999523162841796875f;
        private const float mythOffset = 12.3299999237060546875f;

        public static int _XPMaximumLevel { get; } = 40;
        public static int _XPMinimumLevel { get; } = 1;
        private static readonly int _XPReduction = 0;

        private static Dictionary<int, int> xpLevelTable = new Dictionary<int, int>();

        private static int ComputeEXPLevel(int level) {
            return (int)Math.Floor((Math.Pow(level, expPower) + expOffset) * expMultiplier);
        }

        static PlayerXP() {
            _XPReduction = ComputeEXPLevel(_XPMinimumLevel);

            // Add to the dictionary
            for (int lvl = 1; lvl <= _XPMaximumLevel; lvl++) {
                xpLevelTable.Add(lvl, GetPointsForXPLevel(lvl));
            }

        }

        /// <summary>
        /// Gets the points for the associated XP level
        /// </summary>
        /// <param name="lvl">EXP Level</param>
        /// <returns>The amount of EXP points for the associated amount of points</returns>
        public static int GetPointsForXPLevel(int lvl) {
            if (lvl <= _XPMinimumLevel) return 0;

            return ComputeEXPLevel(lvl) - _XPReduction;
        }

        /// <summary>
        /// Gets the respective level for a given amount of XP points
        /// </summary>
        /// <param name="points">Amount of XP Points</param>
        /// <returns>The level associated with the points</returns>
        public static int GetLevelForPoints(int points) {
            if (points < 0) return _XPMinimumLevel;
            if (points >= xpLevelTable.Last().Value) return _XPMaximumLevel;

            // Get the closest level to the point amounts (price is right rules)
            return xpLevelTable.First(lv => points < lv.Value).Key - 1;
        }

        public static long GetPointsForMythPoints(int points)
        {
            points-=1;
            points+=(int)mythBaseValue;
            
            return (long)Math.Floor((Math.Pow(points, expPower) + mythOffset) * mythMultiplier) -
                   (long)Math.Floor((Math.Pow(mythBaseValue, expPower) + mythOffset) * mythMultiplier) +
                   (long)Math.Floor((Math.Pow(points, expPower) + mythOffset) * mythMultiplier);
        }
    }

    public static class MayhemLevel {
        public static readonly int MinimumLevel = 0;
        public static readonly int MaximumLevel = 10;
    }
}
