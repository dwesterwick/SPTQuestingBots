using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class MinMaxConfig
    {
        [DataMember(Name = "min", EmitDefaultValue = false, IsRequired = true)]
        public double Min { get; set; } = 0;

        [DataMember(Name = "max", EmitDefaultValue = false, IsRequired = true)]
        public double Max { get; set; } = 100;

        public MinMaxConfig()
        {

        }

        public MinMaxConfig(double min, double max): this()
        {
            Min = min;
            Max = max;
        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

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
