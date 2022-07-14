using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

namespace VisualScriptingPrompt
{
    [InitializeOnLoad]
    public static class Library
    {
        const int maxHintLength = 100;

        public static List<(string name, Func<Unit> func)> units = new();

        static Library()
        {
            BuildUnitsLibrary();
        }

        public static List<(string name, Func<Unit> func)> GetSuggestions(string input, int num)
        {
            return units
                .Where(unit => unit.name.Contains(input.ToLower()))
                .Take(num)
                .ToList();
        }

        public static string GetHintText(List<(string name, Func<Unit> func)> suggestions)
        {
            var list = suggestions.Select(suggestion => suggestion.name);
            var hint = string.Join(", ", list);
            return hint.Length > maxHintLength ? hint.Substring(0, maxHintLength) + "..." : hint;
        }

        public static void AddTypeToUnits(Type type)
        {
            var forbiddenCharacters = new Regex(@"[`_<>]");
            var typeInfo = type.GetTypeInfo();
            if (
                typeInfo.IsInterface
                || typeInfo.IsGenericType
                || typeInfo.IsEnum
            ) return;

            if (forbiddenCharacters.IsMatch(typeInfo.Name)) return;
            var ns = typeInfo.Namespace;
            if (Config.data.excludeNamespaces.Any(n => ns != null ? ns.Contains(n) : true)) return;

            var flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;

            var constructors = type.GetConstructors(flags);
            var methods = type.GetMethods(flags);
            var properties = type.GetProperties(flags);
            var fields = type.GetFields(flags);

            // Add constructors
            foreach (var constructor in constructors)
            {
                if (constructor.ContainsGenericParameters) continue;
                var parameters = constructor.GetParameters().Select(p => p.ParameterType.Name);
                var parametersString = parameters.Count() > 0 ? $"({String.Join(',', parameters)})" : "";
                var unitName = $"{typeInfo.Name}.{constructor.Name}{parametersString}".ToLower();
                units.Add((unitName, () => new InvokeMember(new Member(type, constructor))));
            }

            // Add methods
            foreach (var method in methods)
            {
                if (method.IsGenericMethod || method.ContainsGenericParameters) continue;
                if (forbiddenCharacters.IsMatch(method.Name)) continue;
                var parameters = method.GetParameters().Select(p => p.ParameterType.Name);
                var parametersString = parameters.Count() > 0 ? $"({String.Join(',', parameters)})" : "";
                var unitName = $"{typeInfo.Name}.{method.Name}{parametersString}".ToLower();
                units.Add((unitName, () => new InvokeMember(new Member(type, method))));
            }

            // Add properties
            foreach (var property in properties)
            {
                if (forbiddenCharacters.IsMatch(property.Name)) continue;
                var unitName = $"{typeInfo.Name}.{property.Name}".ToLower();
                var unitSetName = $"{typeInfo.Name}.set{property.Name}".ToLower();
                units.Add((unitName, () => new GetMember(new Member(type, property))));
                if (property.CanWrite && property.GetSetMethod(true).IsPublic)
                    units.Add((unitSetName, () => new SetMember(new Member(type, property))));
            }

            // Add fields
            foreach (var field in fields)
            {
                if (forbiddenCharacters.IsMatch(field.Name)) continue;
                var unitName = $"{typeInfo.Name}.{field.Name}".ToLower();
                var unitSetName = $"{typeInfo.Name}.set{field.Name}".ToLower();
                units.Add((unitName, () => new GetMember(new Member(type, field))));
                if (!field.IsInitOnly)
                    units.Add((unitSetName, () => new SetMember(new Member(type, field))));
            }
        }

        public static void AddTypesToUnits(Type[] types)
        {
            foreach (var type in types) AddTypeToUnits(type);
        }

        public static void AddVisualScriptingTypesToUnits()
        {
            // Get all types derived from Unit
            Type unitType = typeof(Unit);
            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => unitType.IsAssignableFrom(t));

            foreach (var type in types)
            {
                var name = type.ToString().Split('.').Last().ToLower();
                units.Add((
                    name,
                    () =>
                    {
                        try
                        {
                            return Activator.CreateInstance(type) as Unit;
                        }
                        catch
                        {
                            return new Null();
                        }
                    }
                ));
            }
        }

        public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToList();
        }

        public static List<(string, Func<Unit>)> GetShortcutUnits()
        {
            var shortcutUnits = new List<(string, Func<Unit>)>();

            // Unit Aliases
            foreach (var alias in Config.data.unitAliases)
            {
                shortcutUnits.Add((alias.from, units.Find(u => u.name == alias.to).func));
            }

            // Literals
            shortcutUnits.Add(("1", () => new Literal(typeof(float))));
            shortcutUnits.Add(("s", () => new Literal(typeof(string))));
            shortcutUnits.Add(("int", () => new Literal(typeof(int))));
            shortcutUnits.Add(("bool", () => new Literal(typeof(bool))));
            shortcutUnits.Add(("listint", () => new Literal(typeof(List<int>))));
            shortcutUnits.Add(("listbool", () => new Literal(typeof(List<bool>))));
            shortcutUnits.Add(("listfloat", () => new Literal(typeof(List<float>))));
            shortcutUnits.Add(("listvector2", () => new Literal(typeof(List<Vector2>))));
            shortcutUnits.Add(("listvector3", () => new Literal(typeof(List<Vector3>))));
            shortcutUnits.Add(("listquaternion", () => new Literal(typeof(List<Quaternion>))));
            shortcutUnits.Add(("listobject", () => new Literal(typeof(List<object>))));
            shortcutUnits.Add(("listtransform", () => new Literal(typeof(List<Transform>))));
            shortcutUnits.Add(("listgameobject", () => new Literal(typeof(List<GameObject>))));

            // Scalar shortcuts
            var scalarUnits = units.Where(unit => unit.name.StartsWith("scalar")).ToList();
            foreach (var unit in scalarUnits)
            {
                shortcutUnits.Add((unit.name[6..], units.Find(u => u.name == unit.name).func));
            }

            // Add graph asset units
            var graphAssets = FindAssetsByType<ScriptGraphAsset>();
            foreach (var asset in graphAssets)
            {
                shortcutUnits.Add(("#" + asset.ToString().ToLower(), () => new SubgraphUnit(asset)));
            }

            return shortcutUnits;
        }

        [MenuItem("Window/Visual Scripting/Rebuild Prompt Node Library")]
        public static void BuildUnitsLibrary()
        {
            units.Clear();

            AddVisualScriptingTypesToUnits();

            // Add types from specified assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName().Name;
                if (Config.data.assemblies.Contains(assemblyName))
                {
                    AddTypesToUnits(assembly.GetTypes());
                }
            }

            // Add shortcuts
            units = GetShortcutUnits().Concat(units).ToList();

            // Sort units
            units = units
                .OrderBy(unit => unit.name.Length)
                // TODO: Better priority sorting
                //
                // .OrderBy(unit =>
                // {
                //     var index = Config.data.priorityNames.ToList().FindIndex(name => unit.name.StartsWith(name));
                //     return index != -1 ? index : Mathf.Infinity;
                // })
                .ToList();

            // Add command aliases from config
            foreach (var alias in Config.data.commandAliases)
            {
                var target = Commands.list.Find(c => c.name == alias.to);
                if (target.name != null)
                {
                    Commands.list.Add((name: alias.from, func: target.func));
                }
            }
        }
    }
}