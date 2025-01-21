using System;
using UdonSharp;
using VRC.SDKBase;

namespace Yamadev.UdonReflection
{
    public class UdonPropertyInfo : UdonSharpBehaviour
    {
        public static UdonPropertyInfo New(string name, Type type, UdonMethodInfo internalGet, UdonMethodInfo internalSet)
        {
            object[] result = new object[4];
            result[0] = name;
            result[1] = type;
            result[2] = internalGet;
            result[3] = internalSet;
            return result.ForceCast<UdonPropertyInfo>();
        }
    }

    public static class UdonPropertyInfoExtensions
    {
        private static object[] Parse(this UdonPropertyInfo property)
        {
            return (object[])(object)property;
        }

        public static string GetName(this UdonPropertyInfo property)
        {
            return (string)property.Parse()[0];
        }

        public static Type GetSystemType(this UdonPropertyInfo property)
        {
            return (Type)property.Parse()[1];
        }

        public static object GetValue(this UdonPropertyInfo property, UdonReflectionBehaviour udon)
        {
            var internalGet = (UdonMethodInfo)property.Parse()[2];
            if (!Utilities.IsValid(internalGet)) return null;
            return internalGet.Invoke(udon, new object[0]);
        }

        public static void SetValue(this UdonPropertyInfo property, UdonReflectionBehaviour udon, object value)
        {
            var internalSet = (UdonMethodInfo)property.Parse()[3];
            if (!Utilities.IsValid(internalSet)) return;
            internalSet.Invoke(udon, new object[] { value });
        }
    }
}