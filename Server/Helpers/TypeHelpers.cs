using System.Reflection;

namespace QuestingBots.Helpers
{
    public static class TypeHelpers
    {
        public static bool CanArgumentsCanBeUsedToCreateType<T>(params object?[] args) => typeof(T).CanArgumentsCanBeUsedToCreate(args);

        public static bool CanArgumentsCanBeUsedToCreate(this Type type, params object?[] args)
        {
            Type?[] argTypes = args.Select(arg => arg?.GetType()).ToArray();
            ConstructorInfo[] constructorInfo = type.GetConstructors();

            if (constructorInfo.Any(info => info.CanArgumentTypesCanBeUsed(argTypes)))
            {
                return true;
            }

            return false;
        }

        public static bool CanArgumentTypesCanBeUsed(this ConstructorInfo constructorInfo, IEnumerable<Type?> argumentTypes)
        {
            IEnumerable<Type> constructorTypes = constructorInfo.GetParameters().Select(param => param.ParameterType);
            return constructorTypes.SequenceEqual(argumentTypes);
        }
    }
}
