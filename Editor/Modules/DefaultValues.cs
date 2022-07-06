using System;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;

namespace VisualScriptingPrompt
{
    public static class DefaultValues
    {
        public static object ParseString(string text)
        { 
            if (float.TryParse(text, out float floatResult)) return floatResult;
            else if (int.TryParse(text, out int intResult)) return intResult;
            else if (bool.TryParse(text, out bool boolResult)) return boolResult;
            else return text;
        }

        public static (ValueInput valueInput, object value) GetValueInputAndParsedValue(string[] args)
        {
            var valueString = args[0];
            var indexString = args.Length > 1 ? args[1] : "";

            if (valueString == null) return (null, null);

            // TODO:
            // Take individual arguments instead of index
            // to make it possible to set multiple values
            // with one command call

            int index = 0;
            int.TryParse(indexString, out index);

            var context = GraphWindow.activeContext;
            var selection = context.selection;
            var unit = selection.Count != 0 ? selection.First() as Unit : null;
            if (unit != null)
            {
                var parsedValue = ParseString(valueString);
                var type = parsedValue.GetType();

                var valueInputs = unit.valueInputs.ToList()
                    .FindAll(input =>
                    {
                        return input.hasDefaultValue
                            && input.type.IsAssignableFrom(type)
                            && !input.hasAnyConnection;
                    });

                if (index > valueInputs.Count() - 1) return (null, null);
                var valueInput = valueInputs[index];
                return (valueInput, parsedValue);
            }
            return (null, null);
        }

        public static void SetDefaultValue(string[] args)
        {
            var (valueInput, value) = GetValueInputAndParsedValue(args);
            if (value != null)
            {
                valueInput.SetDefaultValue(value); 
            }
        }
    }
}