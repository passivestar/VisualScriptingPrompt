using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

namespace VisualScriptingPrompt
{
    public class Prompt : EditorWindow
    {
        static Vector2 popupSize = new Vector2(600f, 44f);

        public static bool isOpened = false;
        static bool justOpened = false;

        public static string promptText = "";
        public static string hintText = "";

        public static event Action OnOpened;
        public static event Action OnConfirmed;
        public static event Action OnCancelled;
        public static event Action OnType;
        public static event Action OnCommandsExecuted;

        [MenuItem("Window/Visual Scripting/Visual Scripting Prompt #q")]
        static void Init()
        {
            if (isOpened) return;

            isOpened = justOpened = true;

            if (GraphWindow.activeContext == null)
            {
                Debug.LogWarning("Unable to find active context.");
                return;
            }

            var window = ScriptableObject.CreateInstance<Prompt>();
            var graphWindowPosition = GraphWindow.active.reliablePosition;

            var pos = new Rect
            (
                graphWindowPosition.center.x - popupSize.x / 2,
                graphWindowPosition.yMin + 40,
                popupSize.x,
                popupSize.y
            );

            window.minSize = new Vector2(20, 20);
            window.position = pos;
            window.ShowPopup();

            OnOpened?.Invoke();
        }

        void OnDestroy()
        {
            promptText = hintText = "";
            isOpened = false;
        }

        void OnLostFocus()
        {
            OnConfirmed?.Invoke();
            this.Close();
        }

        void OnGUI()
        {
            GUI.SetNextControlName("Prompt");
            promptText = EditorGUILayout.TextField(promptText);
            GUILayout.Label(hintText);

            if (justOpened)
            {
                EditorGUI.FocusTextInControl("Prompt");
                justOpened = false;
            }

            if (Event.current.isKey)
            {
                if (promptText.Length == 0) hintText = $"Type unit names ({Library.units.Count} units)";
                var e = Event.current;

                if (e.keyCode == KeyCode.Escape)
                {
                    OnCancelled?.Invoke();
                    this.Close();
                    return;
                }

                if (e.keyCode == KeyCode.Return)
                {
                    OnConfirmed?.Invoke();
                    this.Close();
                    return;
                }

                ProcessCommandInput(promptText);
                Repaint();
            }
        }

        void ProcessCommandInput(string input)
        {
            OnType?.Invoke();

            input = input.Trim();
            if (input == "") return;

            // Split by whitespace, find suggested keys, map to commands
            var words = Regex.Split(input, @"\s+");

            var currentCommands = new List<Action>();

            for (var i = 0; i < words.Length; i++)
            {
                var left = false;
                var word = words[i];
                var lastWord = i == words.Length - 1;
                if (word.StartsWith("!"))
                {
                    word = word[1..];
                    left = true;
                }

                var commands = Commands.list;
                var command = commands.Find(entry => word.StartsWith(entry.name));
                var args = "";
                if (command.name == null)
                {
                    command = Commands.emptyCommand;
                    args = word;
                }
                else
                {
                    args = word[command.name.Length..];
                }
                currentCommands.Add(command.func(args.Split(','), left, lastWord));
            }

            foreach (var command in currentCommands)
            {
                command();
            }
            OnCommandsExecuted?.Invoke();
        }
    }
}