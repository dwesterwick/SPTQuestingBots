using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Helpers
{
    public static class TarkovTypeHelpers
    {
        public static Type FindTargetTypeByMethod(string methodName)
        {
            Predicate<MethodInfo> methodInfoPredicate = (m) => { return m.Name.Contains(methodName); };

            try
            {
                return findTargetType_Internal(methodInfoPredicate);
            }
            catch (TypeLoadException e)
            {
                throw new TypeLoadException($"Cannot find any type containing method {methodName}", e);
            }
        }

        public static Type FindTargetTypeByMethod(string methodName, Type[] parameterTypes)
        {
            Predicate<MethodInfo> methodInfoPredicate = (m) => { return m.Name.Contains(methodName) && m.HasAllParameterTypesInOrder(parameterTypes); };

            try
            {
                return findTargetType_Internal(methodInfoPredicate);
            }
            catch (TypeLoadException e)
            {
                throw new TypeLoadException($"Cannot find any type containing method {methodName} and types {string.Join(", ", parameterTypes.Select(t => t.Name))}", e);
            }
        }

        public static Type FindTargetTypeByField(string fieldName)
        {
            Predicate<FieldInfo> fieldInfoPredicate = (m) => { return m.Name.Contains(fieldName); };

            try
            {
                return findTargetType_Internal(fieldInfoPredicate);
            }
            catch (TypeLoadException e)
            {
                throw new TypeLoadException($"Cannot find any type containing field {fieldName}", e);
            }
        }

        public static bool IsUnmapped(this MethodInfo methodInfo)
        {
            int index = methodInfo.Name.IndexOf("method");

            if ((index < 0) || (index > 1))
            {
                return false;
            }

            return true;
        }

        public static bool HasAllParameterTypes(this MethodInfo methodInfo, Type[] argumentTypes)
        {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

            foreach (Type argumentType in argumentTypes)
            {
                if (!parameterInfos.Any(p => argumentTypes.Contains(p.ParameterType)))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HasAllParameterTypesInOrder(this MethodInfo methodInfo, Type[] argumentTypes)
        {
            ParameterInfo[] parameterInfo = methodInfo.GetParameters();

            if (parameterInfo.Length < argumentTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < argumentTypes.Length; i++)
            {
                if (parameterInfo[i].ParameterType != argumentTypes[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static Type findTargetType_Internal<T>(Predicate<T> containsObjectPredicate)
        {
            List<Type> targetTypeOptions = SPT.Reflection.Utils.PatchConstants.EftTypes
                .Where(t => t.getTypeObjects<T>().Any(m => containsObjectPredicate(m)))
                .ToList();

            if (targetTypeOptions.Count != 1)
            {
                throw new TypeLoadException("Cannot find a matching type");
            }

            return targetTypeOptions[0];
        }

        private static TObjType[] getTypeObjects<TObjType>(this Type type)
        {
            Type objType = typeof(TObjType);

            if (objType == typeof(MethodInfo))
            {
                TObjType[]? methods = type.GetMethods() as TObjType[];
                return methods!;
            }

            if (objType == typeof(PropertyInfo))
            {
                TObjType[]? properties = type.GetProperties() as TObjType[];
                return properties!;
            }

            if (objType == typeof(FieldInfo))
            {
                TObjType[]? fields = type.GetFields() as TObjType[];
                return fields!;
            }

            throw new InvalidOperationException($"Type {objType.Name} is not supported");
        }
    }
}
