using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp.Compiler;
using UnityEditor;
using UnityEngine;

namespace Yamadev.UdonReflection.Editor;

[InitializeOnLoad, HarmonyPatch]
internal static class UdonSharpPatcher
{
    internal static Lazy<List<Type>> allTypes = new Lazy<List<Type>>(() => AppDomain.CurrentDomain.GetAssemblies().SelectMany(i => i.GetTypes()).ToList());

    static UdonSharpPatcher()
    {
        Harmony harmony = new Harmony("Yamadev.YamaPlayer.Editor.UdonSharpPatcher");

        var emitContextType = allTypes.Value.FirstOrDefault(t => t.FullName == "UdonSharp.Compiler.Emit.EmitContext");
        var emitContextOriginal = emitContextType.GetMethod("InitConstFields", BindingFlags.NonPublic | BindingFlags.Instance);
        var emitContextPatcher = typeof(UdonSharpPatcher).GetMethod(nameof(InitConstFieldsPatcher), BindingFlags.NonPublic | BindingFlags.Static);
        harmony.Patch(emitContextOriginal, null, new HarmonyMethod(emitContextPatcher));

        var exportOriginal = emitContextType.GetMethod("MethodNeedsExport");
        var exportPatcher = typeof(UdonSharpPatcher).GetMethod(nameof(MethodNeedsExportPatcher), BindingFlags.NonPublic | BindingFlags.Static);
        harmony.Patch(exportOriginal, null, new HarmonyMethod(exportPatcher));

        var methodSymbolType = allTypes.Value.FirstOrDefault(t => t.FullName == "UdonSharp.Compiler.Symbols.MethodSymbol");
        var methodSymbolOriginal = methodSymbolType.GetMethod("Emit");
        var methodSymbolPatcher = typeof(UdonSharpPatcher).GetMethod(nameof(MethodSymbolEmitPatcher), BindingFlags.NonPublic | BindingFlags.Static);
        harmony.Patch(methodSymbolOriginal, new HarmonyMethod(methodSymbolPatcher));

        var assebleProgramOriginal = typeof(UdonSharpCompilerV1).GetMethod("AssembleProgram", BindingFlags.NonPublic | BindingFlags.Static);
        var assebleProgramPatcher = typeof(UdonSharpPatcher).GetMethod(nameof(AssembleProgramPatcher), BindingFlags.NonPublic | BindingFlags.Static);
        harmony.Patch(assebleProgramOriginal, new HarmonyMethod(assebleProgramPatcher));

    }

    private static Lazy<List<string>> targetTypes = new Lazy<List<string>>(InitializeTargetTypes);

    private static List<string> InitializeTargetTypes()
    {
        var types = allTypes.Value.Where(i => i.IsSubclassOf(typeof(UdonReflectionBehaviour)) && !i.IsAbstract);
        return types.Select(t => t.FullName).ToList();
    }

    private static void InitConstFieldsPatcher(object __instance)
    {
        var emitType = __instance.GetType().GetProperty("EmitType").GetValue(__instance);
        if (!targetTypes.Value.Contains(emitType.ToString())) return;

        var RootTable = __instance.GetType().GetProperty("RootTable", BindingFlags.Public | BindingFlags.Instance).GetValue(__instance);
        var CreateReflectionValue = RootTable.GetType().GetMethod("CreateReflectionValue", BindingFlags.Public | BindingFlags.Instance);
        var GetTypeSymbol = __instance.GetType().GetMethod("GetTypeSymbol", new Type[] { typeof(Type) }); // BindingFlags.Public | BindingFlags.Instance

        var fields = (IList)__instance.GetType().GetProperty("DeclaredFields").GetValue(__instance);
        List<string> fieldNames = new List<string>();
        List<Type> fieldTypes = new List<Type>();
        foreach (var field in fields) fieldNames.Add(field.GetType().GetProperty("Name").GetValue(field) as string);
        foreach (var field in fields)
        {
            var typeSymbol = field.GetType().GetProperty("Type").GetValue(field);
            var udonType = typeSymbol.GetType().GetProperty("UdonType").GetValue(typeSymbol);
            var systemType = udonType.GetType().GetProperty("SystemType").GetValue(udonType) as Type;
            fieldTypes.Add(systemType);
        }
        CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_fieldnames", GetTypeSymbol.Invoke(__instance, new object[] { typeof(string).MakeArrayType() }), fieldNames.ToArray() });
        CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_fieldtypes", GetTypeSymbol.Invoke(__instance, new object[] { typeof(Type).MakeArrayType() }), fieldTypes.ToArray() });
    }

    private static void MethodNeedsExportPatcher(object __instance, ref bool __result)
    {
        var emitType = __instance.GetType().GetProperty("EmitType").GetValue(__instance);
        if (!targetTypes.Value.Contains(emitType.ToString())) return;

        __result = true;
    }

    private static ConcurrentDictionary<string, List<string>> methodDict = new ConcurrentDictionary<string, List<string>>();

    private static void MethodSymbolEmitPatcher(object __instance, object context)
    {
        var emitType = context.GetType().GetProperty("EmitType").GetValue(context);
        if (!targetTypes.Value.Contains(emitType.ToString())) return;

        // https://github.com/NGenesis/UdonCustomEventArgs/blob/main/Packages/com.genesis.udoncustomeventargs/Editor/HarmonyPatcher.cs
        // MIT License
        var linkage = context.GetType().GetMethod("GetMethodLinkage", BindingFlags.Public | BindingFlags.Instance).Invoke(context, new object[] { __instance, false });
        var RootTable = context.GetType().GetProperty("RootTable", BindingFlags.Public | BindingFlags.Instance).GetValue(context);
        var CreateReflectionValue = RootTable.GetType().GetMethod("CreateReflectionValue", BindingFlags.Public | BindingFlags.Instance);
        var GetTypeSymbol = context.GetType().GetMethod("GetTypeSymbol", new Type[] { typeof(Type) });
        var MethodExportName = linkage.GetType().GetProperty("MethodExportName", BindingFlags.Public | BindingFlags.Instance).GetValue(linkage) as string;
        var ParameterValues = linkage.GetType().GetProperty("ParameterValues", BindingFlags.Public | BindingFlags.Instance).GetValue(linkage) as IEnumerable<object>;
        var ReturnValue = linkage.GetType().GetProperty("ReturnValue", BindingFlags.Public | BindingFlags.Instance).GetValue(linkage);

        if (!string.IsNullOrEmpty(MethodExportName))
        {
            var parameterNames = new List<string>();
            var parameterTypes = new List<Type>();

            foreach (var parameter in ParameterValues)
            {
                var UniqueID = parameter.GetType().GetProperty("UniqueID", BindingFlags.Public | BindingFlags.Instance).GetValue(parameter) as string;
                parameterNames.Add(UniqueID);

                var UdonType = parameter.GetType().GetProperty("UdonType", BindingFlags.Public | BindingFlags.Instance).GetValue(parameter);
                var SystemType = UdonType.GetType().GetProperty("SystemType", BindingFlags.Public | BindingFlags.Instance).GetValue(UdonType) as Type;
                parameterTypes.Add(SystemType);
            }

            var ReturnUniqueID = ReturnValue?.GetType()?.GetProperty("UniqueID", BindingFlags.Public | BindingFlags.Instance)?.GetValue(ReturnValue) as string;
            var ReturnUdonType = ReturnValue?.GetType()?.GetProperty("UdonType", BindingFlags.Public | BindingFlags.Instance)?.GetValue(ReturnValue);
            var ReturnSystemType = ReturnUdonType?.GetType()?.GetProperty("SystemType", BindingFlags.Public | BindingFlags.Instance)?.GetValue(ReturnUdonType) as Type;

            CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_argnames_{MethodExportName}", GetTypeSymbol.Invoke(context, new object[] { typeof(string).MakeArrayType() }), parameterNames.ToArray() });
            CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_argtypes_{MethodExportName}", GetTypeSymbol.Invoke(context, new object[] { typeof(Type).MakeArrayType() }), parameterTypes.ToArray() });
            CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_returnname_{MethodExportName}", GetTypeSymbol.Invoke(context, new object[] { typeof(string) }), ReturnUniqueID });
            CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_returntype_{MethodExportName}", GetTypeSymbol.Invoke(context, new object[] { typeof(Type) }), ReturnSystemType });

            methodDict.AddOrUpdate(emitType.ToString(), new List<string>(new string[] { MethodExportName }), (k, v) =>
            {
                v.Add(MethodExportName);
                return v;
            });
        }
    }

    private static void AssembleProgramPatcher(object binding)
    {
        var rootTypeSymbol = binding.GetType().GetField("Item1").GetValue(binding);
        var rootBinding = binding.GetType().GetField("Item2").GetValue(binding);
        var programClass = rootBinding.GetType().GetField("programClass").GetValue(rootBinding) as Type;
        if (!methodDict.ContainsKey(programClass.FullName)) return;

        var assemblyModule = rootBinding.GetType().GetField("assemblyModule").GetValue(rootBinding);
        var rootTable = assemblyModule.GetType().GetProperty("RootTable").GetValue(assemblyModule);
        var CreateReflectionValue = rootTable.GetType().GetMethod("CreateReflectionValue", BindingFlags.Public | BindingFlags.Instance);

        var emitContextType = allTypes.Value.FirstOrDefault(t => t.FullName == "UdonSharp.Compiler.Emit.EmitContext");
        var context = Activator.CreateInstance(emitContextType, args: new object[] { assemblyModule, rootTypeSymbol });
        var GetTypeSymbol = context.GetType().GetMethod("GetTypeSymbol", new Type[] { typeof(Type) });

        CreateReflectionValue.Invoke(rootTable, new object[] { $"__refl_methodnames", GetTypeSymbol.Invoke(context, new object[] { typeof(string).MakeArrayType() }), methodDict[programClass.FullName].ToArray() });
    }
}