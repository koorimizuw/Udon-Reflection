using System;
using UdonSharp;
using VRC.Udon.Common.Interfaces;

namespace Yamadev.UdonReflection
{
    public class UdonMethodInfo: UdonSharpBehaviour
    {
        public static UdonMethodInfo New(string name, string internalName, Type[] argTypes, string[] argNames, Type returnType, string returnName)
        {
            object[] result = new object[6];
            result[0] = name;
            result[1] = internalName;
            result[2] = argTypes;
            result[3] = argNames;
            result[4] = returnType;
            result[5] = returnName;
            return result.ForceCast<UdonMethodInfo>();
        }
    }

    public static class UdonMethodInfoExtensions
    {
        private static object[] Parse(this UdonMethodInfo methodInfo)
        {
            return (object[])(object)methodInfo;
        }

        public static Type[] GetArgTypes(this UdonMethodInfo methodInfo)
        {
            return (Type[])methodInfo.Parse()[2];
        }

        public static Type GetReturnType(this UdonMethodInfo methodInfo)
        {
            return (Type)methodInfo.Parse()[4];
        }

        public static object Invoke(this UdonMethodInfo methodInfo, UdonReflectionBehaviour udon, object[] args)
        {
            string internalName = (string)methodInfo.Parse()[1];
            string[] argNames = (string[])methodInfo.Parse()[3];
            string returnName = (string)methodInfo.Parse()[5];
            Type[] argTypes = methodInfo.GetArgTypes();
            if (argNames.Length > 0)
            {
                if (args == null || args.Length != argNames.Length) return null;
                for (int i = 0; i < argTypes.Length; i++)
                {
                    if (argTypes[i] != args[i].GetType()) return null;
                }
                for (int i = 0; i < argNames.Length; i++)
                {
                    udon.SetProgramVariable(argNames[i], args[i]);
                }
            }
            udon.SendCustomEvent(internalName);
            if (returnName != null && ((IUdonEventReceiver)udon).GetProgramVariableType(returnName) != null)
                return udon.GetProgramVariable(returnName);
            return null;
        }
    }
}