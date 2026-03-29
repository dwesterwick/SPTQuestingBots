using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models
{
    public class SemanticVersionRange
    {
        public Version MinVersion { get; }
        public Version MaxVersion { get; }

        public SemanticVersionRange(Version min, Version max)
        {
            MinVersion = min;
            MaxVersion = max;
        }

        public static SemanticVersionRange Parse(string text) => SemanticVersionHelpers.ParseRange(text);

        public override string ToString() => $"[{MinVersion} .. {MaxVersion})";
    }
}
