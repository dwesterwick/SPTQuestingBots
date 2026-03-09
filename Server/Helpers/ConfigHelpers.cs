using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QuestingBots.Helpers
{
    public class ConfigHelpers
    {
        public static bool IsDataContractType<T>(T obj)
        {
            return Attribute.IsDefined(typeof(T), typeof(DataContractAttribute));
        }

        public static string Serialize<T>(T obj)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(memoryStream, obj);
                memoryStream.Position = 0;
                return reader.ReadToEnd();
            }
        }

        public static T? Deserialize<T>(string json)
        {
            using (Stream stream = new MemoryStream())
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(T));
                return (T?)deserializer.ReadObject(stream);
            }
        }
    }
}
