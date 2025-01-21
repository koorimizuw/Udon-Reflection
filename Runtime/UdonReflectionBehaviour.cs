using System;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace Yamadev.UdonReflection
{
    public abstract class UdonReflectionBehaviour : UdonSharpBehaviour { }

    public static class UdonReflectionExtensions
    {
        public static UdonFieldInfo[] GetFields(this UdonReflectionBehaviour udon)
        {
            string[] fieldNames = (string[])udon.GetProgramVariable("__refl_fieldnames");
            Type[] fieldTypes = (Type[])udon.GetProgramVariable("__refl_fieldtypes");

            UdonFieldInfo[] results = new UdonFieldInfo[fieldNames.Length];
            for (int i = 0; i < fieldNames.Length; i++)
            {
                results[i] = UdonFieldInfo.New(fieldNames[i], fieldTypes[i]);
            }

            return results;
        }

        public static UdonFieldInfo GetField(this UdonReflectionBehaviour udon, string name)
        {
            string[] fieldNames = (string[])udon.GetProgramVariable("__refl_fieldnames");
            int index = Array.IndexOf(fieldNames, name);
            if (index == -1) return null;

            Type[] fieldTypes = (Type[])udon.GetProgramVariable("__refl_fieldtypes");
            return UdonFieldInfo.New(fieldNames[index], fieldTypes[index]);
        }

        public static UdonMethodInfo[] GetMethods(this UdonReflectionBehaviour udon)
        {
            string[] methodNames = (string[])udon.GetProgramVariable("__refl_methodnames");

            UdonMethodInfo[] results = new UdonMethodInfo[methodNames.Length];
            for (int i = 0; i < methodNames.Length; i++)
            {
                foreach (var method in udon.GetAllMethodByName(methodNames[i]))
                    results[i] = method;
            }

            return results;
        }

        public static UdonMethodInfo GetMethod(this UdonReflectionBehaviour udon, string name)
        {
            var methods = udon.GetAllMethodByName(name);
            if (methods.Length == 1) return methods[0];
            return null;
        }

        private static UdonMethodInfo[] GetAllMethodByName(this UdonReflectionBehaviour udon, string name)
        {
            object[] results = new object[0];
            if (((IUdonEventReceiver)udon).GetProgramVariableType($"__refl_argtypes_{name}") != null)
            {
                results = results.Add(udon.BindMethod(name, name));
            }

            int index = 0;
            while (true)
            {
                string targetName = $"__{index}_{name}";
                if (((IUdonEventReceiver)udon).GetProgramVariableType($"__refl_argtypes_{targetName}") == null) break;

                results = results.Add(udon.BindMethod(name, targetName));
                index++;
            }
            return (UdonMethodInfo[])results;
        }

        private static UdonMethodInfo BindMethod(this UdonReflectionBehaviour udon, string name, string internalName)
        {
            Type[] argTypes = new Type[0];
            string[] argNames = new string[0];
            if (internalName.StartsWith("_"))
            {
                argTypes = (Type[])udon.GetProgramVariable($"__refl_argtypes_{internalName}");
                argNames = (string[])udon.GetProgramVariable($"__refl_argnames_{internalName}");
            }
            Type returnType = (Type)udon.GetProgramVariable($"__refl_returntype_{internalName}");
            string returnName = (string)udon.GetProgramVariable($"__refl_returnname_{internalName}");
            return UdonMethodInfo.New(name, internalName, argTypes, argNames, returnType, returnName);
        }

        public static UdonPropertyInfo GetProperty(this UdonReflectionBehaviour udon, string name)
        {
            var internalGet = GetMethod(udon, $"get_{name}");
            var internalSet = GetMethod(udon, $"set_{name}");
            if (!Utilities.IsValid(internalGet) && !Utilities.IsValid(internalSet)) return null;
            return UdonPropertyInfo.New(name, internalGet.GetReturnType(), internalGet, internalSet);
        }
    }
}