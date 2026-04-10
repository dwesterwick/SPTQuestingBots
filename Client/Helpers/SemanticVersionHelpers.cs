using QuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuestingBots.Helpers
{
    public static class SemanticVersionHelpers
    {
        public static SemanticVersionRange ParseRange(string input)
        {
            Version minPossibleVersion = new Version(0, 0, 0, 0);
            Version maxPossibleVersion = new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);

            foreach ((string comparisonOp, string versionString) in input.TokenizeRange())
            {
                if (!Version.TryParse(versionString, out Version version))
                {
                    throw new FormatException($"Cannot parse version {versionString}");
                }

                switch (comparisonOp)
                {
                    case "=":
                        minPossibleVersion = Max(minPossibleVersion, version);
                        maxPossibleVersion = Min(maxPossibleVersion, version.NextBuild());
                        break;

                    case ">":
                        minPossibleVersion = Max(minPossibleVersion, version.NextBuild());
                        break;

                    case ">=":
                        minPossibleVersion = Max(minPossibleVersion, version);
                        break;

                    case "<":
                        maxPossibleVersion = Min(maxPossibleVersion, version);
                        break;

                    case "<=":
                        maxPossibleVersion = Min(maxPossibleVersion, version.NextBuild());
                        break;

                    case "~":
                        minPossibleVersion = Max(minPossibleVersion, version);
                        maxPossibleVersion = Min(maxPossibleVersion, version.NextMinor());
                        break;

                    case "^":
                        minPossibleVersion = Max(minPossibleVersion, version);
                        maxPossibleVersion = Min(maxPossibleVersion, version.NextMajor());
                        break;

                    default:
                        throw new NotSupportedException($"Unsupported comparison operator \"{comparisonOp}\" for version {versionString}");
                }
            }

            return new SemanticVersionRange(minPossibleVersion, maxPossibleVersion);
        }

        private static IEnumerable<(string comparisonOp, string version)> TokenizeRange(this string versionRange)
        {
            string[] tokens = versionRange.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in tokens)
            {
                if (token.StartsWith(">=")) yield return (">=", token[2..]);
                else if (token.StartsWith("<=")) yield return ("<=", token[2..]);
                else if (token.StartsWith(">")) yield return (">", token[1..]);
                else if (token.StartsWith("<")) yield return ("<", token[1..]);
                else if (token.StartsWith("~")) yield return ("~", token[1..]);
                else if (token.StartsWith("^")) yield return ("^", token[1..]);
                else yield return ("=", token);
            }
        }

        private static Version NextRevision(this Version v) => new Version(v.Major, v.Minor, v.Build, v.Revision + 1);
        private static Version NextBuild(this Version v) => new Version(v.Major, v.Minor, v.Build + 1, 0);
        private static Version NextMinor(this Version v) => new Version(v.Major, v.Minor + 1, 0, 0);
        private static Version NextMajor(this Version v) => new Version(v.Major + 1, 0, 0, 0);

        private static Version Min(Version a, Version b) => a.CompareTo(b) <= 0 ? a : b;
        private static Version Max(Version a, Version b) => a.CompareTo(b) >= 0 ? a : b;
    }
}
