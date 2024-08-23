using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Models
{
    public abstract class JSONObject<T>
    {
        public static BindingFlags DefaultPropertySearchBindingFlags { get; } = BindingFlags.Instance | BindingFlags.Public;

        public void UpdateJSONProperties(Dictionary<string, object> jsonProperties)
        {
            UpdateJSONProperties(jsonProperties, DefaultPropertySearchBindingFlags);
        }

        public void UpdateJSONProperties(Dictionary<string, object> jsonProperties, BindingFlags bindingFlags)
        {
            foreach (string jsonPropertyName in jsonProperties.Keys)
            {
                UpdateJSONProperty(jsonPropertyName, jsonProperties[jsonPropertyName], bindingFlags);
            }
        }

        public void UpdateJSONProperty(string jsonPropertyName, object val)
        {
            UpdateJSONProperty(jsonPropertyName, val, DefaultPropertySearchBindingFlags);
        }

        public void UpdateJSONProperty(string jsonPropertyName, object val, BindingFlags bindingFlags)
        {
            Type type = typeof(T);

            PropertyInfo[] matchingProperties = type
                .GetProperties(bindingFlags)
                .Where(p => p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName == jsonPropertyName)
                .ToArray();

            if (matchingProperties.Length == 0)
            {
                string allPropertiesText = string.Join(", ", type
                .GetProperties(bindingFlags)
                .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName)
                .Where(n => n?.Length > 0)
                .ToArray());

                throw new InvalidOperationException("Cannot find JSON property " + jsonPropertyName + " for " + type.FullName + ". Available properties: " + allPropertiesText);
            }
            if (matchingProperties.Length > 1)
            {
                throw new InvalidOperationException("Found more than one match for JSON property " + jsonPropertyName + " for " + type.FullName);
            }

            if (val.GetType() == typeof(JArray))
            {
                try
                {
                    JArray array = (JArray)val;
                    val = array.ToObject(matchingProperties[0].PropertyType);
                }
                catch (Exception)
                {
                    LoggingController.LogError("Cannot convert JArray to " + matchingProperties[0].PropertyType.FullName + " for property " + jsonPropertyName + " of type " + type.FullName);
                    throw;
                }
            }

            if (val.GetType() == typeof(JObject))
            {
                try
                {
                    JObject jObject = (JObject)val;
                    val = jObject.ToObject(matchingProperties[0].PropertyType);
                }
                catch (Exception)
                {
                    LoggingController.LogError("Cannot convert JObject to " + matchingProperties[0].PropertyType.FullName + " for property " + jsonPropertyName + " of type " + type.FullName);
                    throw;
                }
            }

            matchingProperties[0].SetValue(this, val);

            //LoggingController.LogInfo("Set value of " + jsonPropertyName + " for " + type.FullName + " object to " + val);
        }
    }
}
