using System;
using UdonSharp;

namespace Yamadev.UdonReflection
{
    public class UdonFieldInfo : UdonSharpBehaviour
    {
        public static UdonFieldInfo New(string name, Type type)
        {
            object[] result = new object[2];
            result[0] = name;
            result[1] = type;
            return result.ForceCast<UdonFieldInfo>();
        }
    }

    public static class UdonFieldInfoExtentions
    {
        private static object[] Parse(this UdonFieldInfo field)
        {
            return (object[])(object)field;
        }

        public static string GetName(this UdonFieldInfo field)
        {
            return (string)field.Parse()[0];
        }

        public static Type GetSystemType(this UdonFieldInfo field)
        {
            return (Type)field.Parse()[1];
        }

        public static object GetValue(this UdonFieldInfo field, UdonReflectionBehaviour udon)
        {
            return udon.GetProgramVariable(field.GetName());
        }

        public static void SetValue(this UdonFieldInfo field, UdonReflectionBehaviour udon, object value)
        {
            udon.SetProgramVariable(field.GetName(), value);
        }

    }
}