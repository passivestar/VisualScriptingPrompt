using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

namespace VisualScriptingPrompt
{
    public static class Commands
    {
        public static (string name, Func<string[], bool, bool, Action> func) emptyCommand;
        public static (string name, Func<string[], bool, bool, Action> func) unitCommand;
        public static (string name, Func<string[], bool, bool, Action> func) defaultValueCommand;

        static Commands()
        {
            emptyCommand = list.Find(command => command.name == "empty:");
            unitCommand = list.Find(command => command.name == "unit:");
            defaultValueCommand = list.Find(command => command.name == "defaultvalue:");
        }

        public static List<(string name, Func<string[], bool, bool, Action> func)> list = new()
        {
            ("empty:", (args, left, last) => () => {
                var (valueInput, value) = DefaultValues.GetValueInputAndParsedValue(args);

                // TODO:
                // - String default value when no node was found
                // - Subgraph if no node was found and string cant
                // be assigned as a default value

                if (value != null && value.GetType() != typeof(string))
                {
                    defaultValueCommand.func(args, left, last)();
                }
                else
                {
                    unitCommand.func(args, left, last)();
                }
            }),

            ("unit:", (args, left, last) => () => {
                var name = args[0].ToLower();
                var suggestionsForWord = Library.GetSuggestions(name, last ? 10 : 1);
                if (suggestionsForWord.Count != 0)
                {
                    Units.MakeUnit(suggestionsForWord.First().func, left);
                    if (last) Prompt.hintText = Library.GetHintText(suggestionsForWord);
                }
            }),

            ("up:", (args, left, last) => () => Misc.GoToPreviousUnit(args)),

            ("subgraph:", (args, left, last) => () => Subgraphs.MakeSubgraph(args, true, true)),
            ("subgraphin:", (args, left, last) => () => Subgraphs.MakeSubgraph(args, true, false)),
            ("subgraphout:", (args, left, last) => () => Subgraphs.MakeSubgraph(args, false, true)),

            ("valueinput:", (args, left, last) => () => {
                var name = args[0];
                if (name == null) return;
                var graph = Ports.GetCurrentOrSelectedGraph();
                var def = Ports.AddValueInputDefinition(graph, name.ToLower(), name, null);
            }),

            ("valueoutput:", (args, left, last) => () => {
                var name = args[0];
                if (name == null) return;
                var graph = Ports.GetCurrentOrSelectedGraph();
                var def = Ports.AddValueOutputDefinition(graph, name.ToLower(), name, null);
            }),

            ("controlinput:", (args, left, last) => () => {
                var name = args[0];
                if (name == null) return;
                var graph = Ports.GetCurrentOrSelectedGraph();
                var def = Ports.AddControlInputDefinition(graph, name.ToLower(), name);
            }),

            ("controloutput:", (args, left, last) => () => {
                var name = args[0];
                if (name == null) return;
                var graph = Ports.GetCurrentOrSelectedGraph();
                var def = Ports.AddControlOutputDefinition(graph, name.ToLower(), name);
            }),

            ("defaultvalue:", (args, left, last) => () => DefaultValues.SetDefaultValue(args)),

            ("vargraph:", (args, left, last) => () => Vars.MakeVariable(args, false, VariableKind.Graph)),
            ("setvargraph:", (args, left, last) => () => Vars.MakeVariable(args, true, VariableKind.Graph)),

            ("varobject:", (args, left, last) => () => Vars.MakeVariable(args, false, VariableKind.Object)),
            ("setvarobject:", (args, left, last) => () => Vars.MakeVariable(args, true, VariableKind.Object)),

            ("varscene:", (args, left, last) => () => Vars.MakeVariable(args, false, VariableKind.Scene)),
            ("setvarscene:", (args, left, last) => () => Vars.MakeVariable(args, true, VariableKind.Scene)),

            ("varapp:", (args, left, last) => () => Vars.MakeVariable(args, false, VariableKind.Application)),
            ("setvarapp:", (args, left, last) => () => Vars.MakeVariable(args, true, VariableKind.Application)),

            ("varsaved:", (args, left, last) => () => Vars.MakeVariable(args, false, VariableKind.Saved)),
            ("setvarsaved:", (args, left, last) => () => Vars.MakeVariable(args, true, VariableKind.Saved)),

            ("showall:", (args, left, last) => () => {
                var name = args[0];
                if (name == null) return;
                name = name.ToLower();
                var suggestions = Library.GetSuggestions(name, 1000).Select(suggestion => suggestion.name);
                Debug.Log(string.Join(", ", suggestions));
            })
        };
    }
}