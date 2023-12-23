using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class MinMaxConfig
    {
        [JsonProperty("min")]
        public double Min { get; set; } = 0;

        [JsonProperty("max")]
        public double Max { get; set; } = 100;

        public MinMaxConfig()
        {

        }

        public MinMaxConfig(double min, double max): this()
        {
            Min = min;
            Max = max;
        }

        public static MinMaxConfig operator +(MinMaxConfig a, MinMaxConfig b)
        {
            return new MinMaxConfig(Math.Round(a.Min + b.Min), Math.Round(a.Max + b.Max));
        }

        public static MinMaxConfig operator -(MinMaxConfig a, MinMaxConfig b)
        {
            return new MinMaxConfig(Math.Round(a.Min - b.Min), Math.Round(a.Max - b.Max));
        }

        public static MinMaxConfig operator *(MinMaxConfig a, MinMaxConfig b)
        {
            return new MinMaxConfig(Math.Round(a.Min * b.Min), Math.Round(a.Max * b.Max));
        }

        public static MinMaxConfig operator /(MinMaxConfig a, MinMaxConfig b)
        {
            return new MinMaxConfig(Math.Round(a.Min / b.Min), Math.Round(a.Max / b.Max));
        }

        public static MinMaxConfig operator +(MinMaxConfig a, double b)
        {
            return new MinMaxConfig(Math.Round(a.Min + b), Math.Round(a.Max + b));
        }

        public static MinMaxConfig operator -(MinMaxConfig a, double b)
        {
            return new MinMaxConfig(Math.Round(a.Min - b), Math.Round(a.Max - b));
        }

        public static MinMaxConfig operator *(MinMaxConfig a, double b)
        {
            return new MinMaxConfig(Math.Round(a.Min * b), Math.Round(a.Max * b));
        }

        public static MinMaxConfig operator /(MinMaxConfig a, double b)
        {
            return new MinMaxConfig(Math.Round(a.Min / b), Math.Round(a.Max / b));
        }
    }
}
