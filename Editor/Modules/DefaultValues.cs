using System;
using System.Collections.Generic;
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

        public static List<(ValueInput valueInput, object value)> GetValueInputsAndParsedValues(string[] args)
        {
            var selection = GraphWindow.activeContext.selection;
            var unit = selection.Count != 0 ? selection.First() as Unit : null;
            if (unit != null)
            {
                List<ValueInput> takenPorts = new();

                return args.Select(value =>
                {
                    var parsedValue = ParseString(value);
                    var type = parsedValue.GetType();
                    var empty = type == typeof(string) && (string)parsedValue == "";

                    var valueInput = unit.valueInputs
                        .FirstOrDefault(input =>
                        {
                            return input.hasDefaultValue
                                && (input.type.IsAssignableFrom(type) || empty)
                                && !input.hasAnyConnection
                                && !takenPorts.Contains(input);
                        });
                    
                    if (valueInput != null)
                    {
                        takenPorts.Add(valueInput);
                    }

                    // If the value is an empty string, skip the input
                    return (valueInput: empty ? null : valueInput, parsedValue); 
                })
                .Where(item => item.valueInput != null)
                .ToList();
            }
            return null;
        }

        public static void SetDefaultValue(string[] args)
        {
            foreach (var (valueInput, value) in GetValueInputsAndParsedValues(args))
            {
                if (valueInput != null && value != null)
                {
                    valueInput.SetDefaultValue(value);
                }
            }
        }
    }
}