using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Unity.VisualScripting;

namespace VisualScriptingPrompt
{
    [InitializeOnLoad]
    public static class Ports
    {
        const int maxLabelLength = 6;

        static List<(
            UnitPortDefinition port,
            FlowGraph graph
        )> currentPorts = new();

        static Ports()
        {
            Prompt.OnOpened += () => currentPorts.Clear();
            Prompt.OnCancelled += Clear;
            Prompt.OnType += Clear;
        }

        public static FlowGraph GetCurrentOrSelectedGraph()
        {
            var context = GraphWindow.activeContext;
            var selection = context.selection;
            var unit = selection.Count != 0 ? selection.First() as Unit : null;
            if (unit is SubgraphUnit)
            {
                var embed = ((SubgraphUnit)unit).nest.embed;
                if (embed != null)
                {
                    return embed;
                }
                return ((SubgraphUnit)unit).nest.graph;
            }
            else
            {
                return (FlowGraph)context.graph;
            }
        }

        public static ValueInputDefinition AddValueInputDefinition(FlowGraph graph, string key, string label, Type type)
        {
            ValueInputDefinition definition = new();
            definition.key = key;
            definition.label = label;
            definition.hideLabel = label.Length > maxLabelLength;
            definition.type = type ?? typeof(object);
            graph.valueInputDefinitions.Add(definition);
            currentPorts.Add((definition, graph));
            graph.PortDefinitionsChanged();
            return definition;
        }

        public static ValueOutputDefinition AddValueOutputDefinition(FlowGraph graph, string key, string label, Type type)
        {
            ValueOutputDefinition definition = new();
            definition.key = key;
            definition.label = label;
            definition.hideLabel = label.Length > maxLabelLength;
            definition.type = type ?? typeof(object);
            graph.valueOutputDefinitions.Add(definition);
            currentPorts.Add((definition, graph));
            graph.PortDefinitionsChanged();
            return definition;
        }

        public static ControlInputDefinition AddControlInputDefinition(FlowGraph graph, string key, string label)
        {
            ControlInputDefinition definition = new();
            definition.key = key;
            definition.label = label;
            definition.hideLabel = label == "Enter";
            graph.controlInputDefinitions.Add(definition);
            currentPorts.Add((definition, graph));
            graph.PortDefinitionsChanged();
            return definition;
        }

        public static ControlOutputDefinition AddControlOutputDefinition(FlowGraph graph, string key, string label)
        {
            ControlOutputDefinition definition = new();
            definition.key = key;
            definition.label = label;
            definition.hideLabel = label == "Exit";
            graph.controlOutputDefinitions.Add(definition);
            currentPorts.Add((definition, graph));
            graph.PortDefinitionsChanged();
            return definition;
        }

        public static List<IUnitConnection> GetOutsideConnections<TPort>(IUnitPortCollection<TPort> ports) where TPort : IUnitPort
        {
            var selection = GraphWindow.activeContext.selection;
            List<IUnitConnection> connections = new List<IUnitConnection>();
            foreach (var port in ports)
            {
                foreach (var connection in port.connections)
                {
                    // Check if the ports are input ports
                    if (typeof(TPort) == typeof(ControlInput) || typeof(TPort) == typeof(ValueInput))
                    {
                        if (!selection.Contains(connection.source.unit))
                        {
                            connections.Add(connection);
                        }
                    }
                    // Else ports are output ports
                    else
                    {
                        if (!selection.Contains(connection.destination.unit))
                        {
                            connections.Add(connection);
                        }
                    }
                }
            }
            return connections;
        }

        public static IUnit GetGraphInput(FlowGraph graph) => graph.units.ToList().Find(unit => unit.GetType() == typeof(GraphInput));
        public static IUnit GetGraphOutput(FlowGraph graph) => graph.units.ToList().Find(unit => unit.GetType() == typeof(GraphOutput));

        public static void Clear()
        {
            foreach (var portDefinition in currentPorts)
            {
                var (port, graph) = portDefinition;
                if (port.GetType() == typeof(ValueInputDefinition))
                {
                    graph.valueInputDefinitions.Remove(port as ValueInputDefinition);
                }
                else if (port.GetType() == typeof(ValueOutputDefinition))
                {
                    graph.valueOutputDefinitions.Remove(port as ValueOutputDefinition);
                }
                else if (port.GetType() == typeof(ControlInputDefinition))
                {
                    graph.controlInputDefinitions.Remove(port as ControlInputDefinition);
                }
                else if (port.GetType() == typeof(ControlOutputDefinition))
                {
                    graph.controlOutputDefinitions.Remove(port as ControlOutputDefinition);
                }
            }
        }
    }
}