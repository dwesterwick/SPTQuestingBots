using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Models
{
    public abstract class JSONObject<T>
    {
        public void UpdateJSONProperties(Dictionary<string, object> jsonProperties)
        {
            foreach (string jsonPropertyName in jsonProperties.Keys)
            {
                UpdateJSONProperty(jsonPropertyName, jsonProperties[jsonPropertyName]);
            }
        }

        public void UpdateJSONProperty(string jsonPropertyName, object val)
        {
            Type type = typeof(T);

            PropertyInfo[] matchingProperties = type
                .GetProperties()
                .Where(p => p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName == jsonPropertyName)
                .ToArray();

            if (matchingProperties.Length == 0)
            {
                throw new InvalidOperationException("Cannot find JSON property " + jsonPropertyName + " for " + type.FullName);
            }
            if (matchingProperties.Length > 1)
            {
                throw new InvalidOperationException("Found more than one match for JSON property " + jsonPropertyName + " for " + type.FullName);
            }

            matchingProperties[0].SetValue(this, val);

            //LoggingController.LogInfo("Set value of " + jsonPropertyName + " for " + type.FullName + " object to " + val);
        }
    }
}
