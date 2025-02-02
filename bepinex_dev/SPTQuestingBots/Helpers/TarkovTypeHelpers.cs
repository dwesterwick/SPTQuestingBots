using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Helpers
{
    public static class TarkovTypeHelpers
    {
        public static Type FindTargetType(string methodName)
        {
            Predicate<MethodInfo> methodInfoPredicate = (m) => { return m.Name.Contains(methodName); };

            try
            {
                return findTargetType_Internal(methodInfoPredicate);
            }
            catch (TypeLoadException)
            {
                throw new TypeLoadException($"Cannot find any type containing method {methodName}");
            }
        }

        public static Type FindTargetType(string methodName, Type[] parameterTypes)
        {
            Predicate<MethodInfo> methodInfoPredicate = (m) => { return m.Name.Contains(methodName) && m.HasAllParameterTypesInOrder(parameterTypes); };

            try
            {
                return findTargetType_Internal(methodInfoPredicate);
            }
            catch (TypeLoadException)
            {
                throw new TypeLoadException($"Cannot find any type containing method {methodName} and types {string.Join(", ", parameterTypes.Select(t => t.Name))}");
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

        private static Type findTargetType_Internal(Predicate<MethodInfo> methodInfoPredicate)
        {
            List<Type> targetTypeOptions = SPT.Reflection.Utils.PatchConstants.EftTypes
                .Where(t => t.GetMethods().Any(m => methodInfoPredicate(m)))
                .ToList();

            if (targetTypeOptions.Count != 1)
            {
                throw new TypeLoadException("Cannot find a matching type");
            }

            return targetTypeOptions[0];
        }
    }
}
