using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace QuestingBots.Models
{
    public class AccessToolsLazyLoadField<Tin, Tout>
    {
        private string _fieldName;
        private Tout _defaultValue;
        private FieldInfo? _fieldInfo = null;

        public AccessToolsLazyLoadField(string fieldName, Tout defaultValue)
        {
            _fieldName = fieldName;
            _defaultValue = defaultValue;
        }

        private FieldInfo GetFieldInfo()
        {
            if (_fieldInfo == null)
            {
                _fieldInfo = AccessTools.Field(typeof(Tin), _fieldName) ?? throw new MissingFieldException(typeof(Tin).FullName, _fieldName);
            }

            return _fieldInfo;
        }

        public Tout GetValue(Tin obj)
        {
            if (obj == null)
            {
                return _defaultValue;
            }

            Tout output = (Tout)GetFieldInfo().GetValue(obj);
            if (output == null)
            {
                return _defaultValue;
            }

            return output;
        }
    }
}
