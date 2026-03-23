using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;

namespace QuestingBots.Helpers
{
    public static class ConfigHelpers
    {
        public static bool IsDataContractType<T>(T obj)
        {
            return Attribute.IsDefined(typeof(T), typeof(DataContractAttribute));
        }

        private static DataContractJsonSerializerSettings _serializerSettings = null!;
        private static DataContractJsonSerializerSettings SerializerSettings
        {
            get
            {
                if (_serializerSettings == null)
                {
                    _serializerSettings = new DataContractJsonSerializerSettings();
                    _serializerSettings.UseSimpleDictionaryFormat = true;
                }

                return _serializerSettings;
            }
        }

        public static string Serialize<T>(T obj)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), SerializerSettings);
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
                DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(T), SerializerSettings);
                return (T?)deserializer.ReadObject(stream);
            }
        }

        public static T? DeserializeAndInitializeMissingFields<T>(string json)
        {
            T? obj = Deserialize<T>(json);
            if (obj != null)
            {
                JsonDocument jsonDocument = JsonDocument.Parse(json);
                Dictionary<string, object?> jsonMap = BuildMapOfPresentJsonFields(jsonDocument.RootElement);

                SetDefaultValuesForMissingFields(typeof(T), obj, jsonMap);
            }

            return obj;
        }

        private static Dictionary<string, object?> BuildMapOfPresentJsonFields(JsonElement element)
        {
            Dictionary<string, object?> map = new Dictionary<string, object?>(StringComparer.Ordinal);

            if (element.ValueKind != JsonValueKind.Object)
            {
                return map;
            }

            foreach (var prop in element.EnumerateObject())
            {
                object? value = prop.Value;
                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    value = BuildMapOfPresentJsonFields(prop.Value);
                }

                map.Add(prop.Name, value);
            }

            return map;
        }

        private static void SetDefaultValuesForMissingFields(Type objType, object obj, Dictionary<string, object?> mapOfPresentJsonFields)
        {
            foreach (PropertyInfo property in objType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                DataMemberAttribute? dataMemberAttribute = property.GetCustomAttribute<DataMemberAttribute>();
                if (dataMemberAttribute == null)
                {
                    continue;
                }

                if (!property.CanWrite)
                {
                    continue;
                }

                string jsonName = dataMemberAttribute.Name ?? property.Name;
                if (!mapOfPresentJsonFields.TryGetValue(jsonName, out object? childJson))
                {
                    object? defaultValue = GetDefaultValue(objType, property);
                    property.SetValue(obj, defaultValue);

                    continue;
                }

                object? childObj = property.GetValue(obj);
                if (childObj == null)
                {
                    continue;
                }

                Type childType = property.PropertyType;
                if ((childType.GetCustomAttribute<DataContractAttribute>() != null) && (childJson is Dictionary<string, object?> childMap))
                {
                    SetDefaultValuesForMissingFields(childType, childObj, childMap);
                }
            }
        }

        private static object? GetDefaultValue(Type type, PropertyInfo property)
        {
            object? template = Activator.CreateInstance(type);
            return property.GetValue(template);
        }
    }
}
