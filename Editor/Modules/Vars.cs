using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Unity.VisualScripting;

namespace VisualScriptingPrompt
{
    public static class Vars
    {
        static List<(
            string name,
            UnifiedVariableUnit variableUnit,
            VariableDeclarations source,
            FlowGraph graph,
            bool wasDefined // Make sure to not remove vars that were already defined
        )> currentVariables = new();

        static Vars()
        {
            Prompt.OnOpened += () => currentVariables.Clear();
            Prompt.OnCancelled += Clear;
            Prompt.OnType += Clear;
        }

        public static object CreateDefaultVariableValue(Type type)
        {
            if (type == typeof(System.String)) return "";
            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        public static void MakeVariable(string[] args, bool set, VariableKind kind = VariableKind.Graph)
        {
            var name = args[0];
            var graph = GraphWindow.activeContext.graph as FlowGraph;
            var selection = GraphWindow.activeContext.selection;

            if (selection.Count == 0)
            {
                Debug.LogWarning("Nothing is selected");
                return;
            }

            var unit = selection.First() as Unit;

            ValueInput valueInput = default(ValueInput);
            ValueOutput valueOutput = default(ValueOutput);

            if (set)
            {
                // Find the first empty output
                valueOutput = unit.valueOutputs.ToList().Find(output => !output.hasValidConnection) as ValueOutput;
                if (valueOutput == null)
                {
                    Debug.LogWarning("No output on the selected node available for the new variable");
                    return;
                }
            }
            else
            {
                // Find the first empty input
                valueInput = unit.valueInputs.ToList().Find(input => !input.hasValidConnection) as ValueInput;
                if (valueInput == null)
                {
                    Debug.LogWarning("No input on the selected node available for the new variable");
                    return;
                }
            }

            if (name == null)
            {
                name = set ? valueOutput.key : valueInput.key;
                var forbiddenCharacters = new Regex(@"[%\`_<>]");
                name = forbiddenCharacters.Replace(name, "");
            }

            if (name.Length == 0)
            {
                return;
            }

            var (source, wasDefined) = SetVariable(name, set ? valueOutput.type : valueInput.type, kind, graph);
            var variableUnit = CreateVariableUnit(name, set, kind, valueInput, valueOutput, graph, unit);

            currentVariables.Add((name, variableUnit as UnifiedVariableUnit, source, graph, wasDefined));
        }

        public static (VariableDeclarations source, bool wasDefined) SetVariable(string name, Type type, VariableKind kind, FlowGraph graph)
        {
            var variableValue = CreateDefaultVariableValue(type);
            VariableDeclarations source = graph.variables;

            switch (kind)
            {
                case VariableKind.Graph:
                    source = graph.variables;
                    break;
                case VariableKind.Object:
                    source = Variables.Object(GraphWindow.activeReference.gameObject);
                    break;
                case VariableKind.Scene:
                    source = Variables.ActiveScene;
                    break;
                case VariableKind.Application:
                    source = Variables.Application;
                    break;
                case VariableKind.Saved:
                    source = Variables.Saved;
                    break;
            }

            var wasDefined = source.IsDefined(name);
            if (!wasDefined)
            {
                source.Set(name, variableValue);
            }

            return (source, wasDefined);
        }

        public static Unit CreateVariableUnit(
            string name,
            bool set,
            VariableKind kind,
            ValueInput input,
            ValueOutput output,
            FlowGraph graph,
            Unit unit
        )
        {
            var variableUnit = set ? new SetVariable() as UnifiedVariableUnit : new GetVariable() as UnifiedVariableUnit;
            graph.units.Add(variableUnit);
            variableUnit.kind = kind;
            variableUnit.name.SetDefaultValue(name);

            if (set)
            {
                ((SetVariable)variableUnit).input.ConnectToValid(output);
                variableUnit.position = unit.position + new Vector2(Units.GetApproximateUnitWidth(unit), 0);
            }
            else
            {
                ((GetVariable)variableUnit).value.ConnectToValid(input);
                variableUnit.position = unit.position + new Vector2(-Units.newNodeDefaultHorizontalOffset, 100f);
            }

            return variableUnit;
        }

        public static void Clear()
        {
            foreach (var variable in currentVariables)
            {
                var (name, variableUnit, source, graph, wasDefined) = variable;
                if (!wasDefined)
                {
                    // VariableDeclarations type doesnt allow removing,
                    // so doing this hacky workaround instead:
                    var declaration = source.GetDeclaration(name);
                    var newSource = source.Where(d => d != declaration).ToList();
                    source.Clear();
                    foreach (var dec in newSource)
                    {
                        source.Set(dec.name, dec.value);
                    }
                }

                graph.units.Remove(variableUnit);
            }

            currentVariables.Clear();
        }
    }
}