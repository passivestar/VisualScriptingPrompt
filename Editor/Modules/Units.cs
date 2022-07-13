using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

namespace VisualScriptingPrompt
{
    [InitializeOnLoad]
    public static class Units
    {
        static List<Unit> currentUnits = new();

        public const float letterWidth = 14f;
        public const float newNodeDefaultHorizontalOffset = 180f;

        public static Unit initialSelectedUnit;

        public static event Action<Unit> OnUnitCreated;

        static Units()
        {
            Prompt.OnOpened += () =>
            {
                currentUnits.Clear();
                var selection = GraphWindow.activeContext.selection;
                initialSelectedUnit = selection.Count != 0 ? selection.First() as Unit : null;
            };
            Prompt.OnCancelled += Clear;
            Prompt.OnType += Clear;
        }

        public static float GetApproximatePortWidth(IUnitPort port)
        {
            var key = port.key;
            if (key == "target") return 120f;
            if (key.StartsWith("%")) return 120f + key.Length * letterWidth;
            return key.Length * letterWidth;
        }

        public static float GetApproximateUnitWidth(Unit unit)
        {
            var width = 0f;
            if (unit.inputs.Count() > 0)
            {
                width += unit.inputs.Max(GetApproximatePortWidth);
            }
            if (unit.outputs.Count() > 0)
            {
                width += unit.outputs.Max(GetApproximatePortWidth);
            }
            return Mathf.Max(newNodeDefaultHorizontalOffset, width);
        }

        public static Unit MakeUnit(Func<Unit> func, bool attachFromLeft = false)
        {
            var context = GraphWindow.activeContext;
            var selection = context.selection;
            var selectedUnit = selection.Count != 0 ? selection.First() as Unit : null;
            var graph = context.graph as FlowGraph;
            var canvas = context.canvas;

            var newUnit = func();
            graph.units.Add(newUnit);

            if (selectedUnit != null)
            {
                // Figure out which side of selected unit to connect to
                if (attachFromLeft && !CanBeConnected(selectedUnit, newUnit) && CanBeConnected(newUnit, selectedUnit))
                {
                    attachFromLeft = false;
                }
                else if (!CanBeConnected(newUnit, selectedUnit) && CanBeConnected(selectedUnit, newUnit))
                {
                    attachFromLeft = true;
                }

                // Set position
                var offsetX = attachFromLeft ? -newNodeDefaultHorizontalOffset : GetApproximateUnitWidth(selectedUnit);
                var offsetY = attachFromLeft ? 100f : 0;
                newUnit.position = selectedUnit.position + new Vector2(offsetX, offsetY);

                // Connect units
                if (attachFromLeft)
                {
                    ConnectUnits(newUnit, selectedUnit);
                }
                else
                {
                    ConnectUnits(selectedUnit, newUnit);
                }
            }
            else
            {
                // Set position of the first unit
                newUnit.position = context.canvas.mousePosition;
            }

            selection.Select(newUnit); // For chaining
            currentUnits.Add(newUnit);
            OnUnitCreated?.Invoke(newUnit);

            return newUnit;
        }

        [MenuItem("Window/Visual Scripting/Connect Selected #e")]
        public static void ConnectSelection()
        {
            var selection = GraphWindow.activeContext.selection;
            var positionSortedSelection = selection.ToList().OrderBy(unit => ((Unit)unit).position.x);

            for (var i = 0; i < positionSortedSelection.Count() - 1; i++)
            {
                var currentUnit = positionSortedSelection.ElementAt(i);
                var nextUnit = positionSortedSelection.ElementAt(i + 1);
                ConnectUnits(currentUnit as Unit, nextUnit as Unit);
            }
        }

        public static bool CanBeConnected(Unit source, Unit target)
        {
            // Check the first unoccupied control output
            if (target.controlOutputs.Count != 0)
            {
                var controlOutput = target.controlOutputs.ToList().Find(output => !output.hasValidConnection);
                if (controlOutput != null)
                {
                    var compatibleControlPort = controlOutput.CompatiblePort(source);
                    if (compatibleControlPort != null && !compatibleControlPort.hasAnyConnection) return true;
                }
            }

            // Check value outputs
            foreach (var valueOutput in target.valueOutputs)
            {
                var compatibleValuePort = valueOutput.CompatiblePort(source);
                if (compatibleValuePort != null && !compatibleValuePort.hasAnyConnection) return true;
            }
            return false;
        }

        public static void ConnectUnits(IUnit leftUnit, IUnit rightUnit)
        {
            // Connect the first unoccupied control output
            if (leftUnit.controlOutputs.Count != 0)
            {
                var controlOutput = leftUnit.controlOutputs.ToList().Find(output => !output.hasValidConnection);
                if (controlOutput != null)
                {
                    var compatibleControlPort = controlOutput.CompatiblePort(rightUnit) as ControlInput;
                    if (compatibleControlPort != null) controlOutput.ConnectToValid(compatibleControlPort);
                }
            }

            // Connect value outputs
            foreach (var valueOutput in leftUnit.valueOutputs)
            {
                var compatibleValuePort = valueOutput.CompatiblePort(rightUnit) as ValueInput;
                if (compatibleValuePort != null) valueOutput.ConnectToValid(compatibleValuePort);
            }
        }

        public static void Clear()
        {
            var graph = GraphWindow.activeContext.graph as FlowGraph;

            foreach (var unit in currentUnits)
            {
                graph.units.Remove(unit);
            }

            currentUnits.Clear();

            // Reset selection
            var selection = GraphWindow.activeContext.selection;
            if (initialSelectedUnit != null)
            {
                selection.Select(initialSelectedUnit);
            }
            else
            {
                selection.Clear();
            }
        }
    }
}