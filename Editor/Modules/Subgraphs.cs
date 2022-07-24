using Unity.VisualScripting;

namespace VisualScriptingPrompt
{
    public static class Subgraphs
    {
        public static void MakeSubgraph(string[] args, bool input, bool output)
        {
            var name = args[0];
            var unit = (SubgraphUnit)Units.MakeUnit(() =>
            {
                var unit = SubgraphUnit.WithInputOutput();
                var subgraph = unit.nest.embed;
                subgraph.title = name;
                if (input)
                {
                    Ports.AddControlInputDefinition(subgraph, "enter", "Enter");
                }
                if (output)
                {
                    Ports.AddControlOutputDefinition(subgraph, "exit", "Exit");
                }
                return unit.Option();
            });
        }
    }
}