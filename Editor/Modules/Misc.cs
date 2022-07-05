using System.Collections.Generic;
using Unity.VisualScripting;

namespace VisualScriptingPrompt
{
    public static class Misc
    {
        static Stack<Unit> currentUnitsStack = new();

        static Misc()
        {
            Units.OnUnitCreated += unit => currentUnitsStack.Push(unit);
        }

        public static void GoToPreviousUnit(string[] args)
        {
            if (currentUnitsStack.Count <= 1) return;

            var selection = GraphWindow.activeContext.selection;
            int times;

            if (!int.TryParse(args[0], out times))
            {
                times = 1;
            }

            currentUnitsStack.Pop();
            for (var i = 0; i < times && currentUnitsStack.Count > 0; i++)
            {
                var unit = currentUnitsStack.Pop();
                selection.Select(unit);
            }
        }
    }
}