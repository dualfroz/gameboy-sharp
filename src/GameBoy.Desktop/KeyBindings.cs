using System;
using System.Collections.Generic;
using GameBoy.Input;
using Raylib_cs;

namespace GameBoy.Desktop
{
    public sealed class KeyBindings
    {
        public static readonly IReadOnlyList<string> ActionNames = new[]
        {
            "up", "down", "left", "right", "a", "b", "start", "select",
            "save-state", "load-state", "pause", "fast-forward"
        };

        public Dictionary<JoypadButton, KeyboardKey> Buttons { get; } = new Dictionary<JoypadButton, KeyboardKey>
        {
            [JoypadButton.Up] = KeyboardKey.KEY_UP,
            [JoypadButton.Down] = KeyboardKey.KEY_DOWN,
            [JoypadButton.Left] = KeyboardKey.KEY_LEFT,
            [JoypadButton.Right] = KeyboardKey.KEY_RIGHT,
            [JoypadButton.B] = KeyboardKey.KEY_Z,
            [JoypadButton.A] = KeyboardKey.KEY_X,
            [JoypadButton.Start] = KeyboardKey.KEY_ENTER,
            [JoypadButton.Select] = KeyboardKey.KEY_RIGHT_SHIFT
        };

        public KeyboardKey SaveState { get; private set; } = KeyboardKey.KEY_F5;
        public KeyboardKey LoadState { get; private set; } = KeyboardKey.KEY_F8;
        public KeyboardKey Pause { get; private set; } = KeyboardKey.KEY_P;
        public KeyboardKey FastForward { get; private set; } = KeyboardKey.KEY_TAB;

        public void Bind(string action, KeyboardKey key)
        {
            switch (action.ToLowerInvariant())
            {
                case "up": Buttons[JoypadButton.Up] = key; break;
                case "down": Buttons[JoypadButton.Down] = key; break;
                case "left": Buttons[JoypadButton.Left] = key; break;
                case "right": Buttons[JoypadButton.Right] = key; break;
                case "a": Buttons[JoypadButton.A] = key; break;
                case "b": Buttons[JoypadButton.B] = key; break;
                case "start": Buttons[JoypadButton.Start] = key; break;
                case "select": Buttons[JoypadButton.Select] = key; break;
                case "save-state": SaveState = key; break;
                case "load-state": LoadState = key; break;
                case "pause": Pause = key; break;
                case "fast-forward": FastForward = key; break;
                default:
                    throw new OptionsException($"unknown action '{action}' (expected one of: {string.Join(", ", ActionNames)})");
            }
        }

        public static KeyboardKey ParseKey(string name)
        {
            string normalized = name.Trim().ToUpperInvariant();
            if (normalized.StartsWith("KEY_", StringComparison.Ordinal))
                normalized = normalized.Substring(4);

            if (normalized.Length == 0
                || !Enum.TryParse("KEY_" + normalized, false, out KeyboardKey key)
                || !Enum.IsDefined(key)
                || key == KeyboardKey.KEY_NULL)
                throw new OptionsException($"unknown key '{name}' (use raylib key names such as Z, ENTER, RIGHT_SHIFT, F5)");

            return key;
        }
    }
}
